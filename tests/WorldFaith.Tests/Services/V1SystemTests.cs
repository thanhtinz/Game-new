using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Server.Services.Dungeon;
using WorldFaith.Server.Services.Faith;
using WorldFaith.Server.Services.Race;
using WorldFaith.Server.Services.Religion;
using WorldFaith.Server.Services.Simulation;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

// ─── Race Faith Affinity Tests ───────────────────────────

public class RaceAffinityServiceTests
{
    private readonly Mock<IRaceRepository> _raceRepo = new();
    private readonly RaceAffinityService _sut;

    public RaceAffinityServiceTests()
    {
        _raceRepo.Setup(r => r.GetByTypeAsync(It.IsAny<string>(), It.IsAny<RaceType>()))
            .ReturnsAsync((RaceDocument?)null);
        _sut = new RaceAffinityService(_raceRepo.Object, NullLogger<RaceAffinityService>.Instance);
    }

    [Theory]
    [InlineData(RaceType.Elf,   GodArchetype.Nature,    160f)]  // Deep harmony
    [InlineData(RaceType.Elf,   GodArchetype.Darkness,  40f)]   // Rejected
    [InlineData(RaceType.Orc,   GodArchetype.War,       160f)]  // Deep harmony
    [InlineData(RaceType.Angel, GodArchetype.Light,     160f)]  // Deep harmony
    [InlineData(RaceType.Angel, GodArchetype.Darkness,  10f)]   // Deep taboo
    [InlineData(RaceType.Demon, GodArchetype.Light,     20f)]   // Taboo
    [InlineData(RaceType.Undead,GodArchetype.Death,     160f)]  // Deep harmony
    [InlineData(RaceType.Human, GodArchetype.Order,     140f)]  // Preferred
    public async Task GetAffinity_ReturnsCorrectDefault(RaceType race, GodArchetype domain, float expected)
    {
        var result = await _sut.GetAffinityAsync("w1", race, domain);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetConversionModifier_GeniusOrc_BoostsKnowledge()
    {
        // Orc base Knowledge = 40%, Genius trait adds +50% = 90%
        var modifier = await _sut.GetConversionModifierAsync(
            "w1", RaceType.Orc, GodArchetype.Knowledge,
            new List<RaceTrait> { RaceTrait.Genius });

        modifier.Should().BeApproximately(0.9f, 0.05f, "Genius Orc should have 90% Knowledge affinity");
    }

    [Fact]
    public async Task GetConversionModifier_FanaticTrait_AmplifiesBase()
    {
        // Elf Nature base 160%, Fanatic amplifies by 1.3x → 208% but capped at 200%
        var modifier = await _sut.GetConversionModifierAsync(
            "w1", RaceType.Elf, GodArchetype.Nature,
            new List<RaceTrait> { RaceTrait.Fanatic });

        modifier.Should().BeGreaterThan(1.5f, "Fanatic Elf should have boosted Nature affinity");
    }

    [Fact]
    public void GetAffinityTier_CorrectClassification()
    {
        RaceAffinityService.GetAffinityTier(160f).Should().Be(AffinityTier.DeepHarmony);
        RaceAffinityService.GetAffinityTier(130f).Should().Be(AffinityTier.Preferred);
        RaceAffinityService.GetAffinityTier(100f).Should().Be(AffinityTier.Neutral);
        RaceAffinityService.GetAffinityTier(70f) .Should().Be(AffinityTier.Difficult);
        RaceAffinityService.GetAffinityTier(40f) .Should().Be(AffinityTier.Rejected);
        RaceAffinityService.GetAffinityTier(15f) .Should().Be(AffinityTier.Taboo);
    }

    [Fact]
    public async Task SeedRaceData_CreatesAllEightRaces()
    {
        _raceRepo.Setup(r => r.GetByWorldAsync("w1")).ReturnsAsync(new List<RaceDocument>());
        _raceRepo.Setup(r => r.CreateAsync(It.IsAny<RaceDocument>()))
            .ReturnsAsync((RaceDocument r) => r);

        await _sut.SeedRaceDataAsync("w1");

        _raceRepo.Verify(r => r.CreateAsync(It.IsAny<RaceDocument>()), Times.Exactly(8));
    }

    [Fact]
    public async Task RecordEnvironmentalMemory_UpdatesRaceDoc()
    {
        var raceDoc = new RaceDocument { WorldId = "w1", Type = RaceType.Orc };
        _raceRepo.Setup(r => r.GetByTypeAsync("w1", RaceType.Orc)).ReturnsAsync(raceDoc);
        _raceRepo.Setup(r => r.UpdateAsync(It.IsAny<RaceDocument>())).Returns(Task.CompletedTask);

        await _sut.RecordEnvironmentalMemoryAsync("w1", RaceType.Orc, "god1", -15f, "God flooded orc camp");

        raceDoc.EnvironmentalMemory.Should().ContainKey("god_god1");
        raceDoc.EnvironmentalMemory["god_god1"].Should().Be(-15f);
    }
}

// ─── Dungeon Service Tests ────────────────────────────────

public class DungeonServiceTests
{
    private readonly Mock<IDungeonRepository>      _dungeonRepo  = new();
    private readonly Mock<IRelicRepository>        _relicRepo    = new();
    private readonly Mock<IGuildMissionRepository> _missionRepo  = new();
    private readonly Mock<INpcRepository>          _npcRepo      = new();
    private readonly Mock<IOrganizationRepository> _orgRepo      = new();
    private readonly Mock<IGodRepository>          _godRepo      = new();
    private readonly Mock<ICivilizationRepository> _civRepo      = new();
    private readonly Mock<IBalanceConfigService>   _balance      = new();
    private readonly DungeonService _sut;

