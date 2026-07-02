using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Religion;

/// <summary>
/// Doctrine System — GDD v1.0 Section 13.
/// Each religion has 5 doctrine axes, ảnh hưởng AI behavior.
/// Missionary behavior, crime response, race reactions, royal support.
/// </summary>
public interface IDoctrineService
{
    Task SetDoctrineAsync(string religionId, string axis, float value);
    Task<DoctrineValues> GetDoctrineAsync(string religionId);
    Task<float> GetMissionarySpeedModifierAsync(string religionId);
    Task<float> GetCrimeResponseModifierAsync(string religionId);
    Task<float> GetRoyalSupportModifierAsync(string religionId);
    Task<float> GetRaceCompatibilityModifierAsync(string religionId, RaceType race);
    Task<bool> ShouldExecuteHereticAsync(string religionId);
    Task EvolveDoctrineFromEventAsync(string religionId, string eventType, float magnitude);
}

public class DoctrineService : IDoctrineService
{
    private readonly IReligionRepository _religionRepo;
    private readonly ILogger<DoctrineService> _logger;

    public DoctrineService(IReligionRepository religionRepo, ILogger<DoctrineService> logger)
    {
        _religionRepo = religionRepo;
        _logger = logger;
    }

    // ─── Set/Get Doctrine ────────────────────────────────────

    public async Task SetDoctrineAsync(string religionId, string axis, float value)
    {
        var religion = await _religionRepo.GetByIdAsync(religionId);
        if (religion == null) return;

        float clamped = Math.Clamp(value, -100f, 100f);

        switch (axis.ToLower())
        {
            case "mercy":     case "mercyvspunishment":      religion.Doctrine.MercyVsPunishment    = clamped; break;
            case "isolation": case "isolationvsexpansion":   religion.Doctrine.IsolationVsExpansion = clamped; break;
            case "harmony":   case "harmonyvsdominion":      religion.Doctrine.HarmonyVsDominion    = clamped; break;
            case "freedom":   case "freedomvsorder":         religion.Doctrine.FreedomVsOrder       = clamped; break;
            case "sacrifice": case "sacrificevsprosperity":  religion.Doctrine.SacrificeVsProsperity= clamped; break;
        }

        await _religionRepo.UpdateAsync(religion);
        _logger.LogInformation("Doctrine updated for '{Religion}': {Axis} = {Value:+0.0;-0.0}", religion.Name, axis, clamped);
    }

    public async Task<DoctrineValues> GetDoctrineAsync(string religionId)
    {
        var religion = await _religionRepo.GetByIdAsync(religionId);
        return religion?.Doctrine ?? new DoctrineValues();
    }

    // ─── Gameplay Modifiers ───────────────────────────────────

    /// <summary>
    /// Expansion doctrine → faster spread.
    /// Isolation doctrine → protects existing followers but converts slower.
    /// </summary>
    public async Task<float> GetMissionarySpeedModifierAsync(string religionId)
    {
        var doc = await GetDoctrineAsync(religionId);
        // IsolationVsExpansion: -100 (isolation) → 0.5x, +100 (expansion) → 2.0x
        return 1f + (doc.IsolationVsExpansion / 100f) * 1.0f;
    }

    /// <summary>
    /// Punishment doctrine → aggressive heresy suppression (Heresy Trial chance +).
    /// Mercy doctrine → lenient (lower schism trigger).
    /// </summary>
    public async Task<float> GetCrimeResponseModifierAsync(string religionId)
    {
        var doc = await GetDoctrineAsync(religionId);
        // MercyVsPunishment: -100 (mercy) → crime tolerated, +100 (punishment) → harsh response
        return 1f + (doc.MercyVsPunishment / 100f) * 0.5f;
    }

    /// <summary>
    /// Order doctrine → nobles and royals support strongly.
    /// Freedom doctrine → commoners like it but elites resist.
    /// </summary>
    public async Task<float> GetRoyalSupportModifierAsync(string religionId)
    {
        var doc = await GetDoctrineAsync(religionId);
        // FreedomVsOrder: -100 (freedom) → royals dislike, +100 (order) → royals support
        return 1f + (doc.FreedomVsOrder / 100f) * 0.6f;
    }

