using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC;

/// <summary>
/// Doctrine Integrity System — Add-On v1.2.
/// NPCs who receive divine power must maintain compatibility with their god's doctrine.
/// Acting against core values decreases integrity → reduces divine power.
/// Severe violations trigger Fall events, excommunication, or conversion.
/// </summary>
public interface IDoctrineIntegrityService
{
    Task<float> ApplyViolationAsync(string npcId, string worldId, ViolationSeverity severity, string description, bool isPublic, string? triggeredByGodId = null, long tick = 0);
    Task<float> ApplyResistanceAsync(string npcId, string description, long tick = 0);
    Task<float> GetPowerModifierAsync(string npcId);
    Task CheckFallConditionAsync(string worldId, string npcId, long tick);
    Task UpdateWarningTagsAsync(string npcId);
    Task<List<ViolationEvent>> GetViolationHistoryAsync(string worldId, int limit = 20);
    Task ApplyRedemptionProgressAsync(string npcId, float amount);
}

public class DoctrineIntegrityService : IDoctrineIntegrityService
{
    private readonly INpcRepository _npcRepo;
    private readonly IGodRepository _godRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly ILogger<DoctrineIntegrityService> _logger;

    // God archetype → doctrine tags (what they value / forbid)
    private static readonly Dictionary<GodArchetype, (DoctrineTag[] values, DoctrineTag[] forbidden)> ArchetypeDoctrine = new()
    {
        [GodArchetype.Light]     = (new[] { DoctrineTag.Purity, DoctrineTag.Light, DoctrineTag.Balance },
                                   new[] { DoctrineTag.Darkness, DoctrineTag.Chaos }),
        [GodArchetype.Darkness]  = (new[] { DoctrineTag.Darkness, DoctrineTag.Chaos },
                                   new[] { DoctrineTag.Purity, DoctrineTag.Light }),
        [GodArchetype.War]       = (new[] { DoctrineTag.War },
                                   new[] { DoctrineTag.Balance }),
        [GodArchetype.Knowledge] = (new[] { DoctrineTag.Knowledge },
                                   new[] { DoctrineTag.Chaos }),
        [GodArchetype.Nature]    = (new[] { DoctrineTag.Nature, DoctrineTag.Balance },
                                   new[] { DoctrineTag.War, DoctrineTag.Darkness }),
        [GodArchetype.Order]     = (new[] { DoctrineTag.Order },
                                   new[] { DoctrineTag.Chaos }),
        [GodArchetype.Chaos]     = (new[] { DoctrineTag.Chaos },
                                   new[] { DoctrineTag.Order, DoctrineTag.Purity }),
        [GodArchetype.Death]     = (new[] { DoctrineTag.Death, DoctrineTag.Balance },
                                   new[] { DoctrineTag.Light }),
    };

    // Severity → integrity loss range (GDD §5)
    private static readonly Dictionary<ViolationSeverity, (float min, float max)> SeverityLoss = new()
    {
        [ViolationSeverity.MinorContradiction] = (2f, 5f),
        [ViolationSeverity.ModerateViolation]  = (8f, 15f),
        [ViolationSeverity.MajorViolation]     = (20f, 35f),
        [ViolationSeverity.SevereBetral]       = (40f, 70f),
        [ViolationSeverity.DoctrineInversion]  = (80f, 100f),
    };

    // Integrity gain when NPC resists temptation
    private static readonly Dictionary<ViolationSeverity, float> ResistanceGain = new()
    {
        [ViolationSeverity.MinorContradiction] = 1f,
        [ViolationSeverity.ModerateViolation]  = 5f,
        [ViolationSeverity.MajorViolation]     = 12f,
        [ViolationSeverity.SevereBetral]       = 20f,
        [ViolationSeverity.DoctrineInversion]  = 30f,
    };

    public DoctrineIntegrityService(
        INpcRepository npcRepo,
        IGodRepository godRepo,
        IReligionRepository religionRepo,
        ILogger<DoctrineIntegrityService> logger)
    {
        _npcRepo = npcRepo;
        _godRepo = godRepo;
        _religionRepo = religionRepo;
        _logger = logger;
    }

    // ─── Apply Violation ──────────────────────────────────

