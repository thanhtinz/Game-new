using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.WorldGen;

/// <summary>
/// Sinh thế giới procedural dùng Perlin Noise layered.
/// Không dùng thư viện ngoài — tự implement noise để tránh dependency.
/// </summary>
public interface IWorldGeneratorService
{
    Task<List<WorldTileData>> GenerateAsync(string worldId, int width, int height, int seed = 0);
}

public class WorldGeneratorService : IWorldGeneratorService
{
    private readonly IWorldRepository _worldRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly ILogger<WorldGeneratorService> _logger;

    public WorldGeneratorService(
        IWorldRepository worldRepo,
        ICivilizationRepository civRepo,
        ILogger<WorldGeneratorService> logger)
    {
        _worldRepo = worldRepo;
        _civRepo = civRepo;
        _logger = logger;
    }

    public async Task<List<WorldTileData>> GenerateAsync(string worldId, int width, int height, int seed = 0)
    {
        if (seed == 0) seed = Random.Shared.Next(1, 999999);
        _logger.LogInformation("Sinh world {WorldId} ({W}x{H}) seed={Seed}", worldId, width, height, seed);

        var noise = new PerlinNoise(seed);
        var tiles = new List<WorldTileData>(width * height);

        // Pass 1: sinh elevation + moisture map
        float[,] elevation = new float[width, height];
        float[,] moisture  = new float[width, height];
        float[,] temperature = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Elevation: nhiều octave để có địa hình phong phú
                float e = noise.Octave(x * 0.04f, y * 0.04f, octaves: 6, persistence: 0.5f);
                // Gradient hướng vào trung tâm để tạo đảo (optional)
                float cx = (x - width * 0.5f) / (width * 0.5f);
                float cy = (y - height * 0.5f) / (height * 0.5f);
                float islandGrad = 1f - (cx * cx + cy * cy);
                elevation[x, y] = Clamp01(e * 0.7f + islandGrad * 0.3f);

                // Moisture độc lập với elevation
                moisture[x, y] = noise.Octave(x * 0.05f + 100f, y * 0.05f + 100f, octaves: 4, persistence: 0.6f);
                // Temperature giảm theo vĩ độ (y)
                float latTemp = 1f - MathF.Abs((y - height * 0.5f) / (height * 0.5f));
                temperature[x, y] = Clamp01(latTemp + noise.Get(x * 0.03f + 200f, y * 0.03f + 200f) * 0.2f);
            }
        }

        // Pass 2: classify biome và assign TileType
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float e = elevation[x, y];
                float m = moisture[x, y];
                float t = temperature[x, y];

                var tileType = ClassifyBiome(e, m, t);
                float fertility = ComputeFertility(tileType, e, m);

                tiles.Add(new WorldTileData
                {
                    X = x,
                    Y = y,
                    Type = tileType,
                    Fertility = fertility
                });
            }
        }

        // Pass 3: đặt Sacred sites ngẫu nhiên (5-8 điểm)
        PlaceSacredSites(tiles, width, height, seed);

        // Lưu tiles vào DB
        await _worldRepo.UpdateTilesAsync(worldId, tiles);

        // Spawn civilizations trên tiles phù hợp
        await SpawnCivilizationsOnTilesAsync(worldId, tiles, width, height);

        _logger.LogInformation("World generation xong: {Count} tiles", tiles.Count);
        return tiles;
    }

    // ─── Biome Classification ────────────────────────────────

    private static TileType ClassifyBiome(float elevation, float moisture, float temperature)
    {
        // Water
        if (elevation < 0.3f) return TileType.Water;

        // Mountain / Volcano
        if (elevation > 0.82f)
            return temperature > 0.7f ? TileType.Volcano : TileType.Mountain;

        // High elevation
        if (elevation > 0.65f) return TileType.Mountain;

        // Desert: nóng + khô
        if (temperature > 0.7f && moisture < 0.3f) return TileType.Desert;

        // Tundra: lạnh
        if (temperature < 0.25f) return TileType.Tundra;

        // Forest: độ ẩm cao
        if (moisture > 0.6f) return TileType.Forest;

        // Default: grassland
        return TileType.Grassland;
    }

    private static float ComputeFertility(TileType type, float elevation, float moisture) => type switch
    {
        TileType.Grassland => 0.6f + moisture * 0.4f,
        TileType.Forest    => 0.5f + moisture * 0.3f,
        TileType.Desert    => 0.05f + moisture * 0.1f,
        TileType.Tundra    => 0.1f,
        TileType.Mountain  => 0.15f,
        TileType.Water     => 0f,
        TileType.Volcano   => 0.7f, // Đất núi lửa rất màu mỡ
        TileType.Sacred    => 1f,
        _ => 0.3f
    };

    // ─── Sacred Sites ────────────────────────────────────────

    private static void PlaceSacredSites(List<WorldTileData> tiles, int width, int height, int seed)
    {
        var rng = new Random(seed ^ 0xDEADBEEF);
        int count = rng.Next(5, 9);

        var landTiles = tiles
            .Where(t => t.Type is TileType.Grassland or TileType.Forest)
            .OrderBy(_ => rng.Next())
            .Take(count)
            .ToList();

        foreach (var tile in landTiles)
            tile.Type = TileType.Sacred;
    }

    // ─── Civilization Placement ──────────────────────────────

    private async Task SpawnCivilizationsOnTilesAsync(
        string worldId, List<WorldTileData> tiles, int width, int height)
    {
        // Tìm các vùng Grassland/Forest có fertility cao để spawn civ
        var goodTiles = tiles
            .Where(t => t.Type is TileType.Grassland or TileType.Forest && t.Fertility > 0.5f)
            .ToList();

        if (!goodTiles.Any()) return;

        int civCount = Math.Min(8, goodTiles.Count / 10);
        civCount = Math.Max(3, civCount);

        var rng = new Random();
        var spawnPoints = new List<WorldTileData>();

        // Đảm bảo các civ đủ xa nhau (khoảng cách tối thiểu = width/4)
        int minDist = Math.Max(8, width / 4);

        foreach (var candidate in goodTiles.OrderByDescending(t => t.Fertility))
        {
            bool tooClose = spawnPoints.Any(sp =>
                MathF.Sqrt(MathF.Pow(sp.X - candidate.X, 2) + MathF.Pow(sp.Y - candidate.Y, 2)) < minDist);

            if (!tooClose)
            {
                spawnPoints.Add(candidate);
                if (spawnPoints.Count >= civCount) break;
            }
        }

        var personalities = Enum.GetValues<CivilizationPersonality>();
        string[] prefixes = { "Ara", "Sol", "Mor", "Thal", "Eld", "Vor", "Kha", "Zyn", "Vel", "Orn" };
        string[] suffixes = { "eth", "ian", "ara", "os", "um", "or", "ix", "ar", "on", "us" };

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var sp = spawnPoints[i];
            var name = prefixes[rng.Next(prefixes.Length)] + suffixes[rng.Next(suffixes.Length)];

            // Lấy 3 tile xung quanh điểm spawn
            var controlledTiles = GetNeighborLandTiles(tiles, sp.X, sp.Y, width, height, 3);

            var civ = new CivilizationDocument
            {
                WorldId = worldId,
                Name = name,
                Personality = personalities[rng.Next(personalities.Length)],
                Population = rng.Next(80, 200),
                Economy = rng.Next(30, 70),
                Military = rng.Next(10, 40),
                ControlledTiles = controlledTiles.Select(t => new TileCoord { X = t.X, Y = t.Y }).ToList()
            };

            await _civRepo.CreateAsync(civ);

            // Cập nhật tiles để biết civ nào ở đó
            foreach (var t in controlledTiles)
                t.CivilizationId = civ.Id;
        }

        // Lưu lại tiles đã cập nhật CivId
        await _worldRepo.UpdateTilesAsync(worldId, tiles);
        _logger.LogInformation("Spawned {Count} civilizations", spawnPoints.Count);
    }

    private static List<WorldTileData> GetNeighborLandTiles(
        List<WorldTileData> tiles, int cx, int cy, int width, int height, int radius)
    {
        return tiles
            .Where(t => t.Type != TileType.Water
                && MathF.Abs(t.X - cx) <= radius
                && MathF.Abs(t.Y - cy) <= radius)
            .OrderBy(t => MathF.Pow(t.X - cx, 2) + MathF.Pow(t.Y - cy, 2))
            .Take(4)
            .ToList();
    }

    private static float Clamp01(float v) => MathF.Max(0f, MathF.Min(1f, v));
}

