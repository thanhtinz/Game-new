using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Achievement;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

public class AchievementServiceTests
{
    private readonly Mock<INpcRepository>          _npcRepo      = new();
    private readonly Mock<IGodRepository>          _godRepo      = new();
    private readonly Mock<IReligionRepository>     _religionRepo = new();
    private readonly Mock<ICivilizationRepository> _civRepo      = new();
    private readonly Mock<IDoctrineIntegrityService> _doctrineIntegrity = new();
    private readonly AchievementService _sut;

    public AchievementServiceTests()
    {
        _npcRepo.Setup(r => r.UpdateAsync(It.IsAny<NpcDocument>())).Returns(Task.CompletedTask);
        _sut = new AchievementService(
            _npcRepo.Object, _godRepo.Object, _religionRepo.Object,
            _civRepo.Object, _doctrineIntegrity.Object,
            NullLogger<AchievementService>.Instance);
    }

    // ─── Achievement Earning ──────────────────────────────

    [Fact]
    public async Task EarnAchievement_ValidKey_AddsToProfile()
    {
        var npc = MakeNpc("n1", NpcTier.Adventurer);
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        var result = await _sut.EarnAchievementAsync("n1", "cleared_dungeon", tick: 50);

        result.Should().NotBeNull();
        result.Name.Should().Be("Conqueror of Darkness");
        result.Rarity.Should().Be(AchievementRarity.Rare);
        npc.DivineProfile.Achievements.Should().HaveCount(1);
        npc.DivineProfile.AchievementValue.Should().Be(20); // weight=20
    }

    [Fact]
    public async Task EarnAchievement_Duplicate_DoesNotAddAgain()
    {
        var npc = MakeNpc("n1", NpcTier.Adventurer);
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.EarnAchievementAsync("n1", "cleared_dungeon", 50);
        await _sut.EarnAchievementAsync("n1", "cleared_dungeon", 60);

        npc.DivineProfile.Achievements.Should().HaveCount(1, "duplicate achievements not allowed");
    }

    [Fact]
    public async Task EarnAchievement_RoyalService_HighGodNoteWeight()
    {
        var npc = MakeNpc("n1", NpcTier.Noble);
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        var result = await _sut.EarnAchievementAsync("n1", "converted_noble_house", tick: 100);

        result.GodNoteWeight.Should().Be(60); // Epic weight
        result.Rarity.Should().Be(AchievementRarity.Epic);
    }

    // ─── Talent Awakening ─────────────────────────────────

    [Fact]
    public async Task AwakenTalent_ValidTalent_AddsToProfile()
    {
        var npc = MakeNpc("n1", NpcTier.Servant);
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.AwakentTalentAsync("n1", "pure_soul", "Survived plague");

        npc.DivineProfile.Talents.Should().HaveCount(1);
        npc.DivineProfile.Talents[0].IsAwakened.Should().BeTrue();
        npc.DivineProfile.Talents[0].AwakenedByEvent.Should().Be("Survived plague");
    }

    [Fact]
    public async Task AwakenTalent_Duplicate_DoesNotAddAgain()
    {
        var npc = MakeNpc("n1", NpcTier.Servant);
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.AwakentTalentAsync("n1", "pure_soul", "Event 1");
        await _sut.AwakentTalentAsync("n1", "pure_soul", "Event 2");

        npc.DivineProfile.Talents.Should().HaveCount(1, "talent should not be added twice");
    }

    // ─── Divine Attention Score ───────────────────────────

    [Fact]
    public async Task DivineAttentionScore_Formula_CorrectComponents()
    {
        var npc = MakeNpc("n1", NpcTier.Noble);
        npc.DevotionLevel = 0.9f;  // FaithLevel = 90
        npc.DivineProfile.Achievements.Add(new NpcAchievement
        {
            Name = "Test", Rarity = AchievementRarity.Epic, GodNoteWeight = 50
        });
        npc.DivineProfile.AchievementValue = 50;
        npc.DivineProfile.Reputation = 30;  // Noble tier
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        var score = await _sut.CalculateDivineAttentionAsync("n1");

        // Score = FaithLevel(90) + AchievementValue(50) + TalentRarity(0) + Reputation(30) + ...
        score.Should().BeGreaterThan(100f, "Noble with Epic achievement should have high attention score");
    }

    [Fact]
    public async Task DivineAttentionScore_HighCorruption_PenalizesScore()
    {
        var npc = MakeNpc("n1", NpcTier.Noble);
        npc.DevotionLevel = 0.9f;
        npc.Ambition = 85f;  // High ambition = +15 corruption risk
        npc.Loyalty = 20f;   // Low loyalty = +20 corruption risk
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        var score = await _sut.CalculateDivineAttentionAsync("n1");
        // CorruptionRisk = 35, penalizes score
        score.Should().BeLessThan(100f);
    }

    // ─── Candidacy Rules (GDD §8) ─────────────────────────

    [Fact]
    public async Task SaintCandidate_AllConditionsMet_MarkedTrue()
    {
        var npc = MakeNpc("n1", NpcTier.Servant);
        npc.DevotionLevel = 0.92f;  // > 0.85 threshold
        npc.Ambition = 10f;          // low
        npc.Loyalty = 90f;           // high → low corruption
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        // Earn major achievement
        await _sut.EarnAchievementAsync("n1", "survived_plague_prayer", 30);
        // Awaken spiritual talent
        await _sut.AwakentTalentAsync("n1", "pure_soul", "High devotion");

        npc.DivineProfile.IsSaintCandidate.Should().BeTrue(
            "High faith + spiritual talent + major achievement + low corruption = Saint candidate");
    }

