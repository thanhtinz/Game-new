using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Models;

// ═══ Dynasty / Bloodline domain models (Dynasty Spec §4, §12) ═══
// Genealogy is stored as separate documents so a family tree can be queried
// without loading every NPC. IDs are stored rather than nested objects.

/// <summary>Ordinary (non-divine) genetic stats, mixed separately from bloodlines.</summary>
public class NpcGeneProfile
{
    public float Height { get; set; } = 50f;
    public float Strength { get; set; } = 50f;
    public float Intelligence { get; set; } = 50f;
    public float ManaCapacity { get; set; } = 50f;
    public float Fertility { get; set; } = 50f;
    public float Longevity { get; set; } = 50f;
    public float DivineAffinity { get; set; } = 50f;
    public float CorruptionResistance { get; set; } = 50f;
    public float MutationChance { get; set; } = 5f;
}

/// <summary>A single mechanical effect a blessing grants (kept minimal/extensible).</summary>
public class BlessingEffect
{
    public string Stat { get; set; } = string.Empty;   // e.g. "Strength", "DivineAffinity"
    public float Amount { get; set; }
    public string? Description { get; set; }
}

/// <summary>Definition of a hereditary blessing at a bloodline's founding (Dynasty Spec §4).</summary>
public class BloodlineBlessingDefinition
{
    public string BlessingId { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Name { get; set; } = string.Empty;
    public string GodId { get; set; } = string.Empty;
    public GodDomain Domain { get; set; }

    /// <summary>Original strength at founding (e.g. 100 for a saint, 35 for a minor blessing).</summary>
    public float FoundingStrength { get; set; }

    /// <summary>Strength normally lost per generation.</summary>
    public float GenerationalDecayRate { get; set; } = 0.22f;

    /// <summary>Can survive many generations at low strength (dormant).</summary>
    public bool CanBecomeDormant { get; set; } = true;

    /// <summary>Stable divine lineage — decays much slower.</summary>
    public bool IsDivineLineage { get; set; }

    public List<BlessingEffect> Effects { get; set; } = new();
}

/// <summary>A blessing as it currently lives inside one NPC (Dynasty Spec §5).</summary>
public class InheritedBlessingInstance
{
    public string BlessingId { get; set; } = string.Empty;
    public string SourceBloodlineId { get; set; } = string.Empty;
    public string SourceGodId { get; set; } = string.Empty;

    /// <summary>0-100 current active strength in this NPC.</summary>
    public float Strength { get; set; }

    /// <summary>0-100 hidden potential — dormant descendants may awaken later.</summary>
    public float Potential { get; set; }

    public BlessingState State { get; set; }
    public int GenerationDistanceFromFounder { get; set; }
}

/// <summary>A lineage carrying blessings/curses across generations (Dynasty Spec §4).</summary>
public class BloodlineDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BloodlineKind Kind { get; set; }
    public string FounderNpcId { get; set; } = string.Empty;
    public string? FounderGodId { get; set; }
    public int FoundedYear { get; set; }

    /// <summary>0-100. How close the house remains to the original source.</summary>
    public float Purity { get; set; } = 100f;
    public float Stability { get; set; } = 50f;
    public float MutationPressure { get; set; }
    public float CorruptionPressure { get; set; }

    public List<BloodlineBlessingDefinition> Blessings { get; set; } = new();
    public List<string> ParentBloodlineIds { get; set; } = new();
    public List<string> ChildBloodlineIds { get; set; } = new();
}

/// <summary>A house / clan / dynasty (Dynasty Spec §4).</summary>
public class FamilyHouseDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FounderNpcId { get; set; } = string.Empty;
    public int FoundedYear { get; set; }
    public FamilyStatus Status { get; set; } = FamilyStatus.Active;
    public string? RealmId { get; set; }
    public NpcTier DominantClass { get; set; } = NpcTier.Commoner; // reuse existing social tier

    public float Honor { get; set; }
    public float Infamy { get; set; }
    public float Wealth { get; set; }
    public float DivineFavor { get; set; }
    public float PoliticalLegitimacy { get; set; }

    public List<string> LivingMemberIds { get; set; } = new();
    public List<string> HistoricalMemberIds { get; set; } = new();
    public List<string> BloodlineIds { get; set; } = new();
}

/// <summary>A parent→child edge for fast genealogy queries (Dynasty Spec §12).</summary>
public class GenealogyEdge
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string ParentNpcId { get; set; } = string.Empty;
    public string ChildNpcId { get; set; } = string.Empty;
    public string? FamilyId { get; set; }
    public int BirthYear { get; set; }
    public ParentType ParentType { get; set; }
}

/// <summary>A historical timeline entry for a family/bloodline (Dynasty Spec §12).</summary>
public class DynastyHistoryEvent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? NpcId { get; set; }
    public string? FamilyId { get; set; }
    public string? BloodlineId { get; set; }
    public DynastyEventType EventType { get; set; }
    public string Summary { get; set; } = string.Empty;
}

// ─── Population-scale genealogy (Dynasty Spec §9, §10, §12) ────
// Race lifespan sets the pace of bloodline evolution: short-lived races diversify
// fast, long-lived races preserve purity and memory.
public class RaceAgeProfile
{
    public RaceType Race { get; set; }
    public int AverageLifespanYears { get; set; }
    public int AdultAgeYears { get; set; }
    public int TypicalParenthoodStart { get; set; }
    public int TypicalGenerationYears { get; set; }
    public float FertilityModifier { get; set; } = 1f;
    public float BloodlineDecayModifier { get; set; } = 1f;
    public float MutationModifier { get; set; } = 1f;
}

// Aggregated commoner lineage — no individual tree unless a member is promoted.
public class PopulationFamilyGroup
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string RegionId { get; set; } = string.Empty;
    public RaceType Race { get; set; }
    public string? FaithId { get; set; }
    public int Count { get; set; }
    public float BirthRatePerGeneration { get; set; } = 0.28f;
    public float DeathRatePerGeneration { get; set; } = 0.20f;
}
