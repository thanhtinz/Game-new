using WorldFaith.Server.Models;

namespace WorldFaith.Server.Services.NPC;

// ─── Population-Scale Faith (NPC Master Spec §10) ─────────────
// Ordinary citizens are simulated in groups for performance. A conversion
// event shifts only a fraction of a group and splits it, so a village can be
// "980 old faith, 20 Nature god" instead of an all-or-nothing flip.

public interface IPopulationFaithService
{
    /// <summary>
    /// Move a fraction (pressure, capped at 25%/event) of a group to a target god,
    /// merging into an existing matching group in <paramref name="groups"/> or
    /// creating a new one. Returns the number of members converted.
    /// </summary>
    int ApplyGroupConversionPressure(
        List<PopulationFaithGroup> groups,
        PopulationFaithGroup group,
        float pressure,
        string targetGodId,
        string? targetReligionId = null);
}

public class PopulationFaithService : IPopulationFaithService
{
    public int ApplyGroupConversionPressure(
        List<PopulationFaithGroup> groups,
        PopulationFaithGroup group,
        float pressure,
        string targetGodId,
        string? targetReligionId = null)
    {
        int converts = (int)MathF.Round(group.Count * Math.Clamp(pressure, 0f, 0.25f));
        converts = Math.Min(converts, group.Count);
        if (converts <= 0) return 0;

        group.Count -= converts;

        // Merge into an existing same-region/race/class group already following the target god.
        var target = groups.FirstOrDefault(g =>
            !ReferenceEquals(g, group) &&
            g.CivilizationId == group.CivilizationId &&
            g.RegionId == group.RegionId &&
            g.Race == group.Race &&
            g.Class == group.Class &&
            g.GodId == targetGodId);

        if (target != null)
        {
            target.Count += converts;
        }
        else
        {
            groups.Add(new PopulationFaithGroup
            {
                WorldId = group.WorldId,
                CivilizationId = group.CivilizationId,
                RegionId = group.RegionId,
                Race = group.Race,
                Class = group.Class,
                GodId = targetGodId,
                ReligionId = targetReligionId,
                Count = converts,
                AverageDevotion = 0.35f,   // fresh converts start lukewarm
                AverageTrust = 45f
            });
        }

        return converts;
    }
}
