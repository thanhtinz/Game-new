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
    // v1.0 GDD
    public WorldAge CurrentAge { get; set; } = WorldAge.EarlyAge;
    public List<string> ForbiddenGodIds { get; set; } = new(); // gods outlawed world-wide
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
    // v1.0 GDD additions
    public GodRankData RankData { get; set; } = new();
    public List<GodMemoryEntry> Memories { get; set; } = new();
    public List<string> RelicIds { get; set; } = new();          // relics linked to this god
    public List<string> ForbiddenInCivIds { get; set; } = new(); // civs that outlawed this god
    public bool IsForgotten { get; set; } = false;               // true = no followers, surviving via relics
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
    public RaceType PrimaryRace { get; set; } = RaceType.Human;
    public GovernmentType Government { get; set; } = GovernmentType.Monarchy;  // v1.0
    public int Population { get; set; } = 100;
    public float Economy { get; set; } = 50f;
    public float Military { get; set; } = 30f;
    public float Food { get; set; } = 50f;          // v1.0: famine system
    public float Stability { get; set; } = 60f;     // v1.0: rebellion trigger
    public float Corruption { get; set; } = 10f;    // v1.0: dark gods amplify
    public float ReligiousUnity { get; set; } = 50f;// v1.0: schism risk
    public float Happiness { get; set; } = 50f;     // v1.0: trust/conversion
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
    // v1.0 GDD: Doctrine Axes
    public DoctrineValues Doctrine { get; set; } = new();
    // Believer type breakdown
    public int CasualCount { get; set; }
    public int DevoutCount { get; set; }
    public int FanaticCount { get; set; }
    public int CultistCount { get; set; }
    public int HereticCount { get; set; }
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

    // Add-On v1.1: Divine Recognition
    public NpcDivineProfile DivineProfile { get; set; } = new();

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

// ─── Race Affinity System (GDD v1.0 Section 9) ───────────────
public class RaceAffinityEntry
{
    public GodArchetype Domain { get; set; }
    public float Percentage { get; set; }  // 10-170
}

public class RaceDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public RaceType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<RaceAffinityEntry> AffinityMatrix { get; set; } = new();
    public List<string> CulturalTaboos { get; set; } = new();     // God archetype names blocked
    public List<RaceTrait> PassiveTraits { get; set; } = new();
    public Dictionary<string, float> EnvironmentalMemory { get; set; } = new(); // godId → trust modifier
}

// ─── God Rank + Memory (GDD v1.0 Section 7 + 22) ────────────
public class GodMemoryEntry
{
    public MemoryType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? RelatedCivId { get; set; }
    public long Tick { get; set; }
    public float TrustImpact { get; set; }
}

// GodDocument: added Rank, Memory, RelicIds, ForbiddenIn
public class GodRankData
{
    public GodRank Rank { get; set; } = GodRank.Nascent;
    public int TotalFaithEarned { get; set; }
    public float RankMultiplier => Rank switch
    {
        GodRank.Forgotten   => 0.1f,
        GodRank.Nascent     => 1.0f,
        GodRank.Awakened    => 1.2f,
        GodRank.Established => 1.5f,
        GodRank.Revered     => 1.8f,
        GodRank.Exalted     => 2.2f,
        GodRank.Ancient     => 3.0f,
        _ => 1f
    };
}

// ─── Doctrine System (GDD v1.0 Section 13) ───────────────────
public class DoctrineValues
{
    // Each axis: -100 (low end) to +100 (high end)
    public float MercyVsPunishment { get; set; } = 0f;    // -100=mercy, +100=punishment
    public float IsolationVsExpansion { get; set; } = 0f; // -100=isolate, +100=expand
    public float HarmonyVsDominion { get; set; } = 0f;    // -100=harmony, +100=dominion
    public float FreedomVsOrder { get; set; } = 0f;       // -100=freedom, +100=order
    public float SacrificeVsProsperity { get; set; } = 0f;// -100=sacrifice, +100=prosperity
}

