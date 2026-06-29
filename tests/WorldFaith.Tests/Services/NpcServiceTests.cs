using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

// ─── Doctrine Integrity Tests ─────────────────────────────

public class DoctrineIntegrityServiceTests
{
    private readonly Mock<INpcRepository>      _npcRepo      = new();
    private readonly Mock<IGodRepository>      _godRepo      = new();
    private readonly Mock<IReligionRepository> _religionRepo = new();
    private readonly DoctrineIntegrityService _sut;

    public DoctrineIntegrityServiceTests()
    {
        _npcRepo.Setup(r => r.UpdateAsync(It.IsAny<NpcDocument>())).Returns(Task.CompletedTask);
        _sut = new DoctrineIntegrityService(
            _npcRepo.Object, _godRepo.Object, _religionRepo.Object,
            NullLogger<DoctrineIntegrityService>.Instance);
    }

    [Fact]
    public async Task ApplyViolation_MinorContradiction_SmallIntegrityLoss()
    {
        var npc = MakeVip("n1", ChurchRank.Priest);
        npc.DivineProfile.DoctrineIntegrity.Score = 80f;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        var result = await _sut.ApplyViolationAsync("n1", "w1",
            ViolationSeverity.MinorContradiction, "Minor slip", false, null, 10);

        result.Should().BeLessThan(80f, "integrity should decrease");
        result.Should().BeGreaterThan(70f, "minor violation should not be catastrophic");
    }

    [Fact]
    public async Task ApplyViolation_DoctrineInversion_CatastrophicLoss()
    {
        var npc = MakeVip("n1", ChurchRank.Saint);
        npc.DivineProfile.DoctrineIntegrity.Score = 90f;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        var result = await _sut.ApplyViolationAsync("n1", "w1",
            ViolationSeverity.DoctrineInversion, "Full doctrine betrayal", true, null, 20);

        result.Should().BeLessThan(20f, "doctrine inversion should devastate integrity");
    }

    [Fact]
    public async Task ApplyViolation_PublicScandal_DecreasesDevotionLevel()
    {
        var npc = MakeVip("n1", ChurchRank.HighPriest);
        npc.DevotionLevel = 0.8f;
        npc.DivineProfile.DoctrineIntegrity.Score = 75f;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.ApplyViolationAsync("n1", "w1",
            ViolationSeverity.MajorViolation, "Public betrayal", true, null, 30);

        npc.DevotionLevel.Should().BeLessThan(0.8f, "public scandal should reduce devotion");
    }

    [Fact]
    public async Task ApplyResistance_IncreasesIntegrityAndTrust()
    {
        var npc = MakeVip("n1", ChurchRank.Priest);
        npc.DivineProfile.DoctrineIntegrity.Score = 65f;
        npc.GodTrustLevel = 50f;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        var result = await _sut.ApplyResistanceAsync("n1", "Resisted temptation", 10);

        result.Should().BeGreaterThan(65f, "resistance should increase integrity");
        npc.GodTrustLevel.Should().BeGreaterThan(50f, "resistance should boost trust");
    }

    [Theory]
    [InlineData(95f, DoctrineIntegrityStatus.Exalted,     1.30f)]
    [InlineData(80f, DoctrineIntegrityStatus.Faithful,    1.05f)]
    [InlineData(60f, DoctrineIntegrityStatus.Shaken,      0.825f)]
    [InlineData(35f, DoctrineIntegrityStatus.Compromised, 0.55f)]
    [InlineData(10f, DoctrineIntegrityStatus.Broken,      0.15f)]
    public void IntegrityRecord_StatusAndModifier_CorrectAtThresholds(
        float score, DoctrineIntegrityStatus expectedStatus, float expectedMod)
    {
        var record = new DoctrineIntegrityRecord { Score = score };
        record.Status.Should().Be(expectedStatus);
        record.PowerModifier.Should().BeApproximately(expectedMod, 0.01f);
    }

    [Fact]
    public async Task CheckFallCondition_BrokenSaint_BecomesBloodSaint()
    {
        var npc = MakeVip("n1", ChurchRank.Saint);
        npc.DivineProfile.IsSaintCandidate = true;
        npc.DivineProfile.DoctrineIntegrity.Score = 10f;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.CheckFallConditionAsync("w1", "n1", tick: 100);

        npc.DivineProfile.ChurchRank.Should().Be(ChurchRank.BloodSaint,
            "Broken saint should fall to BloodSaint");
        npc.DivineProfile.IsSaintCandidate.Should().BeFalse("Fallen saint loses candidacy");
        npc.DivineProfile.DoctrineIntegrity.IsExcommunicated.Should().BeTrue();
    }

