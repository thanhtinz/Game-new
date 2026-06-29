using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Models;

namespace WorldFaith.Server.Models;

// ─── World ──────────────────────────────────────────────
public class WorldDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Name { get; set; } = string.Empty;
    public GameMode Mode { get; set; }
    public VictoryCondition VictoryCondition { get; set; }
    public int MaxGods { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Cycle { get; set; }
    public long Tick { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<WorldTileData> Tiles { get; set; } = new();
}

public class WorldTileData
{
    public int X { get; set; }
    public int Y { get; set; }
    public TileType Type { get; set; }
    public float Fertility { get; set; }
    public string? CivilizationId { get; set; }
    public string? ReligionId { get; set; }
    public bool HasTemple { get; set; }
    public int Population { get; set; }
}

// ─── God ────────────────────────────────────────────────
public class GodDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public GodArchetype Archetype { get; set; }
    public float Faith { get; set; } = 100f;
    public float Trust { get; set; } = 50f;
    public float Fear { get; set; } = 0f;
    public int FollowerCount { get; set; }
    public List<MiracleType> UnlockedMiracles { get; set; } = new()
    {
        MiracleType.Dream,
        MiracleType.Rain,
        MiracleType.BlessHarvest
    };
    public bool IsAlive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActionAt { get; set; } = DateTime.UtcNow;
}

// ─── Civilization ────────────────────────────────────────
public class CivilizationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CivilizationPersonality Personality { get; set; }
    public int Population { get; set; } = 100;
    public float Economy { get; set; } = 50f;
    public float Military { get; set; } = 30f;
    public string? RulingReligionId { get; set; }
    public List<string> ReligionIds { get; set; } = new();
    public List<TileCoord> ControlledTiles { get; set; } = new();
    public bool IsAtWar { get; set; }
    public CivilizationState State { get; set; } = CivilizationState.Tribal;
    public CivilizationAiMemory AiMemory { get; set; } = new();
}

public class TileCoord
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class CivilizationAiMemory
{
    public string? LastGodInteraction { get; set; }
    public float GodTrustLevel { get; set; }
    public int TicksAtWar { get; set; }
    public string? CurrentTarget { get; set; }
}

// ─── Religion ────────────────────────────────────────────
public class ReligionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public int FollowerCount { get; set; }
    public int TempleCount { get; set; }
    public float DevotionLevel { get; set; } = 0.5f;
    public bool IsHidden { get; set; }
    public List<string> CivilizationIds { get; set; } = new();
    public List<string> SchismIds { get; set; } = new();
    public DateTime FoundedAt { get; set; } = DateTime.UtcNow;
}

// ─── Evolution Entity ────────────────────────────────────
public class EvolutionEntityDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public EvolutionStage Stage { get; set; }
    public string? GodInfluenceId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public float Power { get; set; } = 10f;
    public int EvolutionPoints { get; set; }
}

// ─── Miracle Event Log ───────────────────────────────────
public class MiracleEventDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public MiracleType Miracle { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool Success { get; set; }
    public bool WasCountered { get; set; }
    public string? CounteredByGodId { get; set; }
    public float FaithCost { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
