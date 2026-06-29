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
