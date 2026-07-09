using WorldFaith.Server.Models;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// ─── Family Tree (Dynasty Spec §6) ────────────────────────────
// Creates children from two parents: records parent IDs, resolves the family
// name, mixes genes, rolls inherited blessings, and derives the child's
// bloodline IDs and primary bloodline (for reputation/UI).

public interface IFamilyTreeService
{
    NpcDocument CreateChild(
        NpcDocument parentA,
        NpcDocument parentB,
        int currentYear,
        string givenName,
        IReadOnlyDictionary<string, BloodlineDocument> bloodlines,
        float divineFavorModifier = 1f);
}

public class FamilyTreeService : IFamilyTreeService
{
    private readonly IBloodlineInheritanceService _inheritance;
    private readonly IGeneMixingService _genes;

    public FamilyTreeService(IBloodlineInheritanceService inheritance, IGeneMixingService genes)
    {
        _inheritance = inheritance;
        _genes = genes;
    }

    public NpcDocument CreateChild(
        NpcDocument parentA,
        NpcDocument parentB,
        int currentYear,
        string givenName,
        IReadOnlyDictionary<string, BloodlineDocument> bloodlines,
        float divineFavorModifier = 1f)
    {
        var (father, mother) = ResolveParents(parentA, parentB);

        var child = new NpcDocument
        {
            Name = givenName,
            FamilyName = ResolveFamilyName(parentA, parentB),
            Race = ResolveChildRace(parentA, parentB),
            BirthYear = currentYear,
            WorldId = parentA.WorldId,
            CivilizationId = parentA.CivilizationId,
            FamilyId = parentA.FamilyId ?? parentB.FamilyId,
            FatherNpcId = father?.Id,
            MotherNpcId = mother?.Id,
            Genes = _genes.Mix(parentA.Genes, parentB.Genes),
        };

        child.InheritedBlessings.AddRange(
            _inheritance.RollAllForChild(parentA, parentB, child, bloodlines, divineFavorModifier));

        child.BloodlineIds = child.InheritedBlessings
            .Select(x => x.SourceBloodlineId)
            .Distinct()
            .ToList();

        child.PrimaryBloodlineId = ChoosePrimaryBloodline(child);

        // Keep both parents' child lists in sync.
        parentA.ChildrenIds.Add(child.Id);
        parentB.ChildrenIds.Add(child.Id);

        return child;
    }

    private static (NpcDocument? father, NpcDocument? mother) ResolveParents(NpcDocument a, NpcDocument b)
    {
        if (a.Sex == SexType.Male || b.Sex == SexType.Female) return (a, b);
        if (a.Sex == SexType.Female || b.Sex == SexType.Male) return (b, a);
        return (a, b); // unknown sexes — arbitrary but stable
    }

    private static string? ResolveFamilyName(NpcDocument a, NpcDocument b)
        => !string.IsNullOrWhiteSpace(a.FamilyName) ? a.FamilyName : b.FamilyName;

    // Same-race parents pass on their race; mixed unions default to Human
    // (placeholder for future culture/race rules — Dynasty Spec §6 note).
    private static RaceType ResolveChildRace(NpcDocument a, NpcDocument b)
        => a.Race == b.Race ? a.Race : RaceType.Human;

    private static string? ChoosePrimaryBloodline(NpcDocument child)
        => child.InheritedBlessings
            .OrderByDescending(x => x.Strength + x.Potential * 0.25f)
            .FirstOrDefault()?.SourceBloodlineId;
}
