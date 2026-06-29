using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Simulation;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

public class ScenarioControllerTests
{
    private readonly Mock<IGodRepository>              _godRepo     = new();
    private readonly Mock<IReligionRepository>         _religionRepo= new();
    private readonly Mock<IEvolutionEntityRepository>  _entityRepo  = new();
    private readonly Mock<IWorldRepository>            _worldRepo   = new();
    private readonly ScenarioController                _sut;

    public ScenarioControllerTests()
    {
        _sut = new ScenarioController(
            _godRepo.Object,
            _religionRepo.Object,
            _entityRepo.Object,
            _worldRepo.Object,
            NullLogger<ScenarioController>.Instance);
    }

    private GodDocument AliveGod(string id, string playerId, int followers = 100)
        => new() { Id = id, PlayerId = playerId, IsAlive = true, FollowerCount = followers };

    private GodDocument DeadGod(string id, string playerId)
        => new() { Id = id, PlayerId = playerId, IsAlive = true, FollowerCount = 0 };

    // ─── Standard: Last God Standing ─────────────────────────

    [Fact]
    public async Task CheckWinCondition_Standard_OneAliveGod_Wins()
    {
        _godRepo.Setup(r => r.GetByWorldAsync("w1"))
            .ReturnsAsync(new List<GodDocument>
            {
                AliveGod("g1", "p1", 500),
                DeadGod("g2", "p2")
            });

        var cfg = ScenarioConfigs.All[ScenarioType.Standard];
        var (ended, winnerId, reason) = await _sut.CheckWinConditionAsync("w1", 1, 1, cfg);

        ended.Should().BeTrue();
        winnerId.Should().Be("p1");
        reason.Should().Contain("cuối cùng");
    }

    [Fact]
    public async Task CheckWinCondition_Standard_MultipleAlive_NotEnded()
    {
        _godRepo.Setup(r => r.GetByWorldAsync("w1"))
            .ReturnsAsync(new List<GodDocument>
            {
                AliveGod("g1", "p1", 500),
                AliveGod("g2", "p2", 300),
            });

        var cfg = ScenarioConfigs.All[ScenarioType.Standard];
        var (ended, _, _) = await _sut.CheckWinConditionAsync("w1", 500, 1, cfg);

        ended.Should().BeFalse();
    }

    // ─── Religion Wars ────────────────────────────────────────

    [Fact]
    public async Task CheckWinCondition_ReligionWars_DominantReligion_Wins()
    {
        var gods = new List<GodDocument> { AliveGod("g1", "p1"), AliveGod("g2", "p2") };
        _godRepo.Setup(r => r.GetByWorldAsync("w1")).ReturnsAsync(gods);

        // God 1 has 800 followers, god 2 has 100 → g1 = 88.8% > 70%
        _religionRepo.Setup(r => r.GetByWorldAsync("w1"))
            .ReturnsAsync(new List<ReligionDocument>
            {
                new() { GodId = "g1", FollowerCount = 800 },
                new() { GodId = "g2", FollowerCount = 100 },
            });

        var cfg = ScenarioConfigs.All[ScenarioType.ReligionWars];
        var (ended, winnerId, reason) = await _sut.CheckWinConditionAsync("w1", 1, 1, cfg);

        ended.Should().BeTrue();
        winnerId.Should().Be("p1");
        reason.Should().Contain("70%").Or.Contain("%");
    }

    [Fact]
    public async Task CheckWinCondition_ReligionWars_NoDominant_NotEnded()
    {
        var gods = new List<GodDocument> { AliveGod("g1", "p1"), AliveGod("g2", "p2") };
        _godRepo.Setup(r => r.GetByWorldAsync("w1")).ReturnsAsync(gods);

        // 60-40 split — no one has 70%
        _religionRepo.Setup(r => r.GetByWorldAsync("w1"))
            .ReturnsAsync(new List<ReligionDocument>
            {
                new() { GodId = "g1", FollowerCount = 600 },
                new() { GodId = "g2", FollowerCount = 400 },
            });

        var cfg = ScenarioConfigs.All[ScenarioType.ReligionWars];
        var (ended, _, _) = await _sut.CheckWinConditionAsync("w1", 1, 1, cfg);

        ended.Should().BeFalse();
    }

    // ─── Evolution Race ───────────────────────────────────────

    [Fact]
    public async Task CheckWinCondition_EvolutionRace_ApexEntity_OwnerWins()
    {
        var gods = new List<GodDocument> { AliveGod("g1", "p1"), AliveGod("g2", "p2") };
        _godRepo.Setup(r => r.GetByWorldAsync("w1")).ReturnsAsync(gods);
        _religionRepo.Setup(r => r.GetByWorldAsync("w1")).ReturnsAsync(new List<ReligionDocument>());

        _entityRepo.Setup(r => r.GetByWorldAsync("w1"))
            .ReturnsAsync(new List<EvolutionEntityDocument>
            {
                new() { Stage = EvolutionStage.CelestialGuardian, GodInfluenceId = "g1" }
            });

        var cfg = ScenarioConfigs.All[ScenarioType.EvolutionRace];
        var (ended, winnerId, _) = await _sut.CheckWinConditionAsync("w1", 1, 1, cfg);

        ended.Should().BeTrue();
        winnerId.Should().Be("p1");
    }

    // ─── Max Cycles ──────────────────────────────────────────

    [Fact]
    public async Task CheckWinCondition_MaxCyclesReached_MostFollowersWins()
    {
        var gods = new List<GodDocument>
        {
            AliveGod("g1", "p1", 1000),
            AliveGod("g2", "p2", 200),
        };
        _godRepo.Setup(r => r.GetByWorldAsync("w1")).ReturnsAsync(gods);

        var cfg = ScenarioConfigs.All[ScenarioType.FaithCrisis]; // MaxCycles=3
        var (ended, winnerId, reason) = await _sut.CheckWinConditionAsync("w1", 1, 3, cfg);

        ended.Should().BeTrue();
        winnerId.Should().Be("p1"); // 1000 followers > 200
    }

    // ─── Configs ─────────────────────────────────────────────

    [Fact]
    public void ScenarioConfigs_AllScenariosHaveValidConfig()
    {
        foreach (ScenarioType type in Enum.GetValues<ScenarioType>())
        {
            ScenarioConfigs.All.Should().ContainKey(type, $"missing config for {type}");
            var cfg = ScenarioConfigs.All[type];
            cfg.Name.Should().NotBeNullOrEmpty();
            cfg.Description.Should().NotBeNullOrEmpty();
            cfg.FaithMultiplier.Should().BeGreaterThan(0f);
        }
    }
}
