using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.WorldGen;

/// <summary>
/// Procedural world generator inspired by WorldBox-style terrain generation.
/// Pipeline:
///   1. Continent shaping (blob-based landmass, not pure radial gradient)
///   2. Elevation via layered Perlin noise + continent mask
///   3. Ridge noise mountain ranges (linear chains, not scattered blobs)
///   4. Moisture + temperature maps
///   5. Biome classification
///   6. River carving (gradient descent from mountain peaks to sea)
///   7. Coastline / Beach tile insertion
///   8. Biome smoothing pass (majority filter, removes single-tile noise specks)
///   9. Sacred sites + civilization + entity placement
/// No external dependencies — Perlin/ridge noise implemented in-file.
/// </summary>
public interface IWorldGeneratorService
{
    /// <summary>Generates the world and returns the seed actually used (useful when seed=0 requests a random one).</summary>
    Task<int> GenerateAsync(string worldId, int width, int height, int seed = 0);
}

public class WorldGeneratorService : IWorldGeneratorService
{
    private readonly IWorldRepository _worldRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IEvolutionEntityRepository _entityRepo;
    private readonly ILogger<WorldGeneratorService> _logger;

    public WorldGeneratorService(
        IWorldRepository worldRepo,
        ICivilizationRepository civRepo,
        IEvolutionEntityRepository entityRepo,
        ILogger<WorldGeneratorService> logger)
    {
        _worldRepo = worldRepo;
        _civRepo = civRepo;
        _entityRepo = entityRepo;
        _logger = logger;
    }

