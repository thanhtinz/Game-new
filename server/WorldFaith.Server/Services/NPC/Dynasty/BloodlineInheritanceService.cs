using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Common;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// ─── Bloodline Inheritance (Dynasty Spec §5) ──────────────────
// Inheritance is a weighted roll, not a yes/no copy. Each child evaluates every
// inheritable blessing from both parents; the result may be active, dormant,
// weakened, or lost. Divine lineages decay much slower; race affinity, bloodline
// purity, divine favor and random variance make siblings differ.

public interface IBloodlineInheritanceService
{
    /// <summary>Roll one blessing for a child; null = not inherited.</summary>
    InheritedBlessingInstance? RollInheritance(
        BloodlineBlessingDefinition blessing,
        InheritedBlessingInstance parentInstance,
        NpcDocument child,
        BloodlineDocument bloodline,
        float divineFavorModifier);

    /// <summary>Roll every inheritable blessing carried by either parent.</summary>
    IReadOnlyList<InheritedBlessingInstance> RollAllForChild(
        NpcDocument parentA,
        NpcDocument parentB,
        NpcDocument child,
        IReadOnlyDictionary<string, BloodlineDocument> bloodlines,
        float divineFavorModifier = 1f);
}

public class BloodlineInheritanceService : IBloodlineInheritanceService
{
    private readonly IRandomService _rng;
    private readonly IBloodlineAffinityService _affinity;

    public BloodlineInheritanceService(IRandomService rng, IBloodlineAffinityService affinity)
    {
        _rng = rng;
        _affinity = affinity;
    }

    public InheritedBlessingInstance? RollInheritance(
        BloodlineBlessingDefinition blessing,
        InheritedBlessingInstance parentInstance,
        NpcDocument child,
        BloodlineDocument bloodline,
        float divineFavorModifier)
    {
        float parentStrength = parentInstance.Strength;
        if (parentStrength <= 1f && !blessing.CanBecomeDormant)
            return null;

        float raceAffinity = _affinity.GetDomainAffinity(child.Race, blessing.Domain);
        float purity = Math.Clamp(bloodline.Purity / 100f, 0.1f, 1.2f);
        float randomVariance = NextFloat(0.75f, 1.25f);

        float decay = blessing.IsDivineLineage
            ? blessing.GenerationalDecayRate * 0.35f
            : blessing.GenerationalDecayRate;

        float inherited = parentStrength * (1f - decay);
        inherited *= purity;
        inherited *= raceAffinity;
        inherited *= divineFavorModifier;
        inherited *= randomVariance;
        inherited = Math.Clamp(inherited, 0f, 100f);

        float potential = Math.Clamp(inherited + NextFloat(0f, 25f), 0f, 100f);

        if (inherited < 3f && potential < 10f)
            return null;

        BlessingState state = inherited switch
        {
            >= 25f => BlessingState.Active,
            >= 3f  => BlessingState.Dormant,
            _      => BlessingState.Faded
        };

        return new InheritedBlessingInstance
        {
            BlessingId = blessing.BlessingId,
            SourceBloodlineId = bloodline.Id,
            SourceGodId = blessing.GodId,
            Strength = inherited,
            Potential = potential,
            State = state,
            GenerationDistanceFromFounder = parentInstance.GenerationDistanceFromFounder + 1
        };
    }

    public IReadOnlyList<InheritedBlessingInstance> RollAllForChild(
        NpcDocument parentA,
        NpcDocument parentB,
        NpcDocument child,
        IReadOnlyDictionary<string, BloodlineDocument> bloodlines,
        float divineFavorModifier = 1f)
    {
        var results = new List<InheritedBlessingInstance>();

        foreach (var parentInstance in parentA.InheritedBlessings.Concat(parentB.InheritedBlessings))
        {
            if (!bloodlines.TryGetValue(parentInstance.SourceBloodlineId, out var bloodline))
                continue;

            var blessing = bloodline.Blessings.FirstOrDefault(b => b.BlessingId == parentInstance.BlessingId);
            if (blessing == null) continue;

            var rolled = RollInheritance(blessing, parentInstance, child, bloodline, divineFavorModifier);
            if (rolled != null) results.Add(rolled);
        }

        return results;
    }

    private float NextFloat(float min, float max) => min + _rng.NextFloat() * (max - min);
}
