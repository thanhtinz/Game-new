using WorldFaith.Server.Models;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// ─── Blessing / Curse Founding (Dynasty Spec §8, Roadmap Phase 3) ──
// A god action on one NPC founds a lineage: a hereditary blessing or curse that
// descendants can inherit. A curse is the same engine with a negative theme,
// different kind and event triggers.

public interface IBloodlineFoundingService
{
    /// <summary>
    /// Found a new bloodline from a divine blessing/curse on <paramref name="founder"/>,
    /// attaching a generation-0 active instance to them. Returns the new bloodline.
    /// </summary>
    BloodlineDocument FoundLineage(
        NpcDocument founder,
        string godId,
        GodDomain domain,
        string bloodlineName,
        float foundingStrength,
        int currentYear,
        bool isDivineLineage = false,
        bool isCurse = false);
}

public class BloodlineFoundingService : IBloodlineFoundingService
{
    public BloodlineDocument FoundLineage(
        NpcDocument founder,
        string godId,
        GodDomain domain,
        string bloodlineName,
        float foundingStrength,
        int currentYear,
        bool isDivineLineage = false,
        bool isCurse = false)
    {
        foundingStrength = Math.Clamp(foundingStrength, 0f, 100f);

        var blessing = new BloodlineBlessingDefinition
        {
            Name = bloodlineName,
            GodId = godId,
            Domain = domain,
            FoundingStrength = foundingStrength,
            IsDivineLineage = isDivineLineage,
            // Divine lineages decay slowly; curses cling harder than minor blessings.
            GenerationalDecayRate = isDivineLineage ? 0.08f : (isCurse ? 0.15f : 0.22f),
            CanBecomeDormant = true,
        };

        var kind = isCurse ? BloodlineKind.CursedLineage
                 : isDivineLineage ? BloodlineKind.DivineLineage
                 : BloodlineKind.BlessedLineage;

        var bloodline = new BloodlineDocument
        {
            WorldId = founder.WorldId,
            Name = bloodlineName,
            Kind = kind,
            FounderNpcId = founder.Id,
            FounderGodId = godId,
            FoundedYear = currentYear,
            Purity = 100f,
            Blessings = { blessing },
        };

        founder.InheritedBlessings.Add(new InheritedBlessingInstance
        {
            BlessingId = blessing.BlessingId,
            SourceBloodlineId = bloodline.Id,
            SourceGodId = godId,
            Strength = foundingStrength,
            Potential = Math.Clamp(foundingStrength + 10f, 0f, 100f),
            State = BlessingState.Active,
            GenerationDistanceFromFounder = 0,
        });

        if (!founder.BloodlineIds.Contains(bloodline.Id))
            founder.BloodlineIds.Add(bloodline.Id);
        founder.PrimaryBloodlineId ??= bloodline.Id;

        return bloodline;
    }
}
