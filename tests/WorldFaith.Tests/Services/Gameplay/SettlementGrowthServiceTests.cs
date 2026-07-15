using FluentAssertions;
using WorldFaith.Server.Services.Gameplay;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Gameplay;

// Gameplay Spec §4 — settlement growth & kingdom formation.
public class SettlementGrowthServiceTests
{
    private readonly SettlementGrowthService _sut = new();

    [Fact]
    public void TinyGroup_IsACamp()
    {
        var s = new SettlementSnapshot(10, false, false, false, false, false, false, false);
        _sut.EvaluateStage(s).Should().Be(SettlementStage.Camp);
    }

    [Fact]
    public void StableSmallGroup_IsAHamlet()
    {
        var s = new SettlementSnapshot(30, StableSupplies: true, false, false, false, false, false, false);
        _sut.EvaluateStage(s).Should().Be(SettlementStage.Hamlet);
    }

    [Fact]
    public void OrganizedCommunity_IsAVillage()
    {
        var s = new SettlementSnapshot(80, true, WorkRoles: true, LocalAuthority: true, false, false, false, false);
        _sut.EvaluateStage(s).Should().Be(SettlementStage.Village);
    }

    [Fact]
    public void TradeSpecialization_IsATown()
    {
        var s = new SettlementSnapshot(200, true, true, true, TradeRoutes: true, Surplus: true, false, false);
        _sut.EvaluateStage(s).Should().Be(SettlementStage.Town);
    }

    [Fact]
    public void DenseCenter_IsACity()
    {
        var s = new SettlementSnapshot(500, true, true, true, true, true, StrongEconomy: true, Defenses: true);
        _sut.EvaluateStage(s).Should().Be(SettlementStage.City);
    }

    [Fact]
    public void BigVillageAlone_DoesNotFormKingdom()
    {
        var input = new KingdomFormationInput(SettlementCount: 1, HasPowerfulCity: false,
            RecognizedLeadership: true, TerritorialClaims: true, EnforcementMeans: true, Legitimacy: 80);

        _sut.CanFormKingdom(input).Should().BeFalse("a single large village is not a kingdom (§4.3)");
    }

    [Fact]
    public void MultipleSettlements_WithLeadershipAndLegitimacy_FormKingdom()
    {
        var input = new KingdomFormationInput(SettlementCount: 3, HasPowerfulCity: false,
            RecognizedLeadership: true, TerritorialClaims: true, EnforcementMeans: true, Legitimacy: 60);

        _sut.CanFormKingdom(input).Should().BeTrue();
    }

    [Fact]
    public void LowLegitimacy_BlocksKingdomFormation()
    {
        var input = new KingdomFormationInput(3, false, true, true, true, Legitimacy: 20);
        _sut.CanFormKingdom(input).Should().BeFalse();
    }
}