    public async Task<int> GenerateAsync(string worldId, int width, int height, int seed = 0)
    {
        if (seed == 0) seed = Random.Shared.Next(1, 999999);
        _logger.LogInformation("Generating world {WorldId} ({W}x{H}) seed={Seed}", worldId, width, height, seed);

        var noise      = new PerlinNoise(seed);
        var ridgeNoise = new PerlinNoise(seed ^ 0x5EED1);
        var rng        = new Random(seed);

        // ── Stage 1+2: Continent mask + Elevation ──────────────
        var continentMask = GenerateContinentMask(width, height, rng);
        float[,] elevation = new float[width, height];
        float[,] ridge     = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float baseNoise = noise.Octave(x * 0.025f, y * 0.025f, octaves: 6, persistence: 0.5f);

                float r = ridgeNoise.Octave(x * 0.018f, y * 0.018f, octaves: 4, persistence: 0.55f);
                float ridgeValue = 1f - MathF.Abs(r * 2f - 1f);
                ridgeValue = MathF.Pow(ridgeValue, 2.2f);
                ridge[x, y] = ridgeValue;

                float e = continentMask[x, y] * 0.55f
                        + baseNoise * 0.30f
                        + ridgeValue * 0.15f;

                elevation[x, y] = Clamp01(e);
            }
        }

        // ── Stage 4: Moisture + Temperature ─────────────────────
        float[,] moisture    = new float[width, height];
        float[,] temperature = new float[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                moisture[x, y] = noise.Octave(x * 0.05f + 100f, y * 0.05f + 100f, octaves: 4, persistence: 0.6f);

                float latTemp = 1f - MathF.Abs((y - height * 0.5f) / (height * 0.5f));
                float elevCooling = elevation[x, y] > 0.65f ? (elevation[x, y] - 0.65f) * 0.8f : 0f;
                temperature[x, y] = Clamp01(latTemp - elevCooling + noise.Get(x * 0.03f + 200f, y * 0.03f + 200f) * 0.15f);
            }
        }

        // ── Stage 5: Biome classification ───────────────────────
        var tiles = new WorldTileData[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float e = elevation[x, y];
                float m = moisture[x, y];
                float t = temperature[x, y];

                var tileType = ClassifyBiome(e, m, t, ridge[x, y]);
                tiles[x, y] = new WorldTileData
                {
                    X = x,
                    Y = y,
                    Type = tileType,
                    Elevation = e,
                    Moisture = m,
                    Fertility = ComputeFertility(tileType, e, m),
                };
            }
        }

        // ── Stage 6: Rivers — gradient descent from peaks to sea ─
        CarveRivers(tiles, elevation, width, height, rng);

        // ── Stage 7: Coastline / Beach insertion ────────────────
        InsertCoastlines(tiles, width, height);

        // ── Stage 8: Biome smoothing (majority filter) ──────────
        SmoothBiomes(tiles, width, height, passes: 2);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                tiles[x, y].Fertility = ComputeFertility(tiles[x, y].Type, tiles[x, y].Elevation, tiles[x, y].Moisture);

        var tileList = Flatten(tiles, width, height);

        // ── Stage 9: Sacred sites ────────────────────────────────
        PlaceSacredSites(tileList, rng);

        await _worldRepo.UpdateTilesAsync(worldId, tileList);

        // ── Civilizations + entities ─────────────────────────────
        await SpawnCivilizationsOnTilesAsync(worldId, tileList, width, height);

        var evoService = new EvolutionEntitySpawner(_entityRepo, _logger);
        await evoService.SpawnAsync(worldId, tileList);

        int riverTiles = tileList.Count(t => t.Type == TileType.River);
        int beachTiles = tileList.Count(t => t.Type == TileType.Beach);
        _logger.LogInformation(
            "World generation complete: {Count} tiles ({Rivers} river, {Beach} beach)",
            tileList.Count, riverTiles, beachTiles);

        return seed;
    }

    // ─── Stage 1: Continent Mask ────────────────────────────────

    /// <summary>
    /// Generates a continent-shaped landmass instead of pure radial gradient.
    /// Places 2-4 "blob centers" with falloff, summed together, so the
    /// landmass has organic bays and peninsulas instead of a perfect circle.
    /// </summary>
    private static float[,] GenerateContinentMask(int width, int height, Random rng)
    {
        var mask = new float[width, height];

        int blobCount = rng.Next(2, 5);
        var blobs = new List<(float cx, float cy, float radius, float strength)>();
        for (int i = 0; i < blobCount; i++)
        {
            blobs.Add((
                cx: (float)(rng.NextDouble() * 0.6 + 0.2) * width,
                cy: (float)(rng.NextDouble() * 0.6 + 0.2) * height,
                radius: (float)(rng.NextDouble() * 0.25 + 0.25) * MathF.Min(width, height),
                strength: (float)(rng.NextDouble() * 0.4 + 0.8)
            ));
        }

        var warpNoise = new PerlinNoise(rng.Next());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float warpX = (warpNoise.Get(x * 0.04f, y * 0.04f) - 0.5f) * 18f;
                float warpY = (warpNoise.Get(x * 0.04f + 50f, y * 0.04f + 50f) - 0.5f) * 18f;

                float best = 0f;
                foreach (var (cx, cy, radius, strength) in blobs)
                {
                    float dx = (x + warpX) - cx;
                    float dy = (y + warpY) - cy;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);
                    float falloff = 1f - Clamp01(dist / radius);
                    falloff = MathF.Pow(falloff, 1.6f) * strength;
                    best = MathF.Max(best, falloff);
                }

                float edgeDistX = MathF.Min(x, width  - 1 - x) / (width  * 0.15f);
                float edgeDistY = MathF.Min(y, height - 1 - y) / (height * 0.15f);
                float edgeFalloff = Clamp01(MathF.Min(edgeDistX, edgeDistY));

                mask[x, y] = best * edgeFalloff;
            }
        }
        return mask;
    }

    // ─── Stage 5: Biome Classification ──────────────────────────

    private static TileType ClassifyBiome(float elevation, float moisture, float temperature, float ridgeValue)
    {
        if (elevation < 0.32f) return TileType.Water;

        if (elevation > 0.78f && ridgeValue > 0.6f)
            return temperature > 0.65f ? TileType.Volcano : TileType.Mountain;

        if (elevation > 0.68f) return TileType.Mountain;

        if (temperature > 0.72f && moisture < 0.32f) return TileType.Desert;
        if (temperature < 0.22f) return TileType.Tundra;
        if (moisture > 0.58f) return TileType.Forest;

        return TileType.Grassland;
    }

    private static float ComputeFertility(TileType type, float elevation, float moisture) => type switch
    {
        TileType.Grassland => 0.6f + moisture * 0.4f,
        TileType.Forest     => 0.5f + moisture * 0.3f,
        TileType.Desert     => 0.05f + moisture * 0.1f,
        TileType.Tundra     => 0.1f,
        TileType.Mountain   => 0.15f,
        TileType.Water      => 0f,
        TileType.Volcano    => 0.7f,
        TileType.Sacred     => 1f,
        TileType.Beach      => 0.25f,
        TileType.River      => 0.9f,
        _ => 0.3f
    };

    // ─── Stage 6: River Carving ──────────────────────────────────

    /// <summary>
    /// Rivers begin at high-elevation mountain tiles and flow downhill
    /// (steepest-descent gradient walk) until they reach water or stall.
    /// </summary>
    private static void CarveRivers(WorldTileData[,] tiles, float[,] elevation, int width, int height, Random rng)
    {
        var sources = new List<(int x, int y)>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (tiles[x, y].Type == TileType.Mountain && elevation[x, y] > 0.75f)
                    sources.Add((x, y));

        if (sources.Count == 0) return;

        int riverCount = Math.Clamp((width * height) / 1400, 3, 14);
        var chosen = sources.OrderBy(_ => rng.Next()).Take(riverCount).ToList();

        foreach (var (sx, sy) in chosen)
        {
            tiles[sx, sy].IsRiverSource = true;
            WalkRiverDownhill(tiles, elevation, width, height, sx, sy);
        }
    }

    private static void WalkRiverDownhill(
        WorldTileData[,] tiles, float[,] elevation, int width, int height, int startX, int startY)
    {
        int x = startX, y = startY;
        int maxSteps = width + height;
        var visited = new HashSet<(int, int)>();

        for (int step = 0; step < maxSteps; step++)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            if (!visited.Add((x, y))) return;

            if (tiles[x, y].Type is TileType.Water or TileType.River) return;

            if (tiles[x, y].Type != TileType.Mountain)
                tiles[x, y].Type = TileType.River;

            int bestX = x, bestY = y;
            float lowestElevation = elevation[x, y];

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;

                    if (elevation[nx, ny] < lowestElevation)
                    {
                        lowestElevation = elevation[nx, ny];
                        bestX = nx; bestY = ny;
                    }
                }
            }

            if (bestX == x && bestY == y) return;
            x = bestX; y = bestY;
        }
    }

    // ─── Stage 7: Coastline / Beach Insertion ───────────────────

    private static void InsertCoastlines(WorldTileData[,] tiles, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = tiles[x, y];
                if (tile.Type is TileType.Water or TileType.River) continue;
                if (tile.Type is TileType.Mountain or TileType.Volcano) continue;

                bool touchesWater = false;
                for (int dx = -1; dx <= 1 && !touchesWater; dx++)
                {
                    for (int dy = -1; dy <= 1 && !touchesWater; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        if (tiles[nx, ny].Type == TileType.Water) touchesWater = true;
                    }
                }

                if (touchesWater)
                {
                    tile.IsCoast = true;
                    if (tile.Type is TileType.Grassland or TileType.Forest or TileType.Desert)
                        tile.Type = TileType.Beach;
                }
            }
        }
    }

    // ─── Stage 8: Biome Smoothing ────────────────────────────────

    /// <summary>
    /// Majority filter: any tile surrounded mostly by a different biome
    /// gets converted to match its neighbours, removing single-tile noise
    /// specks for a more natural region look. Water/River/Mountain/Volcano/Sacred
    /// are protected since they shape the world's macro structure.
    /// </summary>
    private static void SmoothBiomes(WorldTileData[,] tiles, int width, int height, int passes)
    {
        var protectedTypes = new HashSet<TileType>
        {
            TileType.Water, TileType.River, TileType.Mountain, TileType.Volcano, TileType.Sacred
        };

        for (int pass = 0; pass < passes; pass++)
        {
            var snapshot = new TileType[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    snapshot[x, y] = tiles[x, y].Type;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (protectedTypes.Contains(snapshot[x, y])) continue;

                    var counts = new Dictionary<TileType, int>();
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                            var t = snapshot[nx, ny];
                            counts[t] = counts.GetValueOrDefault(t) + 1;
                        }
                    }

                    var majority = counts.OrderByDescending(kv => kv.Value).First();

                    if (majority.Value >= 6 && !protectedTypes.Contains(majority.Key))
                        tiles[x, y].Type = majority.Key;
                }
            }
        }
    }

    // ─── Stage 9: Sacred Sites ───────────────────────────────────

    private static void PlaceSacredSites(List<WorldTileData> tiles, Random rng)
    {
        int count = rng.Next(5, 9);

        var landTiles = tiles
            .Where(t => t.Type is TileType.Grassland or TileType.Forest)
            .OrderBy(_ => rng.Next())
            .Take(count)
            .ToList();

        foreach (var tile in landTiles)
            tile.Type = TileType.Sacred;
    }

    // ─── Civilization Placement ───────────────────────────────────

    private async Task SpawnCivilizationsOnTilesAsync(
        string worldId, List<WorldTileData> tiles, int width, int height)
    {
        var goodTiles = tiles
            .Where(t => t.Type is TileType.Grassland or TileType.Forest && t.Fertility > 0.5f)
            .ToList();

        if (!goodTiles.Any()) return;

        int civCount = Math.Min(8, goodTiles.Count / 10);
        civCount = Math.Max(3, civCount);

        var rng = new Random();
        var spawnPoints = new List<WorldTileData>();
        int minDist = Math.Max(8, width / 4);

        var scored = goodTiles
            .Select(t => (tile: t, score: t.Fertility + (NearRiverOrCoast(tiles, t, width, height) ? 0.3f : 0f)))
            .OrderByDescending(x => x.score)
            .Select(x => x.tile);

        foreach (var candidate in scored)
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

        foreach (var sp in spawnPoints)
        {
            var name = prefixes[rng.Next(prefixes.Length)] + suffixes[rng.Next(suffixes.Length)];
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

            foreach (var t in controlledTiles)
                t.CivilizationId = civ.Id;
        }

        await _worldRepo.UpdateTilesAsync(worldId, tiles);
        _logger.LogInformation("Spawned {Count} civilizations", spawnPoints.Count);
    }

    private static bool NearRiverOrCoast(List<WorldTileData> tiles, WorldTileData center, int width, int height)
    {
        for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
            {
                int nx = center.X + dx, ny = center.Y + dy;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                var t = tiles.FirstOrDefault(x => x.X == nx && x.Y == ny);
                if (t != null && (t.Type == TileType.River || t.IsCoast)) return true;
            }
        return false;
    }

    private static List<WorldTileData> GetNeighborLandTiles(
        List<WorldTileData> tiles, int cx, int cy, int width, int height, int radius)
    {
        return tiles
            .Where(t => t.Type != TileType.Water
                && t.Type != TileType.River
                && MathF.Abs(t.X - cx) <= radius
                && MathF.Abs(t.Y - cy) <= radius)
            .OrderBy(t => MathF.Pow(t.X - cx, 2) + MathF.Pow(t.Y - cy, 2))
            .Take(4)
            .ToList();
    }

    private static List<WorldTileData> Flatten(WorldTileData[,] tiles, int width, int height)
    {
        var list = new List<WorldTileData>(width * height);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                list.Add(tiles[x, y]);
        return list;
    }

    private static float Clamp01(float v) => MathF.Max(0f, MathF.Min(1f, v));
}

