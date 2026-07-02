using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.WorldGen;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

public class WorldGeneratorServiceTests
{
    private readonly Mock<IWorldRepository>            _worldRepo  = new();
    private readonly Mock<ICivilizationRepository>     _civRepo    = new();
    private readonly Mock<IEvolutionEntityRepository>  _entityRepo = new();
    private readonly WorldGeneratorService _sut;

    // Captures the tiles passed to UpdateTilesAsync per worldId, since
    // GenerateAsync now returns the seed (int) rather than the tile list directly.
    private readonly Dictionary<string, List<WorldTileData>> _savedTiles = new();

    public WorldGeneratorServiceTests()
    {
        _worldRepo
            .Setup(r => r.UpdateTilesAsync(It.IsAny<string>(), It.IsAny<List<WorldTileData>>()))
            .Returns((string worldId, List<WorldTileData> tiles) =>
            {
                _savedTiles[worldId] = tiles; // last call wins (civ-placement re-saves the same list)
                return Task.CompletedTask;
            });
        _civRepo.Setup(r => r.CreateAsync(It.IsAny<CivilizationDocument>()))
            .ReturnsAsync((CivilizationDocument c) => c);
        _entityRepo.Setup(r => r.CreateAsync(It.IsAny<EvolutionEntityDocument>()))
            .ReturnsAsync((EvolutionEntityDocument e) => e);

        _sut = new WorldGeneratorService(
            _worldRepo.Object, _civRepo.Object, _entityRepo.Object,
            NullLogger<WorldGeneratorService>.Instance);
    }

    private async Task<List<WorldTileData>> GenerateAndGetTiles(string worldId, int w, int h, int seed)
    {
        await _sut.GenerateAsync(worldId, w, h, seed);
        return _savedTiles[worldId];
    }

    [Fact]
    public async Task GenerateAsync_ReturnsCorrectTileCount()
    {
        var tiles = await GenerateAndGetTiles("w1", 32, 32, seed: 12345);

        tiles.Should().HaveCount(32 * 32);
    }

    [Fact]
    public async Task GenerateAsync_WithExplicitSeed_ReturnsThatSameSeed()
    {
        var usedSeed = await _sut.GenerateAsync("w1", 32, 32, seed: 777);

        usedSeed.Should().Be(777, "passing a nonzero seed should be used as-is, not replaced");
    }

    [Fact]
    public async Task GenerateAsync_WithZeroSeed_ReturnsRandomNonzeroSeed()
    {
        var usedSeed = await _sut.GenerateAsync("w1", 32, 32, seed: 0);

        usedSeed.Should().NotBe(0, "seed=0 should be replaced with a randomly generated seed");
        usedSeed.Should().BeInRange(1, 999999);
    }

    [Fact]
    public async Task GenerateAsync_SameSeed_ProducesIdenticalTerrain()
    {
        var tilesA = await GenerateAndGetTiles("w1", 32, 32, seed: 555);
        var tilesB = await GenerateAndGetTiles("w2", 32, 32, seed: 555);

        var typesA = tilesA.OrderBy(t => t.X).ThenBy(t => t.Y).Select(t => t.Type).ToList();
        var typesB = tilesB.OrderBy(t => t.X).ThenBy(t => t.Y).Select(t => t.Type).ToList();

        typesA.Should().Equal(typesB, "same seed must produce deterministic, reproducible terrain");
    }

    [Fact]
    public async Task GenerateAsync_DifferentSeeds_ProduceDifferentTerrain()
    {
        var tilesA = await GenerateAndGetTiles("w1", 32, 32, seed: 1);
        var tilesB = await GenerateAndGetTiles("w2", 32, 32, seed: 2);

        var typesA = tilesA.OrderBy(t => t.X).ThenBy(t => t.Y).Select(t => t.Type).ToList();
        var typesB = tilesB.OrderBy(t => t.X).ThenBy(t => t.Y).Select(t => t.Type).ToList();

        typesA.Should().NotEqual(typesB, "different seeds should produce different worlds");
    }

    [Fact]
    public async Task GenerateAsync_ContainsWaterAndLand_NotAllOneType()
    {
        var tiles = await GenerateAndGetTiles("w1", 64, 64, seed: 42);

        tiles.Should().Contain(t => t.Type == TileType.Water, "world should have ocean");
        tiles.Should().Contain(t => t.Type == TileType.Grassland || t.Type == TileType.Forest,
            "world should have habitable land");
    }

    [Fact]
    public async Task GenerateAsync_HasMountains_FromRidgeNoise()
    {
        var tiles = await GenerateAndGetTiles("w1", 64, 64, seed: 99);

        tiles.Should().Contain(t => t.Type == TileType.Mountain,
            "ridge noise should produce mountain tiles");
    }