// ─── Dungeon System (GDD v1.0 Section 12, 16) ────────────────
public class DungeonDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public DungeonType Type { get; set; }
    public DungeonState State { get; set; } = DungeonState.Active;
    public int X { get; set; }
    public int Y { get; set; }
    public float DangerLevel { get; set; } = 30f;   // 0-100
    public float Reward { get; set; } = 50f;         // faith/relic potential
    public string? OriginGodId { get; set; }         // god who spawned it
    public string? ActiveMissionId { get; set; }
    public string? RelicId { get; set; }             // relic inside if any
    public long SpawnedAtTick { get; set; }
}

public class RelicDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public RelicType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OriginGodId { get; set; }
    public string? CurrentOwnerId { get; set; }     // NPC or civ holding it
    public string? LocationDungeonId { get; set; }
    public string? LocationCivId { get; set; }
    public float MemoryPower { get; set; } = 50f;   // how well it preserves god memory
    public float FaithBonus { get; set; }            // passive faith bonus to holder's god
    public bool IsActive { get; set; } = true;
    public long DiscoveredAtTick { get; set; }
    public List<GodMemoryEntry> Memories { get; set; } = new();
}

public class GuildMissionDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string WorldId { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string DungeonId { get; set; } = string.Empty;
    public GuildMissionState State { get; set; } = GuildMissionState.Active;
    public List<string> AdventurerIds { get; set; } = new();
    public string? QuestGiverId { get; set; }   // Noble/Civ who commissioned
    public long StartedAtTick { get; set; }
    public long? CompletedAtTick { get; set; }
    public string? DiscoveredRelicId { get; set; }
    public float FaithImpact { get; set; }
    public string OutcomeDescription { get; set; } = string.Empty;
}

// ─── Add-On v1.1: NPC Achievement & Divine Recognition ───

public class NpcAchievement
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AchievementCategory Category { get; set; }
    public AchievementRarity Rarity { get; set; }
    public int GodNoteWeight { get; set; }     // +1 to +150
    public long EarnedAtTick { get; set; }
}

public class NpcTalent
{
    public string Name { get; set; } = string.Empty;
    public NpcTalentGroup Group { get; set; }
    public int RarityScore { get; set; }        // 1-100, used in DivineAttentionScore
    public bool IsAwakened { get; set; }        // talent may be hidden until awakened
    public string? AwakenedByEvent { get; set; }
}

// Full v1.1 profile for NPC tier 2-5
// (Tier 1 commoners use aggregate, no individual profile)
public class NpcDivineProfile
{
    // Achievement & Talent
    public List<NpcAchievement> Achievements { get; set; } = new();
    public List<NpcTalent> Talents { get; set; } = new();

    // Divine Attention Score (GDD §5)
    // = FaithLevel + AchievementValue + TalentRarity + Reputation + MiracleExposure + DestinyModifier - CorruptionRisk
    public float FaithLevel { get; set; }
    public float AchievementValue { get; set; }   // sum of GodNoteWeight
    public float TalentRarity { get; set; }
    public float Reputation { get; set; }
    public float MiracleExposure { get; set; }
    public float DestinyModifier { get; set; }    // hidden seed, revealed over time
    public float CorruptionRisk { get; set; }
    public float DivineAttentionScore =>
        FaithLevel + AchievementValue + TalentRarity + Reputation
        + MiracleExposure + DestinyModifier - CorruptionRisk;

    // Church progression
    public ChurchRank ChurchRank { get; set; } = ChurchRank.Believer;
    public string? AssignedReligionId { get; set; }
    public long ChurchRankEarnedAt { get; set; }

    // Motivation
    public NpcMotivation PrimaryMotivation { get; set; }
    public NpcMotivation? SecondaryMotivation { get; set; }

    // Destiny
    public bool IsSaintCandidate { get; set; }
    public bool IsProphetCandidate { get; set; }
    public bool IsChampionCandidate { get; set; }
    public bool IsDarkPathCandidate { get; set; }