// ─── Perlin Noise Implementation ──────────────────────────────
/// <summary>
/// Self-contained 2D Perlin noise — no external dependency.
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

        return (res + 1f) * 0.5f;
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

// ─── Evolution Entity Spawner ──────────────────────────────────
internal class EvolutionEntitySpawner
{
    private readonly IEvolutionEntityRepository _repo;
    private readonly ILogger _logger;
    private readonly Random _rng = new();

    private static readonly Dictionary<EvolutionStage, float> StagePower = new()
    {
        { EvolutionStage.WildAnimal, 10f },
        { EvolutionStage.Monster,    20f },
        { EvolutionStage.HumanHero,  30f },
    };

    private static readonly Dictionary<EvolutionStage, string[]> Names = new()
    {
        { EvolutionStage.WildAnimal, new[] { "Wild Wolf", "Forest Bear", "Eagle", "Black Tiger" } },
        { EvolutionStage.Monster,    new[] { "Monster", "Water Beast", "Stone Giant" } },
        { EvolutionStage.HumanHero,  new[] { "Warrior", "Mage", "Knight" } },
    };

    public EvolutionEntitySpawner(IEvolutionEntityRepository repo, ILogger logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task SpawnAsync(string worldId, List<WorldTileData> tiles)
    {
        var spawns = new List<(EvolutionStage stage, TileType[] validTiles, int count)>
        {
            (EvolutionStage.WildAnimal, new[] { TileType.Forest, TileType.Grassland, TileType.River }, 12),
            (EvolutionStage.Monster,    new[] { TileType.Mountain, TileType.Tundra, TileType.Volcano }, 6),
            (EvolutionStage.HumanHero,  new[] { TileType.Sacred }, 4),
        };

        int total = 0;
        foreach (var (stage, validTypes, count) in spawns)
        {
            var candidates = tiles.Where(t => validTypes.Contains(t.Type))
                .OrderBy(_ => _rng.Next()).Take(count).ToList();

            foreach (var tile in candidates)
            {
                var nameList = Names[stage];
                await _repo.CreateAsync(new EvolutionEntityDocument
                {
                    WorldId = worldId,
                    Stage = stage,
                    Name = nameList[_rng.Next(nameList.Length)],
                    X = tile.X,
                    Y = tile.Y,
                    Power = StagePower[stage],
                    EvolutionPoints = 0
                });
                total++;
            }
        }
        _logger.LogInformation("Spawned {Count} evolution entities", total);
    }
}
