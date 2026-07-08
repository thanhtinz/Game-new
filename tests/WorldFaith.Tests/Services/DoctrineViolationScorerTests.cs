using FluentAssertions;
using WorldFaith.Server.Models;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;
using Xunit;

namespace WorldFaith.Tests.Services;

// NPC Master Spec §7 / §13 — doctrine violation scoring.
public class DoctrineViolationScorerTests
{
    [Fact]
    public void PublicIntentionalForbiddenAct_ShouldCreateHighSeverity()
    {
        var doctrine = new DoctrineValues
        {
            MercyVsPunishment = 80f,          // strict punishment doctrine
            ForbiddenTags = { DoctrineTag.Darkness }
        };
        var evt = new DoctrineEvent
        {
            Tags = { DoctrineTag.Darkness },  // forbidden act
            MercyImpact = -80f,               // a merciful act — opposite of doctrine
            WasPublic = true,
            WasIntentional = true
        };

        var severity = DoctrineViolationScorer.CalculateSeverity(doctrine, evt);

        ((int)severity).Should().BeGreaterThanOrEqualTo((int)ViolationSeverity.MajorViolation,
            "a public, intentional, forbidden, doctrine-opposing act is a serious violation");
    }

    [Fact]
    public void MinorPrivateSlip_ShouldBeLowSeverity()
    {
        var doctrine = new DoctrineValues { MercyVsPunishment = 10f };
        var evt = new DoctrineEvent
        {
            MercyImpact = 5f,       // barely off doctrine
            WasPublic = false,
            WasIntentional = false
        };

        DoctrineViolationScorer.CalculateSeverity(doctrine, evt)
            .Should().Be(ViolationSeverity.MinorContradiction);
    }

    [Fact]
    public void SacredTag_ReducesConflict()
    {
        var doctrine = new DoctrineValues { SacredTags = { DoctrineTag.Light } };
        var honoring = new DoctrineEvent { Tags = { DoctrineTag.Light } };
        var neutral  = new DoctrineEvent();

        // Honoring a sacred tag should never score higher than a neutral act.
        ((int)DoctrineViolationScorer.CalculateSeverity(doctrine, honoring))
            .Should().BeLessThanOrEqualTo((int)DoctrineViolationScorer.CalculateSeverity(doctrine, neutral));
    }
}
