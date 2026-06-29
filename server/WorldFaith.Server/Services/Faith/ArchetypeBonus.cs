using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Faith;

/// <summary>
/// Tính bonus Faith và modifier theo God Archetype.
/// Được gọi từ FaithService và MiracleService.
/// </summary>
public static class ArchetypeBonus
{
    /// <summary>
    /// Multiplier faith generation theo archetype + game state.
    /// </summary>
    public static float GetFaithGenMultiplier(
        GodDocument god,
        List<ReligionDocument> godReligions,
        List<CivilizationDocument> civs)
    {
        float mult = 1f;

        switch (god.Archetype)
        {
            case GodArchetype.Light:
                // +20% từ followers có Trust cao
                float avgTrust = god.Trust;
                if (avgTrust > 70f) mult += 0.2f;
                break;

            case GodArchetype.Darkness:
            case GodArchetype.Death:
                // Already handled via fear bonus in FaithService
                break;

            case GodArchetype.Chaos:
                // +15% nếu có civ đang war
                bool anyWar = civs.Any(c =>
                    godReligions.Any(r => r.CivilizationIds.Contains(c.Id)) && c.IsAtWar);
                if (anyWar) mult += 0.15f;
                break;

            case GodArchetype.Order:
                // +10% từ temples
                int temples = godReligions.Sum(r => r.TempleCount);
                mult += temples * 0.01f;
                break;

            case GodArchetype.War:
                // +10% nếu civ đang chiến thắng (Military > 80)
                bool strongCiv = civs.Any(c =>
                    godReligions.Any(r => r.CivilizationIds.Contains(c.Id))
                    && c.Military > 80f && c.IsAtWar);
                if (strongCiv) mult += 0.10f;
                break;

            case GodArchetype.Knowledge:
                // +5% faith per 10 miracles performed (tracked via god.LastActionAt as proxy)
                mult += 0.05f; // flat bonus vì không track miracle count per god
                break;

            case GodArchetype.Nature:
                // +10% từ Forest tiles religion
                mult += 0.10f;
                break;
        }

        return MathF.Min(mult, 3f); // Cap 3x
    }

    /// <summary>
    /// Miracle cost modifier theo archetype — trả về multiplier lên cost (< 1 = giảm giá).
    /// </summary>
    public static float GetMiracleCostMultiplier(GodArchetype archetype, MiracleType miracle)
    {
        return archetype switch
        {
            GodArchetype.Order when miracle == MiracleType.Revelation
                => 0.80f,

            GodArchetype.Light when miracle == MiracleType.HealFollower
                => 0f,    // Miễn phí

            GodArchetype.Light when miracle is MiracleType.BlessHarvest or MiracleType.Dream
                => 0.85f,

            GodArchetype.Darkness when miracle == MiracleType.Curse
                => 0.5f,

            GodArchetype.Darkness when miracle == MiracleType.DemonInvasion
                => 0.80f,

            GodArchetype.Chaos when miracle == MiracleType.Storm or MiracleType.Earthquake
                => 0.85f,

            GodArchetype.War when miracle == MiracleType.HolyWar
                => 0.70f,

            GodArchetype.War when miracle == MiracleType.DemonInvasion
                => 0.85f,

            GodArchetype.Knowledge when miracle == MiracleType.DivineVoice
                => 0.70f,

            GodArchetype.Knowledge when miracle == MiracleType.Revelation
                => 0.85f,

            GodArchetype.Nature when miracle == MiracleType.DivineBeastCreation
                => 0.80f,

            GodArchetype.Nature when miracle == MiracleType.Rain
                => 0.50f,

            GodArchetype.Death when miracle == MiracleType.Curse
                => 0.60f,

            _ => 1f // không có discount
        };
    }

    /// <summary>
    /// Miracle effect multiplier — khuếch đại hiệu ứng miracle theo archetype.
    /// </summary>
    public static float GetMiracleEffectMultiplier(GodArchetype archetype, MiracleType miracle)
    {
        return archetype switch
        {
            GodArchetype.Darkness when miracle == MiracleType.Curse
                => 2.0f, // Curse x2 damage

            GodArchetype.Knowledge when miracle == MiracleType.DivineVoice
                => 1.5f, // DivineVoice trust bonus x1.5

            GodArchetype.War when miracle == MiracleType.HolyWar
                => 1.5f, // HolyWar duration/effect x1.5

            GodArchetype.Nature when miracle == MiracleType.Rain
                => 2.0f, // Rain fertility x2

            GodArchetype.Light when miracle == MiracleType.BlessHarvest
                => 1.5f, // BlessHarvest x1.5

            GodArchetype.Chaos
                => 0.8f + (new Random().NextSingle() * 0.8f), // Chaos: 80%-160% random

            _ => 1f
        };
    }
}
