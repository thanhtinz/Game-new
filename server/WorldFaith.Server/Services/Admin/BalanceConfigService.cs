using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace WorldFaith.Server.Services.Admin;

// ─── Balance Config Document ─────────────────────────────
public class BalanceConfigDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DataType { get; set; } = "float"; // float, int, bool, string
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
}

// ─── Default Values ──────────────────────────────────────
public static class BalanceDefaults
{
    public static readonly Dictionary<string, (string value, string category, string desc, string type)> All = new()
    {
        // Faith
        ["faith.tick_interval_ms"]        = ("500",   "faith",     "Milliseconds per simulation tick",              "int"),
        ["faith.follower_gen_rate"]        = ("0.01",  "faith",     "Faith per follower per tick",                   "float"),
        ["faith.temple_gen_rate"]          = ("0.5",   "faith",     "Faith per temple per tick",                     "float"),
        ["faith.max_faith"]                = ("1000",  "faith",     "Max Faith a god can hold",                      "float"),
        ["faith.fear_dark_bonus"]          = ("0.02",  "faith",     "Extra faith per Fear for dark god archetypes",  "float"),

        // Miracles - Tier 1
        ["miracle.cost.rain"]              = ("10",    "miracle",   "Faith cost: Rain",                              "float"),
        ["miracle.cost.dream"]             = ("5",     "miracle",   "Faith cost: Dream",                             "float"),
        ["miracle.cost.bless_harvest"]     = ("15",    "miracle",   "Faith cost: BlessHarvest",                      "float"),
        ["miracle.cost.heal_follower"]     = ("8",     "miracle",   "Faith cost: HealFollower",                      "float"),
        ["miracle.cost.omen"]              = ("3",     "miracle",   "Faith cost: Omen",                              "float"),
        // Tier 2
        ["miracle.cost.storm"]             = ("30",    "miracle",   "Faith cost: Storm",                             "float"),
        ["miracle.cost.earthquake"]        = ("40",    "miracle",   "Faith cost: Earthquake",                        "float"),
        ["miracle.cost.curse"]             = ("25",    "miracle",   "Faith cost: Curse",                             "float"),
        ["miracle.cost.portal"]            = ("50",    "miracle",   "Faith cost: Portal",                            "float"),
        ["miracle.cost.divine_voice"]      = ("20",    "miracle",   "Faith cost: DivineVoice",                       "float"),
        // Tier 3
        ["miracle.cost.volcano"]           = ("100",   "miracle",   "Faith cost: Volcano",                           "float"),
        ["miracle.cost.demon_invasion"]    = ("120",   "miracle",   "Faith cost: DemonInvasion",                     "float"),
        ["miracle.cost.divine_beast"]      = ("80",    "miracle",   "Faith cost: DivineBeastCreation",               "float"),
        ["miracle.cost.revelation"]        = ("60",    "miracle",   "Faith cost: Revelation",                        "float"),
        ["miracle.cost.holy_war"]          = ("150",   "miracle",   "Faith cost: HolyWar",                           "float"),
        ["miracle.counter_window_sec"]     = ("5",     "miracle",   "Seconds rival can counter a miracle",           "int"),

        // Religion
        ["religion.spread_base_chance"]    = ("0.3",   "religion",  "Base spread chance per 5 ticks",                "float"),
        ["religion.devotion_decay"]        = ("0.001", "religion",  "Devotion decay per tick (no temple)",           "float"),
        ["religion.temple_devotion_bonus"] = ("0.002", "religion",  "Devotion gain per temple per tick",             "float"),
        ["religion.schism_threshold"]      = ("0.35",  "religion",  "Devotion below this can trigger schism",        "float"),
        ["religion.schism_min_followers"]  = ("500",   "religion",  "Min followers to trigger schism",               "int"),
        ["religion.schism_interval_ticks"] = ("50",    "religion",  "Ticks between schism checks",                   "int"),
        ["religion.heresy_chance"]         = ("0.08",  "religion",  "Heresy trigger chance per 80 ticks",            "float"),
        ["religion.crusade_min_devotion"]  = ("0.7",   "religion",  "Min devotion to start crusade",                 "float"),
        ["religion.crusade_min_military"]  = ("60",    "religion",  "Min military for crusade civ",                  "float"),

        // Evolution
        ["evolution.wild_to_divine_pts"]   = ("100",   "evolution", "Points to evolve WildAnimal→DivineBeast",       "int"),
        ["evolution.divine_to_celestial"]  = ("500",   "evolution", "Points to evolve DivineBeast→CelestialGuardian","int"),
        ["evolution.hero_to_saint_pts"]    = ("150",   "evolution", "Points to evolve HumanHero→Saint",              "int"),
        ["evolution.saint_to_demon_pts"]   = ("600",   "evolution", "Points to evolve Saint→FallenDemonLord",        "int"),
        ["evolution.monster_to_titan"]     = ("120",   "evolution", "Points to evolve Monster→Titan",                "int"),
        ["evolution.titan_to_apocalyptic"] = ("450",   "evolution", "Points to evolve Titan→ApocalypticEntity",      "int"),
        ["evolution.force_evolve_cost"]    = ("50",    "evolution", "Faith cost for god force-evolve",                "float"),
        ["evolution.sacred_tile_bonus"]    = ("1.5",   "evolution", "Multiplier for evolution points on Sacred tile", "float"),

        // Civilization AI
        ["civ.pop_growth_tribal"]          = ("0.001", "civ",       "Population growth rate: Tribal",                "float"),
        ["civ.pop_growth_kingdom"]         = ("0.003", "civ",       "Population growth rate: Kingdom",               "float"),
        ["civ.pop_growth_empire"]          = ("0.005", "civ",       "Population growth rate: Empire",                "float"),
        ["civ.pop_decay_collapsing"]       = ("0.01",  "civ",       "Population decay rate: Collapsing",             "float"),
        ["civ.kingdom_min_pop"]            = ("500",   "civ",       "Min population to reach Kingdom",               "int"),
        ["civ.empire_min_pop"]             = ("5000",  "civ",       "Min population to reach Empire",                "int"),

        // World
        ["world.rebirth_tick_interval"]    = ("1000",  "world",     "Ticks per rebirth cycle",                       "int"),
        ["world.initial_civ_count"]        = ("6",     "world",     "Civilizations spawned per world",               "int"),
        ["world.initial_entity_land"]      = ("12",    "world",     "WildAnimals spawned on land tiles",             "int"),
        ["world.initial_entity_harsh"]     = ("6",     "world",     "Monsters spawned on harsh tiles",               "int"),
        ["world.initial_entity_sacred"]    = ("4",     "world",     "HumanHeroes spawned on sacred tiles",           "int"),

        // NPC v3
        ["npc.noble_house_count_min"]      = ("3",     "npc",       "Min Noble Houses per Kingdom",                  "int"),
        ["npc.noble_house_count_max"]      = ("7",     "npc",       "Max Noble Houses per Kingdom",                  "int"),
        ["npc.champion_trust_required"]    = ("70",    "npc",       "Trust level needed for Adventurer → Champion",  "float"),
        ["npc.betrayal_ambition_threshold"]= ("75",    "npc",       "Ambition threshold for Noble betrayal risk",    "float"),
        ["npc.betrayal_loyalty_threshold"] = ("40",    "npc",       "Loyalty below which betrayal may occur",        "float"),
        ["npc.crime_chance_theft"]         = ("0.08",  "npc",       "Chance of theft when economy < 20",             "float"),
        ["npc.crime_chance_corruption"]    = ("0.05",  "npc",       "Chance of Noble corruption scandal",            "float"),
        ["npc.marriage_chance"]            = ("0.15",  "npc",       "Chance of Noble marriage per social tick",      "float"),
        ["npc.disease_chance"]             = ("0.03",  "npc",       "Chance of disease outbreak per 50 ticks",       "float"),
        ["npc.lucky_threshold"]            = ("88",    "npc",       "Luck roll >= this = lucky event",               "float"),
        ["npc.unlucky_threshold"]          = ("12",    "npc",       "Luck roll <= this = unlucky event",             "float"),

        // Organization v3
        ["org.guild_quest_chance"]         = ("0.08",  "org",       "Chance of Adventure Guild quest per tick",      "float"),
        ["org.guild_survival_chance"]      = ("0.7",   "org",       "Adventurer survival rate in monster quest",     "float"),
        ["org.underground_heat_gain"]      = ("0.5",   "org",       "Heat gained per tick for underground orgs",     "float"),
        ["org.court_deadlock_economy_dmg"] = ("3",     "org",       "Economy/Military lost per tick in court deadlock","float"),
        ["org.religious_corruption_chance"]= ("0.04",  "org",       "Chance High Priest corruption per tick",        "float"),
    };
}

