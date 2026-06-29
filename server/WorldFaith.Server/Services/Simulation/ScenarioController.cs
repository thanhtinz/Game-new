using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Contracts;

namespace WorldFaith.Server.Services.Simulation;

// ─── Scenario Definitions ────────────────────────────────
public enum ScenarioType
{
    Standard,                 // Game thường
    TheLastLight,             // 1 god of Light vs nhiều Darkness gods
    ReligionWars,             // Chỉ thắng bằng religion domination
    EvolutionRace,            // Ai evolve entity đến Apex đầu tiên
    FaithCrisis,              // Faith tạo ra rất chậm, phải economy giỏi
    Apocalypse,               // Monsters mạnh gấp đôi, phải survive
}

public class ScenarioConfig
{
    public ScenarioType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxCycles { get; set; } = 3;           // Kết thúc sau N cycles
    public float FaithMultiplier { get; set; } = 1f;  // Tốc độ faith gen
    public float EntityPowerMultiplier { get; set; } = 1f;
    public bool ReligionWinOnly { get; set; } = false;
    public bool ApexWinCondition { get; set; } = false;
    public int MinPlayersForScenario { get; set; } = 2;
}

public static class ScenarioConfigs
{
    public static readonly Dictionary<ScenarioType, ScenarioConfig> All = new()
    {
        [ScenarioType.Standard] = new ScenarioConfig
        {
            Type = ScenarioType.Standard,
            Name = "Tiêu Chuẩn",
            Description = "Game thông thường. Nhiều điều kiện thắng.",
            MaxCycles = 999
        },
        [ScenarioType.TheLastLight] = new ScenarioConfig
        {
            Type = ScenarioType.TheLastLight,
            Name = "Ánh Sáng Cuối Cùng",
            Description = "1 god Light chống lại tất cả. Light thắng nếu sống sót 3 cycles.",
            MaxCycles = 3,
            MinPlayersForScenario = 3
        },
        [ScenarioType.ReligionWars] = new ScenarioConfig
        {
            Type = ScenarioType.ReligionWars,
            Name = "Thánh Chiến",
            Description = "Chỉ thắng khi tôn giáo của bạn chiếm > 70% followers thế giới.",
            MaxCycles = 5,
            ReligionWinOnly = true
        },
        [ScenarioType.EvolutionRace] = new ScenarioConfig
        {
            Type = ScenarioType.EvolutionRace,
            Name = "Đua Tiến Hóa",
            Description = "God đầu tiên evolve entity lên Apex stage thắng ngay lập tức.",
            MaxCycles = 999,
            ApexWinCondition = true
        },
        [ScenarioType.FaithCrisis] = new ScenarioConfig
        {
            Type = ScenarioType.FaithCrisis,
            Name = "Khủng Hoảng Niềm Tin",
            Description = "Faith tạo ra chậm hơn 5x. Mỗi miracle phải cân nhắc kỹ.",
            MaxCycles = 3,
            FaithMultiplier = 0.2f
        },
        [ScenarioType.Apocalypse] = new ScenarioConfig
        {
            Type = ScenarioType.Apocalypse,
            Name = "Ngày Tận Thế",
            Description = "Monsters mạnh gấp 3x. Civilizations liên tục bị tấn công. Survive!",
            MaxCycles = 2,
            EntityPowerMultiplier = 3f
        },
    };
}

// ─── Scenario Controller ─────────────────────────────────
public interface IScenarioController
{
    Task<(bool ended, string? winnerId, string reason)> CheckWinConditionAsync(
        string worldId, long tick, int cycle, ScenarioConfig config);
    Task ApplyScenarioEffectsAsync(string worldId, ScenarioConfig config, long tick);
}

public class ScenarioController : IScenarioController
{
    private readonly IGodRepository _godRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IEvolutionEntityRepository _entityRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly ILogger<ScenarioController> _logger;

    public ScenarioController(
        IGodRepository godRepo,
        IReligionRepository religionRepo,
        IEvolutionEntityRepository entityRepo,
        IWorldRepository worldRepo,
        ILogger<ScenarioController> logger)
    {
        _godRepo = godRepo;
        _religionRepo = religionRepo;
        _entityRepo = entityRepo;
        _worldRepo = worldRepo;
        _logger = logger;
    }

