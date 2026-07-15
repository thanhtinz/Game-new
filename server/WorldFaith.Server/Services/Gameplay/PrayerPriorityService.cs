using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Gameplay;

// ─── Prayer & Request Board (Gameplay Spec §7.2–7.3) ──────────
// Collects requests directed at the player's god and ranks them so 700 NPCs do
// not produce 700 equal alerts. The board reports requests; deciding whether to
// answer is gameplay (§7.3 critical rule).

public enum PrayerChannel { IndividualPetition, GroupRitual }

public class PrayerRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PetitionerNpcId { get; set; } = string.Empty;
    public string? TargetNpcId { get; set; }
    public PrayerChannel Channel { get; set; }
    public string? Location { get; set; }

    // Critical triggers (§7.3)
    public bool ImmediateThreatToLife { get; set; }
    public bool ThreatToSacredSite { get; set; }
    public bool ThreatToSettlement { get; set; }
    public bool PartOfMajorStory { get; set; }

    /// <summary>0-100 severity of the consequence if the request is ignored.</summary>
    public float ConsequenceIfIgnored { get; set; }

    /// <summary>Daily wish, gratitude, minor need, or a repeated ritual.</summary>
    public bool IsRoutine { get; set; }

    // Resolution flags (§7.3 "Resolved")
    public bool IsAnswered { get; set; }
    public bool IsWithdrawn { get; set; }
    public bool IsExpired { get; set; }
    public bool IsFailed { get; set; }
    public bool IsFulfilledByMortals { get; set; }
}

public interface IPrayerPriorityService
{
    RequestPriority Classify(PrayerRequest request);
    IReadOnlyList<PrayerRequest> RankActive(IEnumerable<PrayerRequest> requests);
}

public class PrayerPriorityService : IPrayerPriorityService
{
    private const float ImportantThreshold = 40f;

    public RequestPriority Classify(PrayerRequest r)
    {
        if (r.IsAnswered || r.IsWithdrawn || r.IsExpired || r.IsFailed || r.IsFulfilledByMortals)
            return RequestPriority.Resolved;

        if (r.ImmediateThreatToLife || r.ThreatToSacredSite || r.ThreatToSettlement || r.PartOfMajorStory)
            return RequestPriority.Critical;

        if (!r.IsRoutine && r.ConsequenceIfIgnored >= ImportantThreshold)
            return RequestPriority.Important;

        return RequestPriority.Routine;
    }

    /// <summary>Unresolved requests, most urgent first (Critical → Important → Routine).</summary>
    public IReadOnlyList<PrayerRequest> RankActive(IEnumerable<PrayerRequest> requests)
        => requests
            .Select(r => (req: r, pri: Classify(r)))
            .Where(x => x.pri != RequestPriority.Resolved)
            .OrderBy(x => (int)x.pri)                              // Critical(0) first
            .ThenByDescending(x => x.req.ConsequenceIfIgnored)
            .Select(x => x.req)
            .ToList();
}
