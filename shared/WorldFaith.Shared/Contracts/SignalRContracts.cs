using WorldFaith.Shared.Enums;

namespace WorldFaith.Shared.Contracts;

// ══════════════════════════════════════════════
//  CLIENT → SERVER  (Requests)
// ══════════════════════════════════════════════

public class JoinWorldRequest
{
    public string WorldId { get; set; } = string.Empty;
    public string GodName { get; set; } = string.Empty;
    public GodArchetype Archetype { get; set; }
}

public class PerformMiracleRequest
{
    public MiracleType Miracle { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public string? TargetCivilizationId { get; set; }
    public string? TargetEntityId { get; set; }
}

public class SendCommunicationRequest
{
    public CommunicationType Type { get; set; }
    public string TargetCivilizationId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class CounterMiracleRequest
{
    public string MiracleEventId { get; set; } = string.Empty;
    public MiracleType CounterMiracle { get; set; }
}

public class EvolveEntityRequest
{
    public string EntityId { get; set; } = string.Empty;
}

public class CreateWorldRequest
{
    public string WorldName { get; set; } = string.Empty;
    public GameMode Mode { get; set; }
    public int MaxGods { get; set; } = 4;
    public int WorldWidth { get; set; } = 128;
    public int WorldHeight { get; set; } = 128;
    public int Seed { get; set; }   // 0 = random; nonzero reproduces identical terrain
    public VictoryCondition VictoryCondition { get; set; }
}

// ══════════════════════════════════════════════
//  SERVER → CLIENT  (Events)
// ══════════════════════════════════════════════

public class WorldTickEvent
{
    public long Tick { get; set; }
    public int Cycle { get; set; }
    public List<DeltaEvent> Deltas { get; set; } = new();
}

public class DeltaEvent
{
    public WorldEventType Type { get; set; }
    public string? SourceGodId { get; set; }
    public string? TargetId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public string Description { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}

public class MiracleResultEvent
{
    public string MiracleEventId { get; set; } = string.Empty;
    public MiracleType Miracle { get; set; }
    public string GodId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool WasCountered { get; set; }
    public string? CounteredByGodId { get; set; }
    public float FaithCost { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class GodUpdateEvent
{
    public string GodId { get; set; } = string.Empty;
    public float Faith { get; set; }
    public float Trust { get; set; }
    public float Fear { get; set; }
    public int FollowerCount { get; set; }
    public bool IsAlive { get; set; }
}

public class CivilizationUpdateEvent
{
    public string CivilizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Population { get; set; }
    public CivilizationState State { get; set; }
    public string? NewRulingReligionId { get; set; }
    public bool Collapsed { get; set; }
}

public class ReligionUpdateEvent
{
    public string ReligionId { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public ReligionEvent Event { get; set; }
    public int FollowerCount { get; set; }
    public bool Erased { get; set; }
}

public class WorldRebirthEvent
{
    public int NewCycle { get; set; }
    public List<string> SurvivedGodIds { get; set; } = new();
    public List<string> FadedGodIds { get; set; } = new();
}

public class GameEndEvent
{
    public VictoryCondition Condition { get; set; }
    public string? WinnerGodId { get; set; }
    public string? WinnerReligionId { get; set; }
    public Dictionary<string, int> FinalRankings { get; set; } = new();
}

public class ErrorEvent
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
