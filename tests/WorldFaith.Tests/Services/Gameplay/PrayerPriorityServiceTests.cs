using FluentAssertions;
using WorldFaith.Server.Services.Gameplay;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Gameplay;

// Gameplay Spec §7.2–7.3 — prayer/request priority.
public class PrayerPriorityServiceTests
{
    private readonly PrayerPriorityService _sut = new();

    [Fact]
    public void ImmediateThreatToLife_IsCritical()
    {
        _sut.Classify(new PrayerRequest { ImmediateThreatToLife = true })
            .Should().Be(RequestPriority.Critical);
    }

    [Fact]
    public void MeaningfulConsequence_IsImportant()
    {
        _sut.Classify(new PrayerRequest { ConsequenceIfIgnored = 60f })
            .Should().Be(RequestPriority.Important);
    }

    [Fact]
    public void DailyWish_IsRoutine()
    {
        _sut.Classify(new PrayerRequest { IsRoutine = true, ConsequenceIfIgnored = 90f })
            .Should().Be(RequestPriority.Routine);
    }

    [Fact]
    public void AnsweredOrFulfilled_IsResolved()
    {
        _sut.Classify(new PrayerRequest { IsAnswered = true, ImmediateThreatToLife = true })
            .Should().Be(RequestPriority.Resolved);
        _sut.Classify(new PrayerRequest { IsFulfilledByMortals = true })
            .Should().Be(RequestPriority.Resolved);
    }

    [Fact]
    public void RankActive_PutsCriticalFirst_AndDropsResolved()
    {
        var critical = new PrayerRequest { ThreatToSettlement = true };
        var important = new PrayerRequest { ConsequenceIfIgnored = 55f };
        var routine = new PrayerRequest { IsRoutine = true };
        var resolved = new PrayerRequest { IsExpired = true, ImmediateThreatToLife = true };

        var ranked = _sut.RankActive(new[] { routine, resolved, important, critical });

        ranked.Should().HaveCount(3, "resolved requests drop off the active board");
        ranked[0].Should().Be(critical);
        ranked[1].Should().Be(important);
    }
}