    // Divine actions history
    public List<string> ReceivedDivineActions { get; set; } = new(); // DivineAction names
    public string? ChosenByGodId { get; set; }   // "Mark as Chosen" target

    // Add-On v1.2: Doctrine Integrity
    public DoctrineIntegrityRecord DoctrineIntegrity { get; set; } = new();
    public EscortGroup? AssignedEscort { get; set; }
    public List<GodNoteWarningTag> ActiveWarnings { get; set; } = new();
}

// God Note entry (returned to players)
public class GodNoteEntry
{
    public string NpcId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RaceType Race { get; set; }
    public NpcTier SocialClass { get; set; }
    public string? ReligionName { get; set; }
    public float FaithPercent { get; set; }          // 0-100
    public List<string> TalentNames { get; set; } = new();
    public List<string> AchievementNames { get; set; } = new();
    public string Potential { get; set; } = string.Empty;  // "Saintess Candidate" v.v.
    public string Risk { get; set; } = string.Empty;
    public List<string> RecommendedActions { get; set; } = new();
    public float DivineAttentionScore { get; set; }
    public GodNoteTab Tab { get; set; }
}

// ─── Add-On v1.2: Doctrine Integrity & Escort System ─────

public class DoctrineIntegrityRecord
{
    public float Score { get; set; } = 80f;   // 0-100
    public DoctrineIntegrityStatus Status =>
        Score >= 90 ? DoctrineIntegrityStatus.Exalted :
        Score >= 70 ? DoctrineIntegrityStatus.Faithful :
        Score >= 50 ? DoctrineIntegrityStatus.Shaken :
        Score >= 25 ? DoctrineIntegrityStatus.Compromised :
                      DoctrineIntegrityStatus.Broken;

    public float PowerModifier => Status switch
    {
        DoctrineIntegrityStatus.Exalted     => 1.30f,  // mid of 1.20-1.40
        DoctrineIntegrityStatus.Faithful    => 1.05f,
        DoctrineIntegrityStatus.Shaken      => 0.825f, // mid of 0.75-0.90
        DoctrineIntegrityStatus.Compromised => 0.55f,  // mid of 0.40-0.70
        DoctrineIntegrityStatus.Broken      => 0.15f,  // mid of 0.00-0.30
        _ => 1f
    };

    public List<string> ViolationHistory { get; set; } = new(); // event descriptions
    public List<GodNoteWarningTag> ActiveWarnings { get; set; } = new();
    public bool IsExcommunicated { get; set; }
    public bool IsCoverUpActive { get; set; }
    public long LastViolationTick { get; set; }
    public float RedemptionProgress { get; set; }   // 0-100, pilgrimage/trial progress
}

public class EscortMember
{
    public string NpcId { get; set; } = string.Empty;
    public EscortRole Role { get; set; }
    public EscortBehavior CurrentBehavior { get; set; } = EscortBehavior.Follow;
    public float Loyalty { get; set; } = 70f;       // 0-100
    public bool IsCorrupted { get; set; }            // secretly serves rival god
    public string? RivalGodId { get; set; }          // if corrupted
}

public class EscortGroup
{
    public string ProtectedNpcId { get; set; } = string.Empty;
    public List<EscortMember> Members { get; set; } = new();
    public float GroupStrength { get; set; }         // sum of escort power
    public float DangerLevel { get; set; }           // current threat level
    public bool IsActive { get; set; } = true;
    public string? LastKnownLocationCivId { get; set; }
    public long FormedAtTick { get; set; }
}

public class ViolationEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string NpcId { get; set; } = string.Empty;
    public string WorldId { get; set; } = string.Empty;
    public ViolationSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public float IntegrityLoss { get; set; }
    public bool IsPublic { get; set; }              // scandal vs private
    public bool IsResisted { get; set; }            // NPC resisted temptation?
    public long Tick { get; set; }
    public string? TriggeredByGodId { get; set; }  // rival god who engineered event
}
