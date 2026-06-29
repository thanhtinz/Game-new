using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Server.Services.Race;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Faith;

public interface IFaithService
{
    Task<bool> CanPerformMiracleAsync(string godId, MiracleType miracle);
    Task<float> ConsumeFaithAsync(string godId, MiracleType miracle);
    Task<float> GenerateFaithTickAsync(string worldId);
    Task<float> GetFaithAsync(string godId);
    Task<float> GetMiracleCostAsync(MiracleType miracle);
}

public class FaithService : IFaithService
{
    private readonly IGodRepository _godRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IBalanceConfigService _balance;
    private readonly IRaceAffinityService _raceAffinity;
    private readonly ILogger<FaithService> _logger;

    // Mapping MiracleType → balance config key
    private static readonly Dictionary<MiracleType, string> MiracleKeys = new()
    {
        { MiracleType.Rain,               "miracle.cost.rain" },
        { MiracleType.Dream,              "miracle.cost.dream" },
        { MiracleType.BlessHarvest,       "miracle.cost.bless_harvest" },
        { MiracleType.HealFollower,       "miracle.cost.heal_follower" },
        { MiracleType.Omen,               "miracle.cost.omen" },
        { MiracleType.Storm,              "miracle.cost.storm" },
        { MiracleType.Earthquake,         "miracle.cost.earthquake" },
        { MiracleType.Curse,              "miracle.cost.curse" },
        { MiracleType.Portal,             "miracle.cost.portal" },
        { MiracleType.DivineVoice,        "miracle.cost.divine_voice" },
        { MiracleType.Volcano,            "miracle.cost.volcano" },
        { MiracleType.DemonInvasion,      "miracle.cost.demon_invasion" },
        { MiracleType.DivineBeastCreation,"miracle.cost.divine_beast" },
        { MiracleType.Revelation,         "miracle.cost.revelation" },
        { MiracleType.HolyWar,            "miracle.cost.holy_war" },
    };

    public FaithService(
        IGodRepository godRepo,
        ICivilizationRepository civRepo,
        IReligionRepository religionRepo,
        IBalanceConfigService balance,
        IRaceAffinityService raceAffinity,
        ILogger<FaithService> logger)
    {
        _godRepo = godRepo;
        _civRepo = civRepo;
        _religionRepo = religionRepo;
        _balance = balance;
        _raceAffinity = raceAffinity;
        _logger = logger;
    }

    public async Task<float> GetMiracleCostAsync(MiracleType miracle)
    {
        if (MiracleKeys.TryGetValue(miracle, out var key))
            return await _balance.GetFloatAsync(key);
        return 10f;
    }

    public async Task<float> GetFaithAsync(string godId)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        return god?.Faith ?? 0f;
    }

    public async Task<bool> CanPerformMiracleAsync(string godId, MiracleType miracle)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null || !god.IsAlive) return false;
        if (!god.UnlockedMiracles.Contains(miracle)) return false;
        var cost = await GetMiracleCostAsync(miracle);
        return god.Faith >= cost;
    }

    public async Task<float> ConsumeFaithAsync(string godId, MiracleType miracle)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null) return 0f;

        var baseCost  = await GetMiracleCostAsync(miracle);
        // Archetype discount
        float discount = ArchetypeBonus.GetMiracleCostMultiplier(god.Archetype, miracle);
        var cost = MathF.Max(0f, baseCost * discount);

        var newFaith = MathF.Max(0f, god.Faith - cost);
        await _godRepo.UpdateFaithAsync(godId, newFaith, god.Trust, god.Fear, god.FollowerCount);

        _logger.LogInformation("God {GodId}({Arch}) spent {Cost} faith on {Miracle}. Remaining: {Faith}",
            godId, god.Archetype, cost, miracle, newFaith);
        return cost;
    }

    public async Task<float> GenerateFaithTickAsync(string worldId)
    {
        var gods      = await _godRepo.GetByWorldAsync(worldId);
        var religions = await _religionRepo.GetByWorldAsync(worldId);

        float followerRate = await _balance.GetFloatAsync("faith.follower_gen_rate");
        float templeRate   = await _balance.GetFloatAsync("faith.temple_gen_rate");
        float maxFaith     = await _balance.GetFloatAsync("faith.max_faith");
        float fearBonus    = await _balance.GetFloatAsync("faith.fear_dark_bonus");

        float total = 0f;
        foreach (var god in gods)
        {
            if (!god.IsAlive) continue;

            var godReligions = religions.Where(r => r.GodId == god.Id).ToList();

            float fromFollowers = god.FollowerCount * followerRate;
            float fromTemples   = godReligions.Sum(r => r.TempleCount) * templeRate;
            float devotionBonus = godReligions.Sum(r => r.DevotionLevel * r.FollowerCount * 0.005f);
            float fearResource  = god.Archetype is GodArchetype.Darkness or GodArchetype.Death or GodArchetype.Chaos
                ? god.Fear * fearBonus : 0f;

            // Archetype multiplier
            float archetypeMult = ArchetypeBonus.GetFaithGenMultiplier(god, godReligions, new List<CivilizationDocument>());

            // Race Affinity modifier — aggregate across civs following this god
            float raceAffinityMult = 1f;
            var civIds = godReligions.SelectMany(r => r.CivilizationIds).Distinct().ToList();
            if (civIds.Any())
            {
                float totalAffinityMult = 0f;
                int civCount = 0;
                foreach (var civId in civIds.Take(10)) // sample max 10 civs để tránh quá chậm
                {
                    var civ = await _civRepo.GetByIdAsync(civId);
                    if (civ == null) continue;
                    float affMult = await _raceAffinity.GetFaithGainModifierAsync(worldId, civ.PrimaryRace, god.Archetype);
                    totalAffinityMult += affMult;
                    civCount++;
                }
                if (civCount > 0) raceAffinityMult = totalAffinityMult / civCount;
            }

            // God Rank multiplier
            float rankMult = god.RankData.RankMultiplier;

            float gain = (fromFollowers + fromTemples + devotionBonus + fearResource)
                * archetypeMult * raceAffinityMult * rankMult;
            float newFaith = MathF.Min(maxFaith, god.Faith + gain);

            await _godRepo.UpdateFaithAsync(god.Id, newFaith, god.Trust, god.Fear, god.FollowerCount);
            total += gain;
        }
        return total;
    }
}