    public DungeonServiceTests()
    {
        _dungeonRepo.Setup(r => r.CreateAsync(It.IsAny<DungeonDocument>()))
            .ReturnsAsync((DungeonDocument d) => d);
        _relicRepo.Setup(r => r.CreateAsync(It.IsAny<RelicDocument>()))
            .ReturnsAsync((RelicDocument r) => r);
        _relicRepo.Setup(r => r.UpdateAsync(It.IsAny<RelicDocument>()))
            .Returns(Task.CompletedTask);
        _godRepo.Setup(r => r.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new GodDocument { Name = "TestGod" });
        _godRepo.Setup(r => r.UpdateAsync(It.IsAny<GodDocument>()))
            .Returns(Task.CompletedTask);
        _balance.Setup(b => b.GetFloatAsync(It.IsAny<string>())).ReturnsAsync(0.1f);

        _sut = new DungeonService(_dungeonRepo.Object, _relicRepo.Object, _missionRepo.Object,
            _npcRepo.Object, _orgRepo.Object, _godRepo.Object, _civRepo.Object,
            _balance.Object, NullLogger<DungeonService>.Instance);
    }

    [Theory]
    [InlineData(DungeonType.AncientRuins)]
    [InlineData(DungeonType.MonstersLair)]
    [InlineData(DungeonType.DarkPortal)]
    [InlineData(DungeonType.ForbiddenSanctum)]
    [InlineData(DungeonType.LostTemple)]
    public async Task SpawnDungeon_AllTypes_Created(DungeonType type)
    {
        var dungeon = await _sut.SpawnDungeonAsync("w1", 10, 10, type, "god1");

        dungeon.Should().NotBeNull();
        dungeon.Type.Should().Be(type);
        dungeon.WorldId.Should().Be("w1");
        dungeon.DangerLevel.Should().BeGreaterThan(0f);
    }

    [Fact]
    public async Task SpawnDungeon_WithGodId_LinksToGod()
    {
        var dungeon = await _sut.SpawnDungeonAsync("w1", 5, 5, DungeonType.LostTemple, "god1");

        dungeon.OriginGodId.Should().Be("god1");
    }

    [Fact]
    public async Task CreateRelic_WithGodId_HasFaithBonus()
    {
        var relic = await _sut.CreateRelicAsync("w1", "god1", RelicType.FaithCrystal);

        relic.FaithBonus.Should().BeGreaterThan(0f);
        relic.OriginGodId.Should().Be("god1");
        relic.IsActive.Should().BeTrue();
    }
}

// ─── Doctrine Service Tests ───────────────────────────────

public class DoctrineServiceTests
{
    private readonly Mock<IReligionRepository> _religionRepo = new();
    private readonly DoctrineService _sut;

    private ReligionDocument MakeReligion(string id = "r1") => new ReligionDocument
    {
        Id = id, WorldId = "w1", Name = "TestFaith", GodId = "g1", Doctrine = new DoctrineValues()
    };

