using WorldFaith.Server.Models;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// ─── Succession (Dynasty Spec §11) ────────────────────────────
// Chooses a family's heir among living members. Legitimacy (recognized birth)
// can matter more than raw bloodline strength; adult seniority breaks ties.

public interface ISuccessionService
{
    NpcDocument? ChooseHeir(FamilyHouseDocument family, IEnumerable<NpcDocument> candidates, int currentYear);
    float ScoreHeir(NpcDocument candidate, int currentYear);
}

public class SuccessionService : ISuccessionService
{
    public NpcDocument? ChooseHeir(FamilyHouseDocument family, IEnumerable<NpcDocument> candidates, int currentYear)
    {
        return candidates
            .Where(c => c.DeathYear == null
                     && (c.FamilyId == family.Id || family.LivingMemberIds.Contains(c.Id)))
            .OrderByDescending(c => ScoreHeir(c, currentYear))
            .FirstOrDefault();
    }

    public float ScoreHeir(NpcDocument c, int currentYear)
    {
        float bloodline = c.InheritedBlessings.Sum(b => b.Strength);
        // Recognized birth (both parents known) confers legitimacy.
        float legitimacy = c.FatherNpcId != null && c.MotherNpcId != null ? 30f : 0f;
        // Favor an established adult heir; cap so age alone can't dominate.
        float seniority = Math.Clamp(currentYear - c.BirthYear, 0, 60);
        return bloodline + legitimacy + seniority;
    }
}
