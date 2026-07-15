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
    Sacred,
    Beach,    // Coastline transition tile between land and Water
    River,    // Freshwater tile carved by the river generator
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

// ─── Add-On v1.1: NPC Achievement & Divine Recognition ───

public enum AchievementCategory
{
    CommonLife, ReligiousDevot, Adventurer, RoyalService, MiracleExposure, DarkForbidden
}

public enum AchievementRarity
{
    Common = 1, Uncommon = 2, Rare = 3, Epic = 4, Legendary = 5
}

public enum NpcTalentGroup
{
    Spiritual, Combat, Mental, Social, DarkForbidden, Resistance
}

public enum ChurchRank
{
    Believer,           // normal follower
    DevoutBeliever,     // serious follower
    TempleHelper,       // shrine assistant
    Priest,             // official religious worker
    HighPriest,         // major temple leader
    Prophet,            // receives divine dreams
    Saint,              // chosen holy figure
    DivineAvatar,       // rare mortal vessel
    // Dark equivalents
    SecretCultist,
    ForbiddenShrineKeeper,
    DarkPriest,
    HereticProphet,
    BloodSaint,
    DemonVessel,
}

public enum NpcMotivation
{
    ServeGod, ProtectFamily, BecomeFamous, GainPower, SeekKnowledge, Revenge, Survive
}

public enum GodNoteTab
{
    TopFaithful, RisingTalents, PotentialPriests,
    SaintCandidates, ProphetCandidates, Champions,
    DangerousFollowers, HiddenCultAssets
}

public enum DivineAction
{
    Bless, SendDream, Test, Promote, MarkAsChosen, Protect, Ignore, Punish, Corrupt
}

// ─── Add-On v1.2: Doctrine Integrity & Escort System ─────

public enum DoctrineIntegrityStatus
{
    Exalted     = 4,  // 90-100: +20% to +40% divine power
    Faithful    = 3,  // 70-89:  normal or slight bonus
    Shaken      = 2,  // 50-69:  -10% to -25% divine power
    Compromised = 1,  // 25-49:  -30% to -60% divine power
    Broken      = 0,  // 0-24:   most power lost or converted
}

public enum ViolationSeverity
{
    MinorContradiction, // -2 to -5
    ModerateViolation,  // -8 to -15
    MajorViolation,     // -20 to -35
    SevereBetrayal,       // -40 to -70
    DoctrineInversion,  // -80 to -100 → triggers fall
}

public enum GodNoteWarningTag
{
    PureCandidate,      // NPC strongly matches doctrine
    ShakenFaith,        // integrity dropping
    Tempted,            // near a violation event
    Compromised,        // already violated, power weakening
    AtRiskOfFall,       // may fall / corrupt / convert
    ProtectedAsset,     // important enough to need escorts
}

public enum EscortRole
{
    GuardKnight,        // frontline protection
    Healer,             // sustain VIP health/sanity
    Disciple,           // spread stories, witness miracles
    Scribe,             // document, diplomacy
    Fanatic,            // self-sacrifice willing
    PilgrimFollower,    // crowds that form around prophets
    CorruptedGuard,     // secretly serves rival god
    CultistAgent,       // planted by dark org
}

public enum EscortBehavior
{
    Follow,     // stay close during travel
    Guard,      // defensive radius when threat appears
    Evacuate,   // move VIP to safe location
    Intercept,  // attack enemy before reaching VIP
    Witness,    // spread stories of the VIP
    Sacrifice,  // die to protect saint/prophet
    Betray,     // secretly serve rival god
}

public enum DoctrineTag
{
    Purity, War, Knowledge, Nature, Darkness, Order,
    Chaos, Death, Light, Balance
}

// NPC personality traits (NPC Master Spec §4). Traits can override race
// expectations and create rare story exceptions in belief decisions.
public enum NpcTrait
{
    Genius, Fanatic, Compassionate, Ambitious, Traumatized,
    Curious, Traditional, Reckless, Cowardly, Honorable, Cruel, Merciful
}

// What a named NPC remembers about a god's actions (NPC Master Spec §8).
// Distinct from MemoryType (relic / faith-economy memory) above.
public enum NpcMemoryType
{
    MiracleSavedLife, MiracleFailed, FamineEnded, ForestBurned,
    MonsterAttackStopped, NobleScandalExposed, SacredSiteDestroyed,
    SaintFell, ProphetMartyred, WarVictory, WarDefeat
}

// Hidden facts an NPC holds — used for blackmail, cults, court intrigue (Spec §4).
public enum NpcSecretType
{
    HiddenFaith, ForbiddenGodWorship, Corruption, Heresy, Affair,
    Treason, ConcealedIdentity, CultMembership
}

// ═══ Dynasty / Bloodline System (Dynasty Spec) ═══════════════
public enum SexType { Male, Female, Unknown }

// Bloodline flavor domain (Dynasty Spec §4). Superset of GodArchetype with
// lineage-specific flavors (Moon, Fire) used by hereditary blessings.
public enum GodDomain { Light, Darkness, Nature, War, Knowledge, Order, Chaos, Death, Moon, Fire }

public enum BloodlineKind
{
    MortalFamily, BlessedLineage, DivineLineage, CursedLineage, HybridLineage, CorruptedLineage
}

// Current state of an inherited blessing inside one NPC (Dynasty Spec §5).
public enum BlessingState { Active, Dormant, Faded, Mutated, Corrupted, Sealed }

public enum FamilyStatus { Active, Declining, Extinct, Hidden, Exiled, Revived }

public enum ParentType { Biological, Adoptive, Unknown, DivineCreated }

public enum DynastyEventType
{
    Founded, Blessed, Cursed, Marriage, Birth, Death, Awakening,
    HybridMutation, Exile, Extinction, Revival, SuccessionDispute
}

// ═══ Gameplay Foundation (Gameplay Spec) ═════════════════════
// Settlement growth stages (§4.1). "Kingdom" is a political organization, not a
// settlement stage — see KingdomFormation (§4.3).
public enum SettlementStage { Camp, Hamlet, Village, Town, City }

// Prayer/request priority (Gameplay Spec §7.3).
public enum RequestPriority { Critical, Important, Routine, Resolved }

// Faith relationship tier that gates personal-gift permission (§9.3).
public enum FaithRelationshipTier { Unknown, Curious, Believer, Devoted, Consecrated, Champion }

// Strength of a rival god's aura trace on an NPC (§10.3).
public enum AuraTraceStrength { None, Faint, Recognizable, Identified, Revealed }

// A participant's role within a shared story event (§11.2).
public enum StoryRole { Actor, Target, Witness, Helper, Victim, Beneficiary, Leader, Messenger }

// Confidence layer for observed world events (§10.2).
public enum EventConfidence { Unknown, Faint, Likely, Strong, Confirmed }