    public async Task<float> ApplyViolationAsync(
        string npcId, string worldId, ViolationSeverity severity,
        string description, bool isPublic, string? triggeredByGodId = null, long tick = 0)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null) return 0f;

        var integrity = npc.DivineProfile.DoctrineIntegrity;
        var (min, max) = SeverityLoss[severity];
        float loss = min + (max - min) * 0.5f; // use midpoint; could be randomised

        // High-rank NPCs suffer more dramatically (GDD §5: "stronger rank → more dramatic impact")
        float rankMult = npc.Tier switch
        {
            NpcTier.Royalty    => 1.3f,
            NpcTier.Noble      => 1.1f,
            NpcTier.Adventurer => 1.0f,
            _                  => 0.8f,
        };
        loss *= rankMult;

        integrity.Score = MathF.Max(0f, integrity.Score - loss);
        integrity.LastViolationTick = tick;
        integrity.ViolationHistory.Insert(0, $"[{severity}] {description}");
        if (integrity.ViolationHistory.Count > 20)
            integrity.ViolationHistory = integrity.ViolationHistory.Take(20).ToList();

        // Faith/devotion impact
        if (isPublic)
        {
            npc.DevotionLevel = MathF.Max(0f, npc.DevotionLevel - loss * 0.005f);
        }

        await UpdateWarningTagsAsync(npc);
        await _npcRepo.UpdateAsync(npc);

        // Check fall condition
        await CheckFallConditionAsync(worldId, npcId, tick);

        _logger.LogInformation("NPC {Name} doctrine violation [{Severity}] -{Loss:F1} integrity → {Score:F0} ({Status})",
            npc.Name, severity, loss, integrity.Score, integrity.Status);

        return integrity.Score;
    }

    // ─── Apply Resistance ─────────────────────────────────

    public async Task<float> ApplyResistanceAsync(string npcId, string description, long tick = 0)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null) return 0f;

        var integrity = npc.DivineProfile.DoctrineIntegrity;
        float gain = 5f; // base resistance reward

        // Higher severity resistance = bigger reward
        float trustGain = 8f;
        npc.GodTrustLevel = MathF.Min(100f, npc.GodTrustLevel + trustGain);
        npc.DevotionLevel = MathF.Min(1f, npc.DevotionLevel + 0.03f);
        integrity.Score = MathF.Min(100f, integrity.Score + gain);

        integrity.ViolationHistory.Insert(0, $"[Resisted] {description}");
        await UpdateWarningTagsAsync(npc);
        await _npcRepo.UpdateAsync(npc);

        _logger.LogInformation("NPC {Name} resisted temptation → integrity +{Gain:F1} = {Score:F0}",
            npc.Name, gain, integrity.Score);
        return integrity.Score;
    }

    // ─── Power Modifier ───────────────────────────────────

    /// <summary>
    /// Returns the doctrine integrity multiplier used in:
    /// FinalDivinePower = Base × FaithLevel × DoctrineIntegrityModifier × GodFavor × RelicBonus × CorruptionModifier
    /// </summary>
    public async Task<float> GetPowerModifierAsync(string npcId)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        return npc?.DivineProfile.DoctrineIntegrity.PowerModifier ?? 1f;
    }

    // ─── Fall Condition ───────────────────────────────────

    /// <summary>
    /// Check if integrity has fallen low enough to trigger a Fall Event.
    /// DoctrineInversion (0-24) → NPC becomes Fallen Saint, Demon Vessel, or enemy champion.
    /// </summary>
    public async Task CheckFallConditionAsync(string worldId, string npcId, long tick)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null) return;

        var integrity = npc.DivineProfile.DoctrineIntegrity;
        var profile = npc.DivineProfile;

        if (integrity.Status != DoctrineIntegrityStatus.Broken) return;

        // Determine fall type based on original path
        if (profile.IsSaintCandidate || profile.ChurchRank >= ChurchRank.Saint)
        {
            // Holy NPC falls
            profile.ChurchRank = ChurchRank.BloodSaint;  // intermediate fallen rank
            profile.IsSaintCandidate = false;
            profile.IsDarkPathCandidate = true;
            integrity.IsExcommunicated = true;
            integrity.ViolationHistory.Insert(0, "[FALL] Saint fallen from grace — excommunicated");
            _logger.LogWarning("NPC {Name} has FALLEN from Saint path (integrity={Score:F0})", npc.Name, integrity.Score);
        }
        else if (profile.IsProphetCandidate)
        {
            // False Prophet
            profile.IsProphetCandidate = false;
            profile.IsDarkPathCandidate = true;
            integrity.IsExcommunicated = true;
            integrity.ViolationHistory.Insert(0, "[FALL] Prophet became a False Prophet");
            _logger.LogWarning("NPC {Name} has FALLEN — False Prophet path", npc.Name);
        }
        else if (npc.IsChampion && profile.ChampionPath == ChampionPath.Saint)
        {
            // Disgraced Champion
            npc.IsChampion = false;
            profile.IsChampionCandidate = false;
            integrity.ViolationHistory.Insert(0, "[FALL] Champion disgraced — power stripped");
            _logger.LogWarning("NPC {Name} disgraced Champion", npc.Name);
        }

        await UpdateWarningTagsAsync(npc);
        await _npcRepo.UpdateAsync(npc);
    }

    // ─── Warning Tags ─────────────────────────────────────

    private async Task UpdateWarningTagsAsync(NpcDocument npc)
    {
        await UpdateWarningTagsAsync_Internal(npc);
    }

    public async Task UpdateWarningTagsAsync(string npcId)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null) return;
        await UpdateWarningTagsAsync_Internal(npc);
        await _npcRepo.UpdateAsync(npc);
    }

    private Task UpdateWarningTagsAsync_Internal(NpcDocument npc)
    {
        var integrity = npc.DivineProfile.DoctrineIntegrity;
        var profile = npc.DivineProfile;
        var warnings = new List<GodNoteWarningTag>();

        // Pure Candidate: high integrity + saint/prophet path
        if (integrity.Score >= 85f && (profile.IsSaintCandidate || profile.IsProphetCandidate))
            warnings.Add(GodNoteWarningTag.PureCandidate);

        // Shaken Faith: 50-69
        if (integrity.Status == DoctrineIntegrityStatus.Shaken)
            warnings.Add(GodNoteWarningTag.ShakenFaith);

        // Compromised: 25-49
        if (integrity.Status == DoctrineIntegrityStatus.Compromised)
            warnings.Add(GodNoteWarningTag.Compromised);

        // At Risk of Fall: Broken or nearly so
        if (integrity.Score < 30f || integrity.IsExcommunicated)
            warnings.Add(GodNoteWarningTag.AtRiskOfFall);

        // Protected Asset: high-value NPC with escort
        if (profile.AssignedEscort?.IsActive == true)
            warnings.Add(GodNoteWarningTag.ProtectedAsset);

        npc.DivineProfile.ActiveWarnings = warnings;
        npc.DivineProfile.DoctrineIntegrity.ActiveWarnings = warnings;
        return Task.CompletedTask;
    }

    // ─── Violation History ────────────────────────────────

    public async Task<List<ViolationEvent>> GetViolationHistoryAsync(string worldId, int limit = 20)
    {
        // Aggregate from all NPCs — in production this would be a separate collection
        var npcs = await _npcRepo.GetByWorldAsync(worldId);
        var events = new List<ViolationEvent>();
        foreach (var npc in npcs.Where(n => n.DivineProfile.DoctrineIntegrity.ViolationHistory.Any()).Take(50))
        {
            foreach (var entry in npc.DivineProfile.DoctrineIntegrity.ViolationHistory.Take(3))
            {
                events.Add(new ViolationEvent
                {
                    NpcId = npc.Id,
                    WorldId = worldId,
                    Description = $"{npc.Name}: {entry}",
                    Tick = npc.DivineProfile.DoctrineIntegrity.LastViolationTick
                });
            }
        }
        return events.OrderByDescending(e => e.Tick).Take(limit).ToList();
    }

    // ─── Redemption ───────────────────────────────────────

    public async Task ApplyRedemptionProgressAsync(string npcId, float amount)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null) return;

        var integrity = npc.DivineProfile.DoctrineIntegrity;
        integrity.RedemptionProgress = MathF.Min(100f, integrity.RedemptionProgress + amount);

        // Full redemption restores integrity
        if (integrity.RedemptionProgress >= 100f)
        {
            integrity.Score = MathF.Min(100f, integrity.Score + 25f);
            integrity.RedemptionProgress = 0f;
            integrity.IsExcommunicated = false;
            integrity.ViolationHistory.Insert(0, "[REDEEMED] Redemption quest completed — integrity restored");
            _logger.LogInformation("NPC {Name} completed redemption — integrity restored to {Score:F0}", npc.Name, integrity.Score);
        }

        await UpdateWarningTagsAsync(npc);
        await _npcRepo.UpdateAsync(npc);
    }
}