    public DoctrineServiceTests()
    {
        _sut = new DoctrineService(_religionRepo.Object, NullLogger<DoctrineService>.Instance);
    }

    [Fact]
    public async Task MissionarySpeed_FullExpansion_Returns2x()
    {
        _religionRepo.Setup(r => r.GetByIdAsync("r1"))
            .ReturnsAsync(MakeReligion() with { Doctrine = new DoctrineValues { IsolationVsExpansion = 100f } });

        var speed = await _sut.GetMissionarySpeedModifierAsync("r1");
        speed.Should().Be(2.0f);
    }

    [Fact]
    public async Task MissionarySpeed_FullIsolation_Returns0_5x()
    {
        _religionRepo.Setup(r => r.GetByIdAsync("r1"))
            .ReturnsAsync(MakeReligion() with { Doctrine = new DoctrineValues { IsolationVsExpansion = -100f } });

        var speed = await _sut.GetMissionarySpeedModifierAsync("r1");
        speed.Should().Be(0.0f);
    }

    [Fact]
    public async Task RaceCompatibility_HarmonyDoctrine_ElfGetsBonus()
    {
        _religionRepo.Setup(r => r.GetByIdAsync("r1"))
            .ReturnsAsync(MakeReligion() with { Doctrine = new DoctrineValues { HarmonyVsDominion = -100f } });

        var compat = await _sut.GetRaceCompatibilityModifierAsync("r1", RaceType.Elf);
        compat.Should().BeGreaterThan(1.0f, "Elves love Harmony doctrine");
    }

    [Fact]
    public async Task RaceCompatibility_DominionDoctrine_OrcGetsBonus()
    {
        _religionRepo.Setup(r => r.GetByIdAsync("r1"))
            .ReturnsAsync(MakeReligion() with { Doctrine = new DoctrineValues { HarmonyVsDominion = 100f } });

        var compat = await _sut.GetRaceCompatibilityModifierAsync("r1", RaceType.Orc);
        compat.Should().BeGreaterThan(1.0f, "Orcs prefer Dominion doctrine");
    }

    [Fact]
    public async Task ShouldExecuteHeretic_HighPunishmentHighOrder_ReturnsTrue()
    {
        _religionRepo.Setup(r => r.GetByIdAsync("r1"))
            .ReturnsAsync(MakeReligion() with
            {
                Doctrine = new DoctrineValues { MercyVsPunishment = 70f, FreedomVsOrder = 60f }
            });

        var result = await _sut.ShouldExecuteHereticAsync("r1");
        result.Should().BeTrue("High Punishment + Order should execute heretics");
    }

    [Fact]
    public async Task ShouldExecuteHeretic_MercyDoctrine_ReturnsFalse()
    {
        _religionRepo.Setup(r => r.GetByIdAsync("r1"))
            .ReturnsAsync(MakeReligion() with
            {
                Doctrine = new DoctrineValues { MercyVsPunishment = -80f }
            });

        var result = await _sut.ShouldExecuteHereticAsync("r1");
        result.Should().BeFalse("Mercy doctrine should not execute heretics");
    }
}

// ─── Government Service Tests ─────────────────────────────

public class GovernmentServiceTests
{
    private readonly Mock<ICivilizationRepository> _civRepo = new();
    private readonly GovernmentService _sut;

    public GovernmentServiceTests()
    {
        _civRepo.Setup(r => r.UpdateAsync(It.IsAny<CivilizationDocument>())).Returns(Task.CompletedTask);
        _sut = new GovernmentService(_civRepo.Object, NullLogger<GovernmentService>.Instance);
    }

    [Theory]
    [InlineData(GovernmentType.Monarchy,     1.5f)]   // Fast policy
    [InlineData(GovernmentType.Theocracy,    1.0f)]
    [InlineData(GovernmentType.NobleCouncil, 0.7f)]   // Slow
    [InlineData(GovernmentType.MonsterHorde, 1.8f)]   // Fastest
    public void GetBehavior_PolicySpeed_CorrectPerType(GovernmentType gov, float expectedSpeed)
    {
        var behavior = _sut.GetBehavior(gov);
        behavior.PolicySpeed.Should().Be(expectedSpeed);
    }