// ─── Balance Config Service ──────────────────────────────
public interface IBalanceConfigService
{
    Task<float> GetFloatAsync(string key);
    Task<int> GetIntAsync(string key);
    Task<bool> GetBoolAsync(string key);
    Task<string> GetStringAsync(string key);
    Task SetAsync(string key, string value, string updatedBy = "admin");
    Task<List<BalanceConfigDocument>> GetAllAsync(string? category = null);
    Task SeedDefaultsAsync();
    void InvalidateCache(string key);
}

public class BalanceConfigService : IBalanceConfigService
{
    private readonly IMongoCollection<BalanceConfigDocument> _collection;
    private readonly Dictionary<string, (string value, DateTime cachedAt)> _cache = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const int CacheTtlSeconds = 60;

    public BalanceConfigService(IMongoDatabase db)
    {
        _collection = db.GetCollection<BalanceConfigDocument>("balance_config");
        _collection.Indexes.CreateOneAsync(
            new CreateIndexModel<BalanceConfigDocument>(
                Builders<BalanceConfigDocument>.IndexKeys.Ascending(c => c.Key),
                new CreateIndexOptions { Unique = true }));
    }

    public async Task<float> GetFloatAsync(string key)
    {
        var val = await GetRawAsync(key);
        return float.TryParse(val, out var f) ? f
            : BalanceDefaults.All.TryGetValue(key, out var d) && float.TryParse(d.value, out var df) ? df : 0f;
    }

