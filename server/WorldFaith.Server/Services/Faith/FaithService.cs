using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Faith;

// Chi phí Faith cho từng miracle
public static class MiracleCosts
{
    private static readonly Dictionary<MiracleType, float> Costs = new()
    {
        // Tier 1
        { MiracleType.Rain,          10f },
        { MiracleType.Dream,          5f },
        { MiracleType.BlessHarvest,  15f },
        { MiracleType.HealFollower,   8f },
        { MiracleType.Omen,           3f },

        // Tier 2
        { MiracleType.Storm,         30f },
        { MiracleType.Earthquake,    40f },
        { MiracleType.Curse,         25f },
        { MiracleType.Portal,        50f },
        { MiracleType.DivineVoice,   20f },

        // Tier 3
        { MiracleType.Volcano,          100f },
        { MiracleType.DemonInvasion,    120f },
        { MiracleType.DivineBeastCreation, 80f },
        { MiracleType.Revelation,        60f },
        { MiracleType.HolyWar,          150f },
    };

    public static float Get(MiracleType type) =>
        Costs.TryGetValue(type, out var cost) ? cost : 10f;
}

public interface IFaithService
{
    Task<bool> CanPerformMiracleAsync(string godId, MiracleType miracle);
    Task<float> ConsumeFaithAsync(string godId, MiracleType miracle);
    Task<float> GenerateFaithTickAsync(string worldId);
    Task<float> GetFaithAsync(string godId);
}

public class FaithService : IFaithService
{
    private readonly IGodRepository _godRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly ILogger<FaithService> _logger;

    public FaithService(
        IGodRepository godRepo,
        ICivilizationRepository civRepo,
        IReligionRepository religionRepo,
        ILogger<FaithService> logger)
    {
        _godRepo = godRepo;
        _civRepo = civRepo;
        _religionRepo = religionRepo;
        _logger = logger;
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

        var cost = MiracleCosts.Get(miracle);
        return god.Faith >= cost;
    }

    public async Task<float> ConsumeFaithAsync(string godId, MiracleType miracle)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null) return 0f;

        var cost = MiracleCosts.Get(miracle);
        var newFaith = MathF.Max(0f, god.Faith - cost);

        await _godRepo.UpdateFaithAsync(godId, newFaith, god.Trust, god.Fear, god.FollowerCount);
        _logger.LogInformation("God {GodId} spent {Cost} faith on {Miracle}. Remaining: {Faith}",
            godId, cost, miracle, newFaith);

        return cost;
    }

    // Gọi mỗi tick - tính toán faith sinh ra từ followers, temples, rituals
    public async Task<float> GenerateFaithTickAsync(string worldId)
    {
        var gods = await _godRepo.GetByWorldAsync(worldId);
        var religions = await _religionRepo.GetByWorldAsync(worldId);
        var civs = await _civRepo.GetByWorldAsync(worldId);

        float totalGenerated = 0f;

        foreach (var god in gods)
        {
            if (!god.IsAlive) continue;

            var godReligions = religions.Where(r => r.GodId == god.Id).ToList();

            // Faith từ followers cơ bản
            float faithFromFollowers = god.FollowerCount * 0.01f;

            // Faith từ temples
            float faithFromTemples = godReligions.Sum(r => r.TempleCount) * 0.5f;

            // Faith bonus từ devotion level
            float devotionBonus = godReligions.Sum(r => r.DevotionLevel * r.FollowerCount * 0.005f);

            // Fear resource cho dark gods
            float fearResource = god.Archetype is GodArchetype.Darkness or GodArchetype.Death or GodArchetype.Chaos
                ? god.Fear * 0.02f : 0f;

            float totalFaith = faithFromFollowers + faithFromTemples + devotionBonus + fearResource;
            float newFaith = MathF.Min(1000f, god.Faith + totalFaith);

            await _godRepo.UpdateFaithAsync(god.Id, newFaith, god.Trust, god.Fear, god.FollowerCount);
            totalGenerated += totalFaith;
        }

        return totalGenerated;
    }
}