// ─── Perlin Noise Implementation ─────────────────────────
/// <summary>
/// Perlin noise 2D tự implement, không cần thư viện ngoài.
/// </summary>
public class PerlinNoise
{
    private readonly int[] _perm;

    public PerlinNoise(int seed)
    {
        var rng = new Random(seed);
        _perm = new int[512];
        var p = Enumerable.Range(0, 256).OrderBy(_ => rng.Next()).ToArray();
        for (int i = 0; i < 512; i++) _perm[i] = p[i & 255];
    }

    public float Get(float x, float y)
    {
        int xi = (int)MathF.Floor(x) & 255;
        int yi = (int)MathF.Floor(y) & 255;
        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);

        float u = Fade(xf), v = Fade(yf);

        int aa = _perm[_perm[xi] + yi];
        int ab = _perm[_perm[xi] + yi + 1];
        int ba = _perm[_perm[xi + 1] + yi];
        int bb = _perm[_perm[xi + 1] + yi + 1];

        float res = Lerp(v,
            Lerp(u, Grad(aa, xf, yf), Grad(ba, xf - 1, yf)),
            Lerp(u, Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1)));

        return (res + 1f) * 0.5f; // normalize 0..1
    }

    public float Octave(float x, float y, int octaves, float persistence)
    {
        float total = 0, frequency = 1, amplitude = 1, maxValue = 0;
        for (int i = 0; i < octaves; i++)
        {
            total += Get(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2;
        }
        return total / maxValue;
    }

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float t, float a, float b) => a + t * (b - a);
    private static float Grad(int hash, float x, float y)
    {
        int h = hash & 3;
        float u = h < 2 ? x : y;
        float v = h < 2 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