    [Fact]
    public async Task EvolveGovernment_TribalToMonarchy_WhenPopGrows()
    {
        var civ = new CivilizationDocument
        {
            Id = "c1", Name = "Test",
            Government = GovernmentType.TribalClan,
            Population = 600,  // > 500 threshold
            Economy = 50f
        };

        await _sut.EvolveGovernmentAsync(civ);
        civ.Government.Should().Be(GovernmentType.Monarchy);
    }

    [Fact]
    public void GetRebellionRisk_LowStabilityLowFood_HighRisk()
    {
        var civ = new CivilizationDocument
        {
            Stability = 15f,  // < 30
            Food = 5f,        // famine
            ReligiousUnity = 10f,
            Government = GovernmentType.NobleCouncil  // x1.3 modifier
        };

        var risk = _sut.GetRebellionRisk(civ);
        risk.Should().BeGreaterThan(0.3f, "Low stability + famine = high rebellion risk");
    }

    [Fact]
    public void GetRoyalConversionImpact_Theocracy_MaxImpact()
    {
        var impact = _sut.GetRoyalConversionImpact(GovernmentType.Theocracy);
        impact.Should().Be(1.0f, "High Priest conversion = maximum kingdom impact");
    }
}

// ─── Believer Type Service Tests ─────────────────────────

public class BelieverTypeServiceTests
{
    private readonly Mock<IReligionRepository> _religionRepo = new();
    private readonly BelieverTypeService _sut;

    public BelieverTypeServiceTests()
    {
        _sut = new BelieverTypeService(_religionRepo.Object, NullLogger<BelieverTypeService>.Instance);
    }

    [Fact]
    public void GetDistribution_NoStored_UsesDefaultRatios()
    {
        var religion = new ReligionDocument { FollowerCount = 100 };

        var (casual, devout, fanatic, cultist, heretic) = _sut.GetDistribution(religion);

        casual.Should().Be(50);
        devout.Should().Be(35);
        fanatic.Should().Be(10);
        cultist.Should().Be(3);
        heretic.Should().Be(2);
    }

    [Fact]
    public async Task CalculateFaith_FanaticHeavy_HighOutput()
    {
        var religion = new ReligionDocument
        {
            FollowerCount = 100,
            CasualCount = 0, DevoutCount = 0, FanaticCount = 100,
            CultistCount = 0, HereticCount = 0,
            DevotionLevel = 1.0f
        };

        var faith = await _sut.CalculateFaithFromBelieversAsync(religion);
        faith.Should().Be(2.0f, "100 Fanatics × 2.0x output × 1.0 devotion = 2.0");
    }

    [Fact]
    public async Task ShiftBelieverTypes_MiracleSuccess_CasualBecomesDevout()
    {
        var religion = new ReligionDocument
        {
            Id = "r1", Name = "Test",
            FollowerCount = 100,
            CasualCount = 50, DevoutCount = 30, FanaticCount = 10,
            CultistCount = 5, HereticCount = 5
        };

        _religionRepo.Setup(r => r.GetByIdAsync("r1")).ReturnsAsync(religion);
        _religionRepo.Setup(r => r.UpdateAsync(It.IsAny<ReligionDocument>())).Returns(Task.CompletedTask);

        await _sut.ShiftBelieverTypesAsync("r1", "MiracleSuccess", 1f);

        religion.CasualCount.Should().BeLessThan(50, "Casuals should become Devout after miracle");
        religion.DevoutCount.Should().BeGreaterThan(30, "Devout count should increase");
    }

    [Fact]
    public async Task ShiftBelieverTypes_HolyWar_DevoutBecomeFanatic()
    {
        var religion = new ReligionDocument
        {
            Id = "r1", Name = "Test",
            FollowerCount = 100,
            CasualCount = 20, DevoutCount = 60, FanaticCount = 10,
            CultistCount = 5, HereticCount = 5
        };

        _religionRepo.Setup(r => r.GetByIdAsync("r1")).ReturnsAsync(religion);
        _religionRepo.Setup(r => r.UpdateAsync(It.IsAny<ReligionDocument>())).Returns(Task.CompletedTask);

        await _sut.ShiftBelieverTypesAsync("r1", "HolyWar", 1f);

        religion.FanaticCount.Should().BeGreaterThan(10, "Holy War should create more Fanatics");
        religion.DevoutCount.Should().BeLessThan(60, "Devout become Fanatic during Holy War");
    }