    [Fact]
    public async Task ProphetCandidate_DreamSensitiveWithMiracleExposure_MarkedTrue()
    {
        var npc = MakeNpc("n1", NpcTier.Servant);
        npc.DevotionLevel = 0.75f;
        npc.DreamsReceived = 5;  // MiracleExposure += 25
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        // Awaken Dream Sensitive talent
        await _sut.AwakentTalentAsync("n1", "dream_sensitive", "Miracle Witnessed");
        // Add miracle exposure achievement
        await _sut.EarnAchievementAsync("n1", "witnessed_revelation", 50);

        npc.DivineProfile.IsProphetCandidate.Should().BeTrue(
            "Dream Sensitive + high miracle exposure = Prophet candidate");
    }

    [Fact]
    public async Task DarkPathCandidate_MultipleDarkAchievements_MarkedTrue()
    {
        var npc = MakeNpc("n1", NpcTier.Noble);
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        await _sut.EarnAchievementAsync("n1", "led_cult", 10);
        await _sut.EarnAchievementAsync("n1", "betrayed_temple", 20);

        npc.DivineProfile.IsDarkPathCandidate.Should().BeTrue(
            "2+ dark achievements = dark path candidate");
    }

    // ─── Divine Actions ────────────────────────────────────

    [Fact]
    public async Task ApplyAction_Bless_IncreasesDevotion()
    {
        var npc = MakeNpc("n1", NpcTier.Servant);
        npc.DevotionLevel = 0.5f;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);
        _godRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(new GodDocument { Id = "g1" });

        await _sut.ApplyDivineActionAsync("n1", "g1", DivineAction.Bless, tick: 1);

        npc.DevotionLevel.Should().Be(0.6f, "Bless should increase devotion by 0.1");
    }

    [Fact]
    public async Task ApplyAction_SendDream_IncreasesDreamsReceived()
    {
        var npc = MakeNpc("n1", NpcTier.Adventurer);
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);
        _godRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(new GodDocument { Id = "g1" });

        await _sut.ApplyDivineActionAsync("n1", "g1", DivineAction.SendDream, tick: 1);

        npc.DreamsReceived.Should().Be(1);
        npc.GodTrustLevel.Should().Be(5f);
    }

    [Fact]
    public async Task ApplyAction_Corrupt_SetsDarkPathCandidate()
    {
        var npc = MakeNpc("n1", NpcTier.Noble);
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);
        _godRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(new GodDocument { Id = "g1" });

        await _sut.ApplyDivineActionAsync("n1", "g1", DivineAction.Corrupt, tick: 1);

        npc.DivineProfile.IsDarkPathCandidate.Should().BeTrue("Corrupt action should set dark path");
        npc.DivineProfile.CorruptionRisk.Should().BeGreaterThan(20f);
    }

    [Fact]
    public async Task ApplyAction_MarkAsChosen_SetsChosenGodId()
    {
        var npc = MakeNpc("n1", NpcTier.Adventurer);
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);
        _godRepo.Setup(r => r.GetByIdAsync("g1")).ReturnsAsync(new GodDocument { Id = "g1" });

        await _sut.ApplyDivineActionAsync("n1", "g1", DivineAction.MarkAsChosen, tick: 1);

        npc.DivineProfile.ChosenByGodId.Should().Be("g1");
        npc.DivineProfile.DestinyModifier.Should().Be(30f);
    }

    // ─── Church Promotion ─────────────────────────────────

    [Fact]
    public async Task ChurchPromotion_PiousServant_BecomesTempleHelper()
    {
        var npc = MakeNpc("n1", NpcTier.Servant);
        npc.DevotionLevel = 0.55f;  // > 0.5 → DevoutBeliever
        npc.PersonalReligionId = "r1";
        npc.DivineProfile.ChurchRank = ChurchRank.DevoutBeliever;
        // Add religious achievement
        npc.DivineProfile.Achievements.Add(new NpcAchievement
        {
            Name = "Test", Category = AchievementCategory.ReligiousDevot, Rarity = AchievementRarity.Common
        });
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        var rank = await _sut.EvaluateChurchPromotionAsync("n1", "r1");

        rank.Should().Be(ChurchRank.TempleHelper,
            "DevoutBeliever + religious achievement should promote to TempleHelper");
    }

    [Fact]
    public async Task ChurchPromotion_DarkPath_FollowsDarkTracks()
    {
        var npc = MakeNpc("n1", NpcTier.Noble);
        npc.DivineProfile.IsDarkPathCandidate = true;
        npc.DivineProfile.ChurchRank = ChurchRank.Believer;
        _npcRepo.Setup(r => r.GetByIdAsync("n1")).ReturnsAsync(npc);

        var rank = await _sut.EvaluateChurchPromotionAsync("n1", "r1");

        rank.Should().Be(ChurchRank.SecretCultist,
            "Dark path candidates should follow the dark church track");
    }

    // ─── Helper ───────────────────────────────────────────

    private static NpcDocument MakeNpc(string id, NpcTier tier) => new NpcDocument
    {
        Id = id,
        Name = $"Test_{id}",
        Tier = tier,
        State = NpcState.Alive,
        DevotionLevel = 0.5f,
        Loyalty = 60f,
        Ambition = 30f,
        Piety = 50f,
        DivineProfile = new NpcDivineProfile
        {
            PrimaryMotivation = NpcMotivation.ServeGod
        }
    };
}