    [Fact]
    public async Task GenerateAsync_RiversFlowFromMountainsTowardLowerElevation()
    {
        var tiles = await GenerateAndGetTiles("w1", 64, 64, seed: 7);
        var riverTiles = tiles.Where(t => t.Type == TileType.River).ToList();

        if (riverTiles.Any())
        {
            var sources = tiles.Where(t => t.IsRiverSource).ToList();
            sources.Should().NotBeEmpty("if rivers exist, at least one source tile must be marked");
            foreach (var source in sources)
                source.Elevation.Should().BeGreaterThan(0.7f, "river sources must start at high elevation");
        }
    }

    [Fact]
    public async Task GenerateAsync_BeachTilesOnlyBorderWater()
    {
        var tiles = await GenerateAndGetTiles("w1", 64, 64, seed: 17);
        var tileGrid = tiles.ToDictionary(t => (t.X, t.Y));
        var beachTiles = tiles.Where(t => t.Type == TileType.Beach).ToList();

        foreach (var beach in beachTiles)
        {
            bool touchesWater = false;
            for (int dx = -1; dx <= 1 && !touchesWater; dx++)
            for (int dy = -1; dy <= 1 && !touchesWater; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (tileGrid.TryGetValue((beach.X + dx, beach.Y + dy), out var neighbour)
                    && neighbour.Type == TileType.Water)
                    touchesWater = true;
            }
            touchesWater.Should().BeTrue("every Beach tile must be adjacent to Water");
        }
    }

    [Fact]
    public async Task GenerateAsync_NoOrphanSingleTileBiomeSpecks()
    {
        var tiles = await GenerateAndGetTiles("w1", 64, 64, seed: 321);
        var tileGrid = tiles.ToDictionary(t => (t.X, t.Y));
        var protectedTypes = new HashSet<TileType>
        {
            TileType.Water, TileType.River, TileType.Mountain, TileType.Volcano, TileType.Sacred
        };

        int speckleCount = 0;
        int checkedCount = 0;

        foreach (var tile in tiles.Where(t => !protectedTypes.Contains(t.Type)))
        {
            checkedCount++;
            int sameTypeNeighbours = 0;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (tileGrid.TryGetValue((tile.X + dx, tile.Y + dy), out var n) && n.Type == tile.Type)
                    sameTypeNeighbours++;
            }
            if (sameTypeNeighbours == 0) speckleCount++;
        }

        float speckleRatio = checkedCount == 0 ? 0 : (float)speckleCount / checkedCount;
        speckleRatio.Should().BeLessThan(0.05f,
            "after smoothing, fewer than 5% of tiles should be isolated single-tile specks");
    }

    [Fact]
    public async Task GenerateAsync_PlacesSacredSites()
    {
        var tiles = await GenerateAndGetTiles("w1", 64, 64, seed: 88);

        tiles.Should().Contain(t => t.Type == TileType.Sacred, "world must contain Sacred sites");
        tiles.Count(t => t.Type == TileType.Sacred).Should().BeInRange(5, 8,
            "5 to 8 sacred sites should be placed per GDD");
    }

    [Fact]
    public async Task GenerateAsync_FertilityMatchesTileType()
    {
        var tiles = await GenerateAndGetTiles("w1", 64, 64, seed: 200);

        foreach (var water in tiles.Where(t => t.Type == TileType.Water))
            water.Fertility.Should().Be(0f, "water tiles have zero fertility");

        foreach (var river in tiles.Where(t => t.Type == TileType.River))
            river.Fertility.Should().BeGreaterThan(0.8f, "riverside land should be highly fertile");

        foreach (var sacred in tiles.Where(t => t.Type == TileType.Sacred))
            sacred.Fertility.Should().Be(1f, "sacred sites have maximum fertility");
    }

    [Fact]
    public async Task GenerateAsync_LargeMap_CompletesWithoutError()
    {
        // Verifies the 128x128 default map size works end-to-end without timeout/exception
        var tiles = await GenerateAndGetTiles("w1", 128, 128, seed: 2024);

        tiles.Should().HaveCount(128 * 128);
    }

    [Fact]
    public async Task GenerateAsync_SpawnsCivilizationsOnFertileLand()
    {
        _civRepo.Invocations.Clear();
        await _sut.GenerateAsync("w1", 64, 64, seed: 50);

        _civRepo.Verify(r => r.CreateAsync(It.IsAny<CivilizationDocument>()), Times.AtLeastOnce,
            "world generation should spawn at least one civilization on fertile land");
    }

    [Fact]
    public void PerlinNoise_SameInputSameSeed_IsDeterministic()
    {
        var noiseA = new PerlinNoise(42);
        var noiseB = new PerlinNoise(42);

        for (float x = 0; x < 10; x += 1.3f)
        {
            float a = noiseA.Get(x, x * 0.5f);
            float b = noiseB.Get(x, x * 0.5f);
            a.Should().BeApproximately(b, 0.0001f);
        }
    }

    [Fact]
    public void PerlinNoise_OutputIsNormalized0To1()
    {
        var noise = new PerlinNoise(7);
        for (float x = -20; x < 20; x += 2.7f)
        for (float y = -20; y < 20; y += 3.1f)
        {
            float v = noise.Get(x, y);
            v.Should().BeInRange(0f, 1f);
        }
    }
}
