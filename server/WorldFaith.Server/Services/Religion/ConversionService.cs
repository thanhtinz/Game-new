using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Race;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Religion;

/// <summary>
/// Conversion System — GDD v1.0 Section 23 (Balance Formulas).
/// ConversionChance = Openness × RaceAffinity × SocialPressure × TrustDiff × RecentEvents × TraitModifier
/// TrustChange = MiracleResult × AttributionCertainty × DoctrineMatch × HarmModifier
/// </summary>
public interface IConversionService
{
    Task<float> CalculateConversionChanceAsync(
        string worldId, NpcDocument npc, ReligionDocument targetReligion, CivilizationDocument civ);
    Task<float> CalculateTrustChangeAsync(
        string religionId, string miracleType, bool success, bool harmOccurred, float certainty);
    Task<bool> AttemptConversionAsync(
        string worldId, NpcDocument npc, ReligionDocument targetReligion, CivilizationDocument civ);
}

public class ConversionService : IConversionService
{
    private readonly IRaceAffinityService _raceAffinity;
    private readonly IDoctrineService _doctrine;
    private readonly IGodRepository _godRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IBelieverTypeService _believerTypes;
    private readonly ILogger<ConversionService> _logger;
    private readonly Random _rng = new();

    public ConversionService(
        IRaceAffinityService raceAffinity,
        IDoctrineService doctrine,
        IGodRepository godRepo,
        IReligionRepository religionRepo,
        IBelieverTypeService believerTypes,
        ILogger<ConversionService> logger)
    {
        _raceAffinity = raceAffinity;
        _doctrine = doctrine;
        _godRepo = godRepo;
        _religionRepo = religionRepo;
        _believerTypes = believerTypes;
        _logger = logger;
    }

    // ─── Conversion Chance ────────────────────────────────────

    /// <summary>
    /// GDD v1.0 Formula:
    /// ConversionChance = Openness × RaceAffinity × SocialPressure × TrustDifference
    ///                  × RecentEvents × TraitModifier
    /// </summary>
    public async Task<float> CalculateConversionChanceAsync(
        string worldId, NpcDocument npc, ReligionDocument targetReligion, CivilizationDocument civ)
    {
        // 1. Base openness by NPC tier (lower tiers convert more easily)
        float openness = npc.Tier switch
        {
            NpcTier.Commoner   => 0.8f,
            NpcTier.Servant    => 0.6f,
            NpcTier.Adventurer => 0.5f,
            NpcTier.Noble      => 0.3f,
            NpcTier.Royalty    => 0.15f,
            _ => 0.5f
        };

        // Traditional trait lowers openness
        if (npc.Personality == NpcPersonality.Pious && npc.PersonalReligionId != null)
            openness *= 0.5f;

        // Curious personality raises openness
        if (npc.Personality == NpcPersonality.Idealistic)
            openness *= 1.3f;

        // 2. Race Affinity modifier
        var god = await _godRepo.GetByIdAsync(targetReligion.GodId);
        float raceAffinity = god != null
            ? await _raceAffinity.GetConversionModifierAsync(worldId, civ.PrimaryRace, god.Archetype, new())
            : 1f;

        // Environmental memory modifier
        if (god != null)
        {
            var raceDoc = await _raceAffinity.GetAffinityAsync(worldId, civ.PrimaryRace, god.Archetype);
            // Environmental penalty/bonus already baked into stored affinity via RecordEnvironmentalMemory
        }

        // 3. Social pressure — if civ ruling religion is this religion
        float socialPressure = 1.0f;
        if (civ.RulingReligionId == targetReligion.Id)
            socialPressure = 1.5f;  // Ruling religion has home field advantage
        else if (targetReligion.IsHidden)
            socialPressure = 0.7f;  // Secret cult harder to find

        // Government modifier on spread
        float govMod = civ.Government switch
        {
            GovernmentType.Theocracy   => 1.4f,  // Priests push hard
            GovernmentType.Monarchy    => 1.2f,  // Royal endorsement helps
            GovernmentType.NobleCouncil=> 1.0f,
            GovernmentType.TribalClan  => 0.9f,
            GovernmentType.MerchantState=>0.8f,  // Faith is pragmatic
            GovernmentType.MonsterHorde=> 0.7f,
            _ => 1.0f
        };
        socialPressure *= govMod;

        // 4. Trust difference — how much does NPC trust this god vs their current god
        float trustDiff = (npc.GodTrustLevel - 50f) / 100f;  // -0.5 to +0.5
        float trustModifier = 1f + trustDiff;

        // 5. Recent events (tracked via NPC's devotion level as proxy)
        float recentEventModifier = 1f + (npc.Piety - 50f) / 100f;  // piety up = more likely

        // 6. Doctrine compatibility with NPC's personality
        float doctrineModifier = await GetDoctrinePersonalityMatchAsync(targetReligion.Id, npc);

        // Final formula
        float chance = openness * raceAffinity * socialPressure * trustModifier * recentEventModifier * doctrineModifier;

        // Cap at reasonable range
        return Math.Clamp(chance * 0.1f, 0.001f, 0.3f); // Max 30% per attempt
    }

