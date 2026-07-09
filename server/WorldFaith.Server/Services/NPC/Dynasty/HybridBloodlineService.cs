using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Common;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// ─── Hybrid Bloodlines (Dynasty Spec §7) ──────────────────────
// When two compatible inherited blessings meet in one child and their combined
// strength passes a threshold, a rare hybrid lineage may mutate into being.

public class HybridBloodlineRule
{
    public GodDomain DomainA { get; set; }
    public GodDomain DomainB { get; set; }
    public string ResultName { get; set; } = string.Empty;
    public float MinimumCombinedStrength { get; set; } = 80f;
    public float MutationChance { get; set; } = 0.08f;
    public bool CreatesNewBloodlineDocument { get; set; } = true;
}

public static class HybridBloodlineRules
{
    // Spec §7 table, mapped onto GodDomain values that exist in this codebase.
    public static readonly IReadOnlyList<HybridBloodlineRule> Default = new List<HybridBloodlineRule>
    {
        new() { DomainA = GodDomain.Light,     DomainB = GodDomain.Nature,    ResultName = "World Tree Lineage" },
        new() { DomainA = GodDomain.Moon,      DomainB = GodDomain.Knowledge, ResultName = "Oracle Lineage" },
        new() { DomainA = GodDomain.Fire,      DomainB = GodDomain.War,       ResultName = "Flame Dragon Lineage" },
        new() { DomainA = GodDomain.Darkness,  DomainB = GodDomain.Death,     ResultName = "Soul Reaper Lineage" },
        new() { DomainA = GodDomain.Order,     DomainB = GodDomain.Knowledge, ResultName = "Runic Sage Lineage" },
        new() { DomainA = GodDomain.Chaos,     DomainB = GodDomain.Nature,    ResultName = "Wild Mutation Lineage" },
    };
}

public interface IHybridBloodlineService
{
    HybridBloodlineRule? TryFindHybrid(
        List<InheritedBlessingInstance> blessings,
        IReadOnlyDictionary<string, GodDomain> domainByBlessingId);
}

public class HybridBloodlineService : IHybridBloodlineService
{
    private readonly IReadOnlyList<HybridBloodlineRule> _rules;
    private readonly IRandomService _rng;

    public HybridBloodlineService(IReadOnlyList<HybridBloodlineRule> rules, IRandomService rng)
    {
        _rules = rules;
        _rng = rng;
    }

    public HybridBloodlineRule? TryFindHybrid(
        List<InheritedBlessingInstance> blessings,
        IReadOnlyDictionary<string, GodDomain> domainByBlessingId)
    {
        for (int i = 0; i < blessings.Count; i++)
        {
            for (int j = i + 1; j < blessings.Count; j++)
            {
                var a = blessings[i];
                var b = blessings[j];

                if (!domainByBlessingId.TryGetValue(a.BlessingId, out var domainA)) continue;
                if (!domainByBlessingId.TryGetValue(b.BlessingId, out var domainB)) continue;

                var rule = _rules.FirstOrDefault(r =>
                    Matches(r, domainA, domainB) &&
                    a.Strength + b.Strength >= r.MinimumCombinedStrength);

                if (rule != null && _rng.NextFloat() <= rule.MutationChance)
                    return rule;
            }
        }

        return null;
    }

    private static bool Matches(HybridBloodlineRule r, GodDomain a, GodDomain b)
        => (r.DomainA == a && r.DomainB == b) || (r.DomainA == b && r.DomainB == a);
}
