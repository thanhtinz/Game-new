using FluentAssertions;
using WorldFaith.Server.Services.Gameplay;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Gameplay;

// Gameplay Spec §11 — connected shared-event stories (the abandonment example).
public class StoryEventServiceTests
{
    private readonly StoryEventService _sut = new();

    private StoryEvent Abandonment() => _sut.CreateSharedEvent(
        coreFact: "Parent P abandoned child C at the river ford",
        year: 812, location: "River Ford", outcome: "C was taken in by a healer",
        participants: new[]
        {
            new StoryParticipantSpec("parent", StoryRole.Actor, "I left to protect them from the raid", EventConfidence.Strong),
            new StoryParticipantSpec("child",  StoryRole.Victim, "I was abandoned and left alone", EventConfidence.Confirmed),
            new StoryParticipantSpec("distant_cousin", StoryRole.Witness, "", EventConfidence.Unknown, Knows: false),
        });

    [Fact]
    public void OneEvent_SharesCoreFact_ButPerspectivesDiffer()
    {
        var evt = Abandonment();

        evt.Perspectives.Should().HaveCount(3);
        evt.CoreFact.Should().Contain("abandoned");

        var parent = evt.Perspectives.Single(p => p.NpcId == "parent");
        var child = evt.Perspectives.Single(p => p.NpcId == "child");
        parent.Belief.Should().NotBe(child.Belief, "same core fact, different interpretations");
        parent.Role.Should().Be(StoryRole.Actor);
        child.Role.Should().Be(StoryRole.Victim);
    }

    [Fact]
    public void NpcWhoDidNotKnow_CannotReport_AndHoldsNoBelief()
    {
        var evt = Abandonment();

        _sut.CanReport(evt, "distant_cousin").Should().BeFalse("cannot remember what they never witnessed (§11.3)");
        evt.Perspectives.Single(p => p.NpcId == "distant_cousin").Belief.Should().BeEmpty();
        _sut.CanReport(evt, "child").Should().BeTrue();
    }

    [Fact]
    public void KnownBy_ReturnsOnlyThoseWhoKnow()
    {
        var evt = Abandonment();
        _sut.KnownBy(evt).Select(p => p.NpcId).Should().BeEquivalentTo(new[] { "parent", "child" });
    }

    [Fact]
    public void NewEvent_StartsAsAnOpenThread()
        => Abandonment().ThreadState.Should().Be(StoryThreadState.Open);
}
