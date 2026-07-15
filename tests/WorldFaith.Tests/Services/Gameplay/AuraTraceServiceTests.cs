using FluentAssertions;
using WorldFaith.Server.Services.Gameplay;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services.Gameplay;

// Gameplay Spec §10.3–10.5 — rival aura trace detection.
public class AuraTraceServiceTests
{
    private readonly AuraTraceService _sut = new();

    private static AuraTraceInput Base(bool influence = true) => new(
        HasRivalInfluence: influence, NpcConfided: false, ExaminedBySpecialist: false,
        RepeatedPatternCount: 0, PlayerCounteredDirectly: false, EnteredSacredArea: false);

    [Fact]
    public void NoRivalInfluence_ShowsNoTrace()
        => _sut.Evaluate(Base(influence: false)).Should().Be(AuraTraceStrength.None);

    [Fact]
    public void BareInfluence_IsFaint()
        => _sut.Evaluate(Base()).Should().Be(AuraTraceStrength.Faint);

    [Fact]
    public void SpecialistExamination_ReachesRecognizable()
        => _sut.Evaluate(Base() with { ExaminedBySpecialist = true })
               .Should().Be(AuraTraceStrength.Recognizable);

    [Fact]
    public void ExaminedRepeatedPattern_ReachesIdentified()
        => _sut.Evaluate(Base() with { ExaminedBySpecialist = true, RepeatedPatternCount = 2 })
               .Should().Be(AuraTraceStrength.Identified);

    [Fact]
    public void Confession_RevealsSpecifics()
        => _sut.Evaluate(Base() with { NpcConfided = true }).Should().Be(AuraTraceStrength.Revealed);

    [Fact]
    public void CounteringAloneDoesNotReveal_AntiFrustrationValidCauseRequired()
    {
        // Player counters but has no confession/examination → must NOT jump to Revealed (§10.5).
        _sut.Evaluate(Base() with { PlayerCounteredDirectly = true })
            .Should().NotBe(AuraTraceStrength.Revealed);
    }
}
