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
    public string ScenarioType { get; set; } = "Standard";
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

// ─── NPC (v3) ────────────────────────────────────────────
public enum NpcTier { Commoner = 1, Servant = 2, Adventurer = 3, Noble = 4, Royalty = 5 }

public enum NpcPersonality { Loyal, Ambitious, Pious, Corrupt, Fearful, Idealistic }

public enum NpcState { Alive, Dead, Exiled, Champion }

public enum RelationshipType { Ally, Rival, Spouse, Parent, Child, Liege, Vassal }

public enum ChampionPath { Saint, FallenDemonLord }

public class NpcRelationship
{
    public string NpcId { get; set; } = string.Empty;
    public RelationshipType Type { get; set; }
    public float Strength { get; set; } = 50f; // 0-100
}

public class NpcDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string CivilizationId { get; set; } = string.Empty;
    public string? OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Social position
    public NpcTier Tier { get; set; } = NpcTier.Commoner;
    public NpcPersonality Personality { get; set; }
    public NpcState State { get; set; } = NpcState.Alive;

    // Stats
    public float Loyalty { get; set; } = 50f;   // to liege / org
    public float Ambition { get; set; } = 30f;  // drives betrayal
    public float Piety { get; set; } = 40f;     // religious devotion
    public float Wealth { get; set; } = 30f;

    // Religion
    public string? PersonalReligionId { get; set; }
    public float DevotionLevel { get; set; } = 0.3f;

    // God interaction
    public string? GodInfluenceId { get; set; }
    public float GodTrustLevel { get; set; } = 0f;
    public int DreamsReceived { get; set; }
    public bool IsChampion { get; set; }
    public ChampionPath? ChampionPath { get; set; }
    public int EvolutionPoints { get; set; } // for champion progression

    // Relationships (Tier 3-5)
    public List<NpcRelationship> Relationships { get; set; } = new();
    public string? SpouseId { get; set; }
    public List<string> ChildrenIds { get; set; } = new();

    // Secrets (for blackmail)
    public string? KnownSecretAboutNpcId { get; set; }
    public string? SecretType { get; set; } // "corruption", "heresy", "affair"

    // Lifecycle
    public int AgeInTicks { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ─── Organization (v3) ───────────────────────────────────
public enum OrganizationType
{
    Kingdom, RoyalCourt, NobleHouse, AdventureGuild, ReligiousInstitution, UndergroundOrg
}

public enum OrgRole { Leader, Senior, Member, Initiate }

public class OrgMember
{
    public string NpcId { get; set; } = string.Empty;
    public OrgRole Role { get; set; }
    public string? RoleTitle { get; set; } // "Chancellor", "Spymaster" etc.
}

public class OrganizationDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string CivilizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public OrganizationType Type { get; set; }

    // Members
    public List<OrgMember> Members { get; set; } = new();
    public string? LeaderNpcId { get; set; }

    // Stats
    public float Power { get; set; } = 30f;       // political/military power
    public float Wealth { get; set; } = 50f;
    public float Loyalty { get; set; } = 60f;     // loyalty to kingdom
    public float ReligionDeviation { get; set; }  // how much they deviate from official religion

    // Allegiances
    public string? AllyOrgId { get; set; }
    public string? RivalOrgId { get; set; }
    public string? GodInfluenceId { get; set; }
    public string? OfficialReligionId { get; set; }

    // Underground only
    public bool IsHidden { get; set; }
    public float HeatLevel { get; set; } // 0-100, chance of exposure

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ─── NPC Events (v3) ─────────────────────────────────────
public enum NpcEventType
{
    // Crime
    Theft, CorruptionScandal, Assassination, HeresyTrial, Extortion, TaxEvasion,
    // Accidents
    CropFailure, DiseaseOutbreak, BuildingCollapse, TradeRobbery,
    // Social
    Marriage, Divorce, Birth, Death, Betrayal, Exile,
    // Political
    Election, Rebellion, Coronation, AllianceFormed, AllianceBroken,
    // Lucky/Unlucky
    TreasureFound, BattleMiracle, BusinessFailure, CrisisOfFaith
}

public class NpcEventDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string CivilizationId { get; set; } = string.Empty;
    public NpcEventType Type { get; set; }
    public List<string> InvolvedNpcIds { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public float FaithImpact { get; set; }
    public float EconomyImpact { get; set; }
    public float StabilityImpact { get; set; }
    public bool GodResponded { get; set; }
    public string? RespondingGodId { get; set; }
    public long Tick { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
