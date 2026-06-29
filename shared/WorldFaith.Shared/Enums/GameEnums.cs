namespace WorldFaith.Shared.Enums;

public enum GodArchetype
{
    Order,
    Chaos,
    Light,
    Darkness,
    Nature,
    Death,
    Knowledge,
    War
}

public enum MiracleType
{
    // Tier 1 - Low cost
    Rain,
    Dream,
    BlessHarvest,
    HealFollower,
    Omen,

    // Tier 2 - Medium cost
    Storm,
    Earthquake,
    Curse,
    Portal,
    DivineVoice,

    // Tier 3 - High cost
    Volcano,
    DemonInvasion,
    DivineBeastCreation,
    Revelation,
    HolyWar
}

public enum CivilizationPersonality
{
    Aggressive,
    Defensive,
    Fanatic,
    Logical,
    Opportunistic
}

public enum ReligionEvent
{
    Founded,
    TempleBuilt,
    Conversion,
    Schism,
    HeresyFormed,
    CultFormed,
    CrusadeStarted,
    ReligionErased
}

public enum EvolutionStage
{
    // Creature path
    WildAnimal,
    DivineBeast,
    CelestialGuardian,

    // Hero path
    HumanHero,
    Saint,
    FallenDemonLord,

    // Monster path
    Monster,
    Titan,
    ApocalypticEntity
}

public enum WorldEventType
{
    MiraclePerformed,
    MiracleCountered,
    CivilizationFounded,
    CivilizationCollapsed,
    ReligionEvent,
    EvolutionOccurred,
    GodFaded,
    WorldRebirth,
    HolyWar,
    DivineConflict
}

public enum GameMode
{
    Sandbox,
    Survival,
    Competitive,
    Scenario
}

public enum VictoryCondition
{
    LastSurvivingGod,
    HighestFaithAfterCycle,
    DominantReligionControl
}

public enum CommunicationType
{
    Dream,       // Low cost, personal
    DivineVoice, // Medium, limited commands
    Revelation   // Mass influence, world scale
}

public enum TileType
{
    Grassland,
    Forest,
    Mountain,
    Desert,
    Water,
    Tundra,
    Volcano,
    Sacred
}

// NPC v3
public enum NpcEventResult { Ignored, GodIntervened, NaturalResolution }
public enum CrimeType { Theft, Corruption, Assassination, Extortion, TaxEvasion, Heresy }
public enum PoliticalEventType { Election, Rebellion, Coronation, Alliance, Embargo }
public enum MarriageType { Political, Royal, Forbidden, Religious, Arranged }

// ─── Race System (v1.0 GDD) ──────────────────────────────
public enum RaceType
{
    Human, Elf, Dwarf, Orc, Beastfolk, Demon, Angel, Undead
}

public enum RaceTrait
{
    Genius, Fanatic, Compassionate, Ambitious, Traumatized, Curious, Traditional, Reckless
}

// Affinity tier từ GDD v1.0
public enum AffinityTier
{
    Taboo        = 0,   // 10-20%
    Rejected     = 1,   // 30-50%
    Difficult    = 2,   // 60-80%
    Neutral      = 3,   // 90-110%
    Preferred    = 4,   // 120-140%
    DeepHarmony  = 5,   // 150-170%
}

// ─── Dungeon System ───────────────────────────────────────
public enum DungeonType
{
    AncientRuins, MonstersLair, ForbiddenSanctum, LostTemple, DarkPortal
}

public enum DungeonState { Active, Cleared, Sealed, Infested, Awakening }

public enum RelicType
{
    FaithCrystal, AncientScripture, DivineShard, CursedArtifact,
    HeroicWeapon, ForgottenIdol, SacredBone, MythicGem
}

public enum GuildMissionState { Active, Success, Failed, Corrupted }

// ─── God Rank ─────────────────────────────────────────────
public enum GodRank
{
    Forgotten   = 0,
    Nascent     = 1,
    Awakened    = 2,
    Established = 3,
    Revered     = 4,
    Exalted     = 5,
    Ancient     = 6,
}

// ─── Religion Doctrine ───────────────────────────────────
public enum DoctrineAxis
{
    MercyVsPunishment,
    IsolationVsExpansion,
    HarmonyVsDominion,
    FreedomVsOrder,
    SacrificeVsProsperity,
}

// ─── Memory / Relic ──────────────────────────────────────
public enum MemoryType
{
    MiracleSuccess, MiracleFail, MonsterSlain, NobleConverted,
    SchismOccurred, HolyWarWon, DisasterSurvived, GodForgotten
}

// ─── Government Types (GDD v1.0 Section 11) ─────────────
public enum GovernmentType
{
    Monarchy,       // Fast policy; royal faith shifts kingdom
    Theocracy,      // High unity; priests dominate
    NobleCouncil,   // Regional stability; factional doctrine
    TribalClan,     // High loyalty; chief/shaman leads
    MerchantState,  // Wealth-driven; faith follows profit
    MonsterHorde,   // Military expansion; strength gods spread fast
}

// ─── Believer Types (GDD v1.0 Section 8) ─────────────────
public enum BelieverType { Casual, Devout, Fanatic, Cultist, Heretic }

// ─── Age System (GDD v1.0 Section 5) ─────────────────────
public enum WorldAge
{
    EarlyAge,       // First followers, small villages
    KingdomAge,     // Political expansion, institutions
    ConflictAge,    // Holy wars, monsters, champions
    CollapseAge,    // Empires fall, god survival crisis
    RebirthAge,     // New civs, relics carry memory
}