    public async Task<(bool ended, string? winnerId, string reason)> CheckWinConditionAsync(
        string worldId, long tick, int cycle, ScenarioConfig config)
    {
        var gods = await _godRepo.GetByWorldAsync(worldId);
        var aliveGods = gods.Where(g => g.IsAlive && g.FollowerCount > 0).ToList();

        // Tất cả scenarios: nếu chỉ còn 1 god → thắng
        if (aliveGods.Count == 1)
            return (true, aliveGods[0].PlayerId, "Thần cuối cùng còn lại!");

        if (aliveGods.Count == 0)
            return (true, null, "Tất cả thần đã biến mất — không ai thắng!");

        // Kiểm tra max cycles
        if (config.MaxCycles != 999 && cycle >= config.MaxCycles)
        {
            // Sau max cycles: god nhiều followers nhất thắng
            var winner = aliveGods.OrderByDescending(g => g.FollowerCount).First();
            return (true, winner.PlayerId, $"Đạt {config.MaxCycles} cycles — người nhiều followers nhất thắng!");
        }

        switch (config.Type)
        {
            case ScenarioType.ReligionWars:
                return await CheckReligionDominationAsync(worldId, gods);

            case ScenarioType.EvolutionRace:
                return await CheckApexEvolutionAsync(worldId, gods);

            case ScenarioType.TheLastLight:
                return CheckLastLightAsync(gods, cycle, config.MaxCycles);
        }

        return (false, null, "");
    }

    public async Task ApplyScenarioEffectsAsync(string worldId, ScenarioConfig config, long tick)
    {
        // Áp dụng entity power multiplier
        if (config.EntityPowerMultiplier != 1f && tick % 50 == 0)
        {
            var entities = await _entityRepo.GetByWorldAsync(worldId);
            foreach (var entity in entities)
            {
                // Chỉ boost monsters trong Apocalypse
                if (entity.Stage is Shared.Enums.EvolutionStage.Monster
                    or Shared.Enums.EvolutionStage.Titan
                    or Shared.Enums.EvolutionStage.ApocalypticEntity)
                {
                    entity.Power = WorldFaith.Server.Services.Evolution.EvolutionService.GetStagePower(entity.Stage)
                        * config.EntityPowerMultiplier;
                    await _entityRepo.UpdateAsync(entity);
                }
            }
        }
    }

    // ─── Condition Checks ────────────────────────────────────

    private async Task<(bool, string?, string)> CheckReligionDominationAsync(
        string worldId, List<GodDocument> gods)
    {
        var religions = await _religionRepo.GetByWorldAsync(worldId);
        int total = religions.Sum(r => r.FollowerCount);
        if (total == 0) return (false, null, "");

        foreach (var god in gods)
        {
            int godFollowers = religions
                .Where(r => r.GodId == god.Id)
                .Sum(r => r.FollowerCount);

            float ratio = (float)godFollowers / total;
            if (ratio >= 0.70f)
                return (true, god.PlayerId,
                    $"Tôn giáo của {god.Name} chiếm {ratio:P0} tín đồ thế giới!");
        }

        return (false, null, "");
    }

    private async Task<(bool, string?, string)> CheckApexEvolutionAsync(
        string worldId, List<GodDocument> gods)
    {
        var entities = await _entityRepo.GetByWorldAsync(worldId);
        var apex = entities.FirstOrDefault(e =>
            e.Stage is Shared.Enums.EvolutionStage.CelestialGuardian
            or Shared.Enums.EvolutionStage.ApocalypticEntity
            or Shared.Enums.EvolutionStage.FallenDemonLord);

        if (apex?.GodInfluenceId == null) return (false, null, "");

        var winnerGod = gods.FirstOrDefault(g => g.Id == apex.GodInfluenceId);
        if (winnerGod == null) return (false, null, "");

        return (true, winnerGod.PlayerId,
            $"{winnerGod.Name} đã tiến hóa {apex.Name} lên {apex.Stage}!");
    }

    private static (bool, string?, string) CheckLastLightAsync(
        List<GodDocument> gods, int cycle, int maxCycles)
    {
        var lightGod = gods.FirstOrDefault(g =>
            g.Archetype == Shared.Enums.GodArchetype.Light && g.IsAlive);

        if (lightGod == null)
            return (true, null, "Ánh sáng đã bị dập tắt — Bóng Tối chiến thắng!");

        if (cycle >= maxCycles)
            return (true, lightGod.PlayerId,
                $"{lightGod.Name} đã giữ vững ánh sáng qua {cycle} chu kỳ!");

        return (false, null, "");
    }
}