    private async Task<float> GetDoctrinePersonalityMatchAsync(string religionId, NpcDocument npc)
    {
        var doc = await _doctrine.GetDoctrineAsync(religionId);
        float match = 1f;

        // Ambitious NPC prefers Power doctrine
        if (npc.Personality == NpcPersonality.Ambitious)
        {
            match += (doc.HarmonyVsDominion) / 100f * 0.3f;
            match += (doc.FreedomVsOrder)    / 100f * 0.2f;
        }

        // Pious NPC prefers Sacrifice doctrine
        if (npc.Personality == NpcPersonality.Pious)
        {
            match += (doc.SacrificeVsProsperity * -1f) / 100f * 0.3f;
        }

        // Loyal NPC prefers Order
        if (npc.Personality == NpcPersonality.Loyal)
        {
            match += (doc.FreedomVsOrder) / 100f * 0.2f;
        }

        // Corrupt NPC prefers Punishment (power to punish enemies)
        if (npc.Personality == NpcPersonality.Corrupt)
        {
            match += (doc.MercyVsPunishment) / 100f * 0.3f;
        }

        return Math.Clamp(match, 0.3f, 2.0f);
    }

    // ─── Trust Change ─────────────────────────────────────────

    /// <summary>
    /// GDD v1.0 Formula:
    /// TrustChange = MiracleResultValue × AttributionCertainty × DoctrineMatch × HarmModifier
    /// </summary>
    public async Task<float> CalculateTrustChangeAsync(
        string religionId, string miracleType, bool success, bool harmOccurred, float certainty)
    {
        // Base value from miracle outcome
        float resultValue = success ? 15f : -10f;

        if (harmOccurred) resultValue -= 20f; // Civilians hurt = big trust loss

        // Attribution certainty (0-1): was the miracle clearly caused by this god?
        // High certainty = full effect. Low = diluted (mortals might attribute to rival)
        float trustChange = resultValue * Math.Clamp(certainty, 0f, 1f);

        // Doctrine affects how civ interprets miracles
        var doc = await _doctrine.GetDoctrineAsync(religionId);

        // Disaster miracles: if doctrine is Punishment, followers expect it
        if (!success && doc.MercyVsPunishment > 30f)
            trustChange *= 0.5f; // "God punishes, that's expected" — less trust loss

        // BlessHarvest success with Prosperity doctrine: extra trust gain
        if (success && miracleType == "BlessHarvest" && doc.SacrificeVsProsperity > 20f)
            trustChange *= 1.3f;

        return trustChange;
    }

    // ─── Attempt Conversion ───────────────────────────────────

    public async Task<bool> AttemptConversionAsync(
        string worldId, NpcDocument npc, ReligionDocument targetReligion, CivilizationDocument civ)
    {
        float chance = await CalculateConversionChanceAsync(worldId, npc, targetReligion, civ);

        if (_rng.NextDouble() > chance) return false;

        // Conversion succeeds
        string? oldReligionId = npc.PersonalReligionId;
        npc.PersonalReligionId = targetReligion.Id;
        npc.GodTrustLevel = 30f; // Start fresh
        npc.DevotionLevel = 0.3f;

        // Update religion follower counts
        targetReligion.FollowerCount++;
        await _religionRepo.UpdateAsync(targetReligion);

        // Shift believer type based on how they converted
        bool wasCoerced = civ.RulingReligionId == targetReligion.Id && npc.Tier == NpcTier.Commoner;
        await _believerTypes.ShiftBelieverTypesAsync(targetReligion.Id,
            wasCoerced ? "MassConversion" : "ProphecyFulfilled", 0.01f);

        // Social pressure conversion: high tier NPC converts → others follow
        if (npc.Tier >= NpcTier.Noble)
        {
            _logger.LogInformation("{Tier} {Name} converted to {Religion} — may trigger cascade",
                npc.Tier, npc.Name, targetReligion.Name);
        }

        return true;
    }
}