    [Fact]
    public async Task CheckFallCondition_BrokenProphet_FallsToDarkPath()
    {
        var npc = MakeVip("n1", ChurchRank.Prophet);
        npc.DivineProfile.IsProphetCandidate = true;
        npc.DivineProfile.DoctrineIntegrity.Score = 5f;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.CheckFallConditionAsync("w1", "n1", tick: 100);

        npc.DivineProfile.IsProphetCandidate.Should().BeFalse("Fallen prophet loses candidacy");
        npc.DivineProfile.IsDarkPathCandidate.Should().BeTrue("Fallen prophet enters dark path");
    }

    [Fact]
    public async Task Redemption_FullCompletion_RestoresIntegrity()
    {
        var npc = MakeVip("n1", ChurchRank.Priest);
        npc.DivineProfile.DoctrineIntegrity.Score = 20f;
        npc.DivineProfile.DoctrineIntegrity.IsExcommunicated = true;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.ApplyRedemptionProgressAsync("n1", 60f);
        await _sut.ApplyRedemptionProgressAsync("n1", 50f); // total > 100

        npc.DivineProfile.DoctrineIntegrity.Score.Should().BeGreaterThan(20f,
            "completed redemption restores integrity");
        npc.DivineProfile.DoctrineIntegrity.IsExcommunicated.Should().BeFalse(
            "full redemption lifts excommunication");
    }

    [Fact]
    public async Task WarningTags_ShakenStatus_AddsShakenFaithTag()
    {
        var npc = MakeVip("n1", ChurchRank.Priest);
        npc.DivineProfile.DoctrineIntegrity.Score = 60f;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.UpdateWarningTagsAsync("n1");

        npc.DivineProfile.ActiveWarnings.Should().Contain(GodNoteWarningTag.ShakenFaith);
    }

    [Fact]
    public async Task WarningTags_BrokenStatus_AddsAtRiskOfFallTag()
    {
        var npc = MakeVip("n1", ChurchRank.Saint);
        npc.DivineProfile.DoctrineIntegrity.Score = 15f;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.UpdateWarningTagsAsync("n1");

        npc.DivineProfile.ActiveWarnings.Should().Contain(GodNoteWarningTag.AtRiskOfFall);
    }

    private static NpcDocument MakeVip(string id, ChurchRank rank) => new NpcDocument
    {
        Id = id, Name = $"VIP_{id}", Tier = NpcTier.Noble, State = NpcState.Alive,
        DevotionLevel = 0.75f, GodTrustLevel = 60f, Loyalty = 70f,
        DivineProfile = new NpcDivineProfile
        {
            ChurchRank = rank,
            DoctrineIntegrity = new DoctrineIntegrityRecord { Score = 80f }
        }
    };
}

// ─── Escort Service Tests ─────────────────────────────────

public class EscortServiceTests
{
    private readonly Mock<INpcRepository>             _npcRepo = new();
    private readonly Mock<IOrganizationRepository>    _orgRepo = new();
    private readonly Mock<ICivilizationRepository>    _civRepo = new();
    private readonly Mock<IGodRepository>             _godRepo = new();
    private readonly Mock<IDoctrineIntegrityService>  _doctrine= new();
    private readonly EscortService _sut;

