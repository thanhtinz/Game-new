using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Server.Services.Faith;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

public class FaithServiceTests
{
    private readonly Mock<IGodRepository>          _godRepo     = new();
    private readonly Mock<ICivilizationRepository> _civRepo     = new();
    private readonly Mock<IReligionRepository>     _religionRepo= new();
    private readonly Mock<IBalanceConfigService>   _balance     = new();
    private readonly FaithService                  _sut;

    public FaithServiceTests()
    {
        // Default balance values
        _balance.Setup(b => b.GetFloatAsync(It.IsAny<string>()))
            .ReturnsAsync((string key) => key switch
            {
                "miracle.cost.rain"          => 10f,
                "miracle.cost.dream"         => 5f,
                "miracle.cost.bless_harvest" => 15f,
                "miracle.cost.storm"         => 30f,
                "miracle.cost.holy_war"      => 150f,
                "faith.follower_gen_rate"    => 0.01f,
                "faith.temple_gen_rate"      => 0.5f,
                "faith.max_faith"            => 1000f,
                "faith.fear_dark_bonus"      => 0.02f,
                _ => 10f
            });

        _sut = new FaithService(
            _godRepo.Object,
            _civRepo.Object,
            _religionRepo.Object,
            _balance.Object,
            NullLogger<FaithService>.Instance);
    }

    // ─── GetMiracleCostAsync ─────────────────────────────────

    [Theory]
    [InlineData(MiracleType.Dream,     5f)]
    [InlineData(MiracleType.Rain,      10f)]
    [InlineData(MiracleType.Storm,     30f)]
    [InlineData(MiracleType.HolyWar,  150f)]
    public async Task GetMiracleCostAsync_ReturnsCorrectCost(MiracleType miracle, float expectedCost)
    {
        var cost = await _sut.GetMiracleCostAsync(miracle);
        cost.Should().Be(expectedCost);
    }

    // ─── CanPerformMiracleAsync ──────────────────────────────

    [Fact]
    public async Task CanPerformMiracle_ReturnsFalse_WhenGodNotFound()
    {
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync((GodDocument?)null);
        var result = await _sut.CanPerformMiracleAsync("god1", MiracleType.Rain);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanPerformMiracle_ReturnsFalse_WhenGodDead()
    {
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(new GodDocument
        {
            Id = "god1", IsAlive = false, Faith = 100f,
            UnlockedMiracles = new List<MiracleType> { MiracleType.Rain }
        });
        var result = await _sut.CanPerformMiracleAsync("god1", MiracleType.Rain);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanPerformMiracle_ReturnsFalse_WhenMiracleNotUnlocked()
    {
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(new GodDocument
        {
            Id = "god1", IsAlive = true, Faith = 100f,
            UnlockedMiracles = new List<MiracleType> { MiracleType.Dream }
        });
        var result = await _sut.CanPerformMiracleAsync("god1", MiracleType.HolyWar);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanPerformMiracle_ReturnsFalse_WhenInsufficientFaith()
    {
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(new GodDocument
        {
            Id = "god1", IsAlive = true, Faith = 5f,  // Rain costs 10
            UnlockedMiracles = new List<MiracleType> { MiracleType.Rain }
        });
        var result = await _sut.CanPerformMiracleAsync("god1", MiracleType.Rain);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanPerformMiracle_ReturnsTrue_WhenAllConditionsMet()
    {
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(new GodDocument
        {
            Id = "god1", IsAlive = true, Faith = 100f,
            UnlockedMiracles = new List<MiracleType> { MiracleType.Rain }
        });
        var result = await _sut.CanPerformMiracleAsync("god1", MiracleType.Rain);
        result.Should().BeTrue();
    }

    // ─── ConsumeFaithAsync ───────────────────────────────────

    [Fact]
    public async Task ConsumeFaithAsync_ReducesFaithByMiracleCost()
    {
        var god = new GodDocument
        {
            Id = "god1", Faith = 100f, Archetype = GodArchetype.Order,
            Trust = 50f, Fear = 0f, FollowerCount = 10,
            UnlockedMiracles = new List<MiracleType> { MiracleType.Rain }
        };
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(god);

        float cost = await _sut.ConsumeFaithAsync("god1", MiracleType.Rain);

        cost.Should().Be(10f); // Rain = 10
        _godRepo.Verify(r => r.UpdateFaithAsync("god1", 90f, It.IsAny<float>(), It.IsAny<float>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ConsumeFaithAsync_NeverGoesBelowZero()
    {
        var god = new GodDocument
        {
            Id = "god1", Faith = 5f, Archetype = GodArchetype.Chaos,
            Trust = 50f, Fear = 0f, FollowerCount = 0,
            UnlockedMiracles = new List<MiracleType> { MiracleType.Rain }
        };
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(god);

        await _sut.ConsumeFaithAsync("god1", MiracleType.Rain);

        _godRepo.Verify(r => r.UpdateFaithAsync("god1", 0f, It.IsAny<float>(), It.IsAny<float>(), It.IsAny<int>()), Times.Once);
    }

    // ─── Archetype Discounts ─────────────────────────────────

    [Fact]
    public async Task ConsumeFaithAsync_LightGod_HealFollowerIsFree()
    {
        var god = new GodDocument
        {
            Id = "god1", Faith = 100f, Archetype = GodArchetype.Light,
            Trust = 80f, Fear = 0f, FollowerCount = 100,
            UnlockedMiracles = new List<MiracleType> { MiracleType.HealFollower }
        };
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(god);

        float cost = await _sut.ConsumeFaithAsync("god1", MiracleType.HealFollower);

        cost.Should().Be(0f); // Light god: HealFollower free
        _godRepo.Verify(r => r.UpdateFaithAsync("god1", 100f, It.IsAny<float>(), It.IsAny<float>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task ConsumeFaithAsync_WarGod_HolyWarDiscounted()
    {
        var god = new GodDocument
        {
            Id = "god1", Faith = 200f, Archetype = GodArchetype.War,
            Trust = 50f, Fear = 0f, FollowerCount = 50,
            UnlockedMiracles = new List<MiracleType> { MiracleType.HolyWar }
        };
        _godRepo.Setup(r => r.GetByIdAsync("god1")).ReturnsAsync(god);

        float cost = await _sut.ConsumeFaithAsync("god1", MiracleType.HolyWar);

        // War god: HolyWar = 150 * 0.70 = 105
        cost.Should().BeApproximately(105f, 1f);
    }
}