    /// <summary>
    /// Doctrine affects how different races receive this religion.
    /// Harmony → nature races like elves, beastfolk.
    /// Dominion → orcs, demons respond better.
    /// Sacrifice → undead, death cultists.
    /// </summary>
    public async Task<float> GetRaceCompatibilityModifierAsync(string religionId, RaceType race)
    {
        var doc = await GetDoctrineAsync(religionId);
        float modifier = 1f;

        switch (race)
        {
            case RaceType.Elf:
            case RaceType.Beastfolk:
                // Love Harmony, hate Dominion
                modifier += (doc.HarmonyVsDominion * -1f) / 100f * 0.4f;
                // Love Sacrifice (nature cycle)
                modifier += (doc.SacrificeVsProsperity * -1f) / 100f * 0.2f;
                break;

            case RaceType.Orc:
            case RaceType.Demon:
                // Love Dominion, hate Harmony
                modifier += (doc.HarmonyVsDominion) / 100f * 0.4f;
                // Love Punishment
                modifier += (doc.MercyVsPunishment) / 100f * 0.3f;
                break;

            case RaceType.Dwarf:
                // Love Order
                modifier += (doc.FreedomVsOrder) / 100f * 0.4f;
                // Love Isolation (tradition)
                modifier += (doc.IsolationVsExpansion * -1f) / 100f * 0.2f;
                break;

            case RaceType.Angel:
                // Love Mercy and Freedom
                modifier += (doc.MercyVsPunishment * -1f) / 100f * 0.5f;
                modifier += (doc.FreedomVsOrder * -1f) / 100f * 0.3f;
                break;

            case RaceType.Undead:
                // Love Sacrifice
                modifier += (doc.SacrificeVsProsperity * -1f) / 100f * 0.5f;
                break;

            case RaceType.Human:
                // Neutral — slight Order preference
                modifier += (doc.FreedomVsOrder) / 100f * 0.1f;
                break;
        }

        return Math.Clamp(modifier, 0.3f, 2.5f);
    }

    /// <summary>
    /// High Punishment doctrine → heretics executed (not tolerated).
    /// </summary>
    public async Task<bool> ShouldExecuteHereticAsync(string religionId)
    {
        var doc = await GetDoctrineAsync(religionId);
        // Punishment > 50 and Order > 30 → execute
        return doc.MercyVsPunishment > 50f && doc.FreedomVsOrder > 30f;
    }

    // ─── Doctrine Evolution ───────────────────────────────────

    /// <summary>
    /// Events shift doctrine over time.
    /// Failed miracle → faith crisis → doctrine softens (more Mercy, less Order).
    /// Successful holy war → doctrine hardens (more Punishment, more Dominion).
    /// Schism → faction takes extreme doctrine position.
    /// </summary>
    public async Task EvolveDoctrineFromEventAsync(string religionId, string eventType, float magnitude)
    {
        var religion = await _religionRepo.GetByIdAsync(religionId);
        if (religion == null) return;

        float shift = magnitude * 5f; // max shift per event

        switch (eventType)
        {
            case "FailedMiracle":
                // Followers question → shift toward Mercy (forgiveness), Freedom
                religion.Doctrine.MercyVsPunishment    -= shift;
                religion.Doctrine.FreedomVsOrder       -= shift * 0.5f;
                break;

            case "HolyWarWon":
                // Victory validates Punishment and Dominion
                religion.Doctrine.MercyVsPunishment    += shift;
                religion.Doctrine.HarmonyVsDominion    += shift;
                break;

            case "DisasterSurvived":
                // Sacrifice has meaning
                religion.Doctrine.SacrificeVsProsperity -= shift;
                break;

            case "RichFollowerConverted":
                // Prosperity gains weight
                religion.Doctrine.SacrificeVsProsperity += shift * 0.5f;
                break;

            case "Schism":
                // Main branch might become more rigid (Order) after schism
                religion.Doctrine.FreedomVsOrder       += shift * 0.3f;
                religion.Doctrine.IsolationVsExpansion += shift * 0.2f;
                break;

            case "HeresyTrial":
                // Successful suppression → more Punishment
                religion.Doctrine.MercyVsPunishment    += shift * 0.5f;
                break;
        }

        // Clamp all axes
        religion.Doctrine.MercyVsPunishment    = Math.Clamp(religion.Doctrine.MercyVsPunishment, -100f, 100f);
        religion.Doctrine.IsolationVsExpansion = Math.Clamp(religion.Doctrine.IsolationVsExpansion, -100f, 100f);
        religion.Doctrine.HarmonyVsDominion    = Math.Clamp(religion.Doctrine.HarmonyVsDominion, -100f, 100f);
        religion.Doctrine.FreedomVsOrder       = Math.Clamp(religion.Doctrine.FreedomVsOrder, -100f, 100f);
        religion.Doctrine.SacrificeVsProsperity= Math.Clamp(religion.Doctrine.SacrificeVsProsperity, -100f, 100f);

        await _religionRepo.UpdateAsync(religion);
        _logger.LogDebug("Doctrine evolved for '{Religion}' after {Event}: M/P={MP:+0;-0}", 
            religion.Name, eventType, religion.Doctrine.MercyVsPunishment);
    }

    // Helper interface needed by ReligionRepository
    private async Task UpdateAsync(ReligionDocument religion) { }
}