    public EscortServiceTests()
    {
        _npcRepo.Setup(r => r.UpdateAsync(It.IsAny<NpcDocument>())).Returns(Task.CompletedTask);
        _doctrine.Setup(d => d.UpdateWarningTagsAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _sut = new EscortService(
            _npcRepo.Object, _orgRepo.Object, _civRepo.Object,
            _godRepo.Object, _doctrine.Object,
            NullLogger<EscortService>.Instance);
    }

    [Fact]
    public async Task GenerateEscort_ForSaint_CreatesLargeEscort()
    {
        var saint = MakeVip("s1", ChurchRank.Saint);
        var potentialGuards = Enumerable.Range(0, 25).Select(i => new NpcDocument
        {
            Id = $"g{i}", Name = $"Guard{i}", Tier = NpcTier.Servant,
            State = NpcState.Alive, Loyalty = 75f, CivilizationId = "civ1"
        }).ToList();

        _npcRepo.Setup(r => r.GetByIdAsync("s1")).ReturnsAsync(saint);
        _npcRepo.Setup(r => r.GetByCivilizationAsync("civ1")).ReturnsAsync(potentialGuards);

        var escort = await _sut.GenerateEscortAsync("w1", saint, tick: 50);

        escort.Should().NotBeNull();
        escort!.Members.Count.Should().BeGreaterThanOrEqualTo(8, "Saints have 8-30 escorts");
        escort.ProtectedNpcId.Should().Be("s1");
        escort.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateEscort_ForPriest_CreatesSmallEscort()
    {
        var priest = MakeVip("p1", ChurchRank.Priest);
        var guards = Enumerable.Range(0, 5).Select(i => new NpcDocument
        {
            Id = $"g{i}", Tier = NpcTier.Servant,
            State = NpcState.Alive, Loyalty = 65f, CivilizationId = "civ1"
        }).ToList();

        _npcRepo.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(priest);
        _npcRepo.Setup(r => r.GetByCivilizationAsync("civ1")).ReturnsAsync(guards);

        var escort = await _sut.GenerateEscortAsync("w1", priest, tick: 10);

        escort.Should().NotBeNull();
        escort!.Members.Count.Should().BeLessThanOrEqualTo(3, "Priests only have 1-3 guards");
    }

    [Fact]
    public async Task AttemptKidnap_StrongEscort_DefeatsAttackers()
    {
        var saint = MakeVip("s1", ChurchRank.Saint);
        saint.DivineProfile.AssignedEscort = new EscortGroup
        {
            ProtectedNpcId = "s1",
            GroupStrength = 200f,
            IsActive = true,
            Members = new List<EscortMember>
            {
                new() { NpcId = "g1", Role = EscortRole.GuardKnight, Loyalty = 90f }
            }
        };
        saint.GodInfluenceId = "god1";

        _npcRepo.Setup(r => r.GetByIdAsync("s1")).ReturnsAsync(saint);
        _npcRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(
            new NpcDocument { Id = "g1", State = NpcState.Alive, DivineProfile = new() });
        _orgRepo.Setup(r => r.GetByIdAsync("org1")).ReturnsAsync(
            new OrganizationDocument { Id = "org1", Power = 25f });
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(
            new GodDocument { Id = "god1", Faith = 500f, Trust = 70f });
        _godRepo.Setup(r => r.UpdateAsync(It.IsAny<GodDocument>())).Returns(Task.CompletedTask);

        var kidnapped = await _sut.AttemptKidnapAsync("w1", saint, "org1", tick: 100);

        kidnapped.Should().BeFalse("strong escort should repel kidnap attempt");
    }

    [Fact]
    public async Task AttemptKidnap_NoEscort_VulnerableToCapture()
    {
        var prophet = MakeVip("p1", ChurchRank.Prophet);
        prophet.DivineProfile.AssignedEscort = null;
        prophet.GodInfluenceId = "god1";

        _npcRepo.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(prophet);
        _orgRepo.Setup(r => r.GetByIdAsync("org1")).ReturnsAsync(
            new OrganizationDocument { Id = "org1", Power = 90f });
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(
            new GodDocument { Id = "god1", Faith = 500f, Trust = 70f });
        _godRepo.Setup(r => r.UpdateAsync(It.IsAny<GodDocument>())).Returns(Task.CompletedTask);

        var kidnapped = await _sut.AttemptKidnapAsync("w1", prophet, "org1", tick: 100);

        kidnapped.Should().BeTrue("prophet with no escort is captured");
        prophet.State.Should().Be(NpcState.Captured);
    }

    [Fact]
    public async Task DisbandEscort_SetsInactive()
    {
        var npc = MakeVip("n1", ChurchRank.Saint);
        npc.DivineProfile.AssignedEscort = new EscortGroup
        {
            ProtectedNpcId = "n1",
            IsActive = true,
            Members = new List<EscortMember>()
        };
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.DisbandEscortAsync("n1");

        npc.DivineProfile.AssignedEscort!.IsActive.Should().BeFalse("disbanded escort is no longer active");
    }

    private static NpcDocument MakeVip(string id, ChurchRank rank) => new NpcDocument
    {
        Id = id, Name = $"VIP_{id}", Tier = NpcTier.Noble, State = NpcState.Alive,
        CivilizationId = "civ1", DivineProfile = new NpcDivineProfile
        {
            ChurchRank = rank,
            DoctrineIntegrity = new DoctrineIntegrityRecord { Score = 80f }
        }
    };
}