    public async Task<int> GetIntAsync(string key)
    {
        var val = await GetRawAsync(key);
        return int.TryParse(val, out var i) ? i
            : BalanceDefaults.All.TryGetValue(key, out var d) && int.TryParse(d.value, out var di) ? di : 0;
    }

    public async Task<bool> GetBoolAsync(string key)
    {
        var val = await GetRawAsync(key);
        return bool.TryParse(val, out var b) ? b : false;
    }

    public async Task<string> GetStringAsync(string key)
        => await GetRawAsync(key);

    public async Task SetAsync(string key, string value, string updatedBy = "admin")
    {
        var filter = Builders<BalanceConfigDocument>.Filter.Eq(c => c.Key, key);
        var update = Builders<BalanceConfigDocument>.Update
            .Set(c => c.Value, value)
            .Set(c => c.UpdatedAt, DateTime.UtcNow)
            .Set(c => c.UpdatedBy, updatedBy);

        await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        InvalidateCache(key);
    }

    public async Task<List<BalanceConfigDocument>> GetAllAsync(string? category = null)
    {
        var filter = category != null
            ? Builders<BalanceConfigDocument>.Filter.Eq(c => c.Category, category)
            : Builders<BalanceConfigDocument>.Filter.Empty;
        return await _collection.Find(filter).SortBy(c => c.Category).ThenBy(c => c.Key).ToListAsync();
    }

    public async Task SeedDefaultsAsync()
    {
        foreach (var (key, (value, category, desc, type)) in BalanceDefaults.All)
        {
            var existing = await _collection.Find(c => c.Key == key).FirstOrDefaultAsync();
            if (existing == null)
            {
                await _collection.InsertOneAsync(new BalanceConfigDocument
                {
                    Key = key, Value = value,
                    Category = category, Description = desc, DataType = type
                });
            }
        }
    }

    public void InvalidateCache(string key)
    {
        _lock.Wait();
        try { _cache.Remove(key); }
        finally { _lock.Release(); }
    }

    private async Task<string> GetRawAsync(string key)
    {
        await _lock.WaitAsync();
        try
        {
            if (_cache.TryGetValue(key, out var cached)
                && (DateTime.UtcNow - cached.cachedAt).TotalSeconds < CacheTtlSeconds)
                return cached.value;
        }
        finally { _lock.Release(); }

        var doc = await _collection.Find(c => c.Key == key).FirstOrDefaultAsync();
        var val = doc?.Value
            ?? (BalanceDefaults.All.TryGetValue(key, out var d) ? d.value : string.Empty);

        await _lock.WaitAsync();
        try { _cache[key] = (val, DateTime.UtcNow); }
        finally { _lock.Release(); }

        return val;
    }
}