    [Fact]
    public async Task ShiftBelieverTypes_Persecution_DevoutBecomeCultist()
    {
        var religion = new ReligionDocument
        {
            Id = "r1", Name = "Test",
            FollowerCount = 100,
            CasualCount = 10, DevoutCount = 70, FanaticCount = 10,
            CultistCount = 5, HereticCount = 5
        };

        _religionRepo.Setup(r => r.GetByIdAsync("r1")).ReturnsAsync(religion);
        _religionRepo.Setup(r => r.UpdateAsync(It.IsAny<ReligionDocument>())).Returns(Task.CompletedTask);

        await _sut.ShiftBelieverTypesAsync("r1", "Persecution", 1f);

        religion.CultistCount.Should().BeGreaterThan(5, "Persecution drives believers underground");
    }
}

// ─── God Rank Service Tests ───────────────────────────────

public class GodRankServiceTests
{
    private readonly Mock<IGodRepository>      _godRepo     = new();
    private readonly Mock<IRelicRepository>    _relicRepo   = new();
    private readonly Mock<IReligionRepository> _religionRepo= new();
    private readonly GodRankService _sut;

    public GodRankServiceTests()
    {
        _godRepo.Setup(r => r.UpdateAsync(It.IsAny<GodDocument>())).Returns(Task.CompletedTask);
        _sut = new GodRankService(_godRepo.Object, _relicRepo.Object, _religionRepo.Object,
            NullLogger<GodRankService>.Instance);
    }

    [Theory]
    [InlineData(0,         GodRank.Nascent)]
    [InlineData(5000,      GodRank.Awakened)]
    [InlineData(25000,     GodRank.Established)]
    [InlineData(100000,    GodRank.Revered)]
    [InlineData(400000,    GodRank.Exalted)]
    [InlineData(1000000,   GodRank.Ancient)]
    public async Task UpdateRank_CorrectRankAtThreshold(int totalFaith, GodRank expectedRank)
    {
        var god = new GodDocument { Id = "g1", Name = "TestGod", RankData = new GodRankData { TotalFaithEarned = totalFaith } };
        _godRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(god);

        await _sut.UpdateRankAsync("g1");

        god.RankData.Rank.Should().Be(expectedRank);
    }

    [Fact]
    public async Task CheckForgotten_HasRelics_SurvivesAsForgotten()
    {
        var god = new GodDocument { Id = "g1", Name = "TestGod", FollowerCount = 0, IsAlive = true };
        _godRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(god);
        _relicRepo.Setup(r => r.GetByGodAsync("w1", "g1"))
            .ReturnsAsync(new List<RelicDocument> { new RelicDocument { IsActive = true, FaithBonus = 5f } });
        _religionRepo.Setup(r => r.GetByGodAsync("g1")).ReturnsAsync(new List<ReligionDocument>());

        var survived = await _sut.CheckForgottenStateAsync("w1", "g1");

        survived.Should().BeTrue("God with relics should survive as Forgotten");
        god.IsForgotten.Should().BeTrue();
        god.IsAlive.Should().BeTrue();
    }

    [Fact]
    public async Task CheckForgotten_NoRelicsNoCults_Eliminated()
    {
        var god = new GodDocument { Id = "g1", Name = "TestGod", FollowerCount = 0, IsAlive = true };
        _godRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(god);
        _relicRepo.Setup(r => r.GetByGodAsync("w1", "g1")).ReturnsAsync(new List<RelicDocument>());
        _religionRepo.Setup(r => r.GetByGodAsync("g1")).ReturnsAsync(new List<ReligionDocument>());

        var survived = await _sut.CheckForgottenStateAsync("w1", "g1");

        survived.Should().BeFalse("God with no relics or cults should be eliminated");
        god.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void GodRankData_Multipliers_CorrectValues()
    {
        new GodRankData { Rank = GodRank.Forgotten }  .RankMultiplier.Should().Be(0.1f);
        new GodRankData { Rank = GodRank.Nascent }    .RankMultiplier.Should().Be(1.0f);
        new GodRankData { Rank = GodRank.Ancient }    .RankMultiplier.Should().Be(3.0f);
    }
}
