using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Gameplay;

// ─── Connected NPC & Family Stories (Gameplay Spec §11) ───────
// NPC biographies are built from shared historical events, not independent
// random sentences. One family event creates ONE record with linked per-
// participant perspectives: everyone remembers the same core fact but may
// interpret it differently — and nobody gets a contradictory core history.

public enum StoryThreadState { Open, Developing, Resolved, Inherited, Forgotten }

/// <summary>How one participant experienced / remembers a shared event (§11.2).</summary>
public class StoryPerspective
{
    public string NpcId { get; set; } = string.Empty;
    public StoryRole Role { get; set; }
    public string Belief { get; set; } = string.Empty;   // what this NPC believes happened
    public EventConfidence Certainty { get; set; } = EventConfidence.Likely;
    public bool Knows { get; set; } = true;              // do they know the event happened at all
    public bool IsSecretHolder { get; set; }             // knows a hidden aspect others don't
}

/// <summary>Input describing one participant when creating a shared event.</summary>
public record StoryParticipantSpec(
    string NpcId,
    StoryRole Role,
    string Belief,
    EventConfidence Certainty = EventConfidence.Likely,
    bool Knows = true,
    bool IsSecretHolder = false);

/// <summary>The single objective event shared by all participants (§11.2).</summary>
public class StoryEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CoreFact { get; set; } = string.Empty;   // objective truth of the simulation
    public int Year { get; set; }
    public string? Location { get; set; }
    public string Outcome { get; set; } = string.Empty;
    public StoryThreadState ThreadState { get; set; } = StoryThreadState.Open;
    public List<StoryPerspective> Perspectives { get; set; } = new();
}

public interface IStoryEventService
{
    StoryEvent CreateSharedEvent(
        string coreFact, int year, string? location, string outcome,
        IEnumerable<StoryParticipantSpec> participants);

    /// <summary>Whether an NPC may truthfully report this event (§11.3: cannot report what they don't know).</summary>
    bool CanReport(StoryEvent evt, string npcId);

    /// <summary>Participants who currently know the event happened.</summary>
    IReadOnlyList<StoryPerspective> KnownBy(StoryEvent evt);
}

public class StoryEventService : IStoryEventService
{
    public StoryEvent CreateSharedEvent(
        string coreFact, int year, string? location, string outcome,
        IEnumerable<StoryParticipantSpec> participants)
    {
        var evt = new StoryEvent
        {
            CoreFact = coreFact,
            Year = year,
            Location = location,
            Outcome = outcome,
        };

        foreach (var p in participants)
        {
            evt.Perspectives.Add(new StoryPerspective
            {
                NpcId = p.NpcId,
                Role = p.Role,
                // An NPC who doesn't know the event holds no belief about it (§11.3).
                Belief = p.Knows ? p.Belief : string.Empty,
                Certainty = p.Knows ? p.Certainty : EventConfidence.Unknown,
                Knows = p.Knows,
                IsSecretHolder = p.IsSecretHolder,
            });
        }

        return evt;
    }

    public bool CanReport(StoryEvent evt, string npcId)
        => evt.Perspectives.Any(p => p.NpcId == npcId && p.Knows);

    public IReadOnlyList<StoryPerspective> KnownBy(StoryEvent evt)
        => evt.Perspectives.Where(p => p.Knows).ToList();
}
