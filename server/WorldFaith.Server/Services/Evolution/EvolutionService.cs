using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Evolution;

public interface IEvolutionService
{
    Task SpawnInitialEntitiesAsync(string worldId, List<WorldTileData> tiles);
    Task<List<DeltaEvent>> TickAsync(string worldId, long tick);
    Task<bool> EvolveEntityAsync(string worldId, string entityId, string godId);
    Task<EvolutionEntityDocument?> CreateDivineBeastAsync(string worldId, string godId, int x, int y);
}

public class EvolutionService : IEvolutionService
{
    private readonly IEvolutionEntityRepository _entityRepo;
    private readonly IGodRepository _godRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IWorldRepository _worldRepo;
    private readonly ILogger<EvolutionService> _logger;
    private readonly Random _rng = new();

    // Điểm evolution cần để lên stage tiếp theo
    private static readonly Dictionary<EvolutionStage, int> EvolutionThresholds = new()
    {
        { EvolutionStage.WildAnimal,   100 },
        { EvolutionStage.DivineBeast,  500 },
        // CelestialGuardian = max, không tiến hóa thêm

        { EvolutionStage.HumanHero,    150 },
        { EvolutionStage.Saint,        600 },
        // FallenDemonLord = max

        { EvolutionStage.Monster,      120 },
        { EvolutionStage.Titan,        450 },
        // ApocalypticEntity = max
    };

    // Power stats theo stage
    private static readonly Dictionary<EvolutionStage, float> StagePower = new()
    {
        { EvolutionStage.WildAnimal,        10f },
        { EvolutionStage.DivineBeast,       80f },
        { EvolutionStage.CelestialGuardian, 300f },
        { EvolutionStage.HumanHero,         30f },
        { EvolutionStage.Saint,             150f },
        { EvolutionStage.FallenDemonLord,   400f },
        { EvolutionStage.Monster,           20f },
        { EvolutionStage.Titan,             200f },
        { EvolutionStage.ApocalypticEntity, 500f },
    };

    // Tên entity theo stage
    private static readonly Dictionary<EvolutionStage, string[]> StageNames = new()
    {
        { EvolutionStage.WildAnimal,        new[] { "Sói Hoang", "Gấu Rừng", "Đại Bàng", "Hổ Đen" } },
        { EvolutionStage.DivineBeast,       new[] { "Thần Thú", "Linh Thú Lửa", "Rồng Thiên", "Kỳ Lân" } },
        { EvolutionStage.CelestialGuardian, new[] { "Hộ Pháp Thiên Giới", "Thần Vệ Binh", "Thiên Long" } },
        { EvolutionStage.HumanHero,         new[] { "Chiến Binh", "Pháp Sư", "Cung Thủ Huyền", "Hiệp Sĩ" } },
        { EvolutionStage.Saint,             new[] { "Thánh Nhân", "Tiên Tri", "Đại Thánh", "Thần Sứ" } },
        { EvolutionStage.FallenDemonLord,   new[] { "Ma Vương Sa Đọa", "Ác Thần Tàn Bạo", "Quỷ Chúa" } },
        { EvolutionStage.Monster,           new[] { "Quái Vật", "Thủy Quái", "Thạch Khổng Lồ", "Ác Thú" } },
        { EvolutionStage.Titan,             new[] { "Titan Cổ Đại", "Khổng Lồ Bóng Tối", "Thần Thú Titan" } },
        { EvolutionStage.ApocalypticEntity, new[] { "Thực Thể Tận Thế", "Hủy Diệt Giả", "Quái Vật Cổ Thần" } },
    };

    public EvolutionService(
        IEvolutionEntityRepository entityRepo,
        IGodRepository godRepo,
        ICivilizationRepository civRepo,
        IWorldRepository worldRepo,
        ILogger<EvolutionService> logger)
    {
        _entityRepo = entityRepo;
        _godRepo = godRepo;
        _civRepo = civRepo;
        _worldRepo = worldRepo;
        _logger = logger;
    }

    // ─── Spawn ───────────────────────────────────────────────

    public async Task SpawnInitialEntitiesAsync(string worldId, List<WorldTileData> tiles)
    {
        // Spawn WildAnimals trên Forest/Grassland
        var landTiles = tiles
            .Where(t => t.Type is TileType.Forest or TileType.Grassland)
            .OrderBy(_ => _rng.Next())
            .Take(12)
            .ToList();

        foreach (var tile in landTiles)
        {
            var entity = new EvolutionEntityDocument
            {
                WorldId = worldId,
                Stage = EvolutionStage.WildAnimal,
                Name = PickName(EvolutionStage.WildAnimal),
                X = tile.X,
                Y = tile.Y,
                Power = StagePower[EvolutionStage.WildAnimal],
                EvolutionPoints = 0
            };
            await _entityRepo.CreateAsync(entity);
        }

        // Spawn Monsters trên Mountain/Tundra
        var harshTiles = tiles
            .Where(t => t.Type is TileType.Mountain or TileType.Tundra or TileType.Volcano)
            .OrderBy(_ => _rng.Next())
            .Take(6)
            .ToList();

        foreach (var tile in harshTiles)
        {
            var entity = new EvolutionEntityDocument
            {
                WorldId = worldId,
                Stage = EvolutionStage.Monster,
                Name = PickName(EvolutionStage.Monster),
                X = tile.X,
                Y = tile.Y,
                Power = StagePower[EvolutionStage.Monster],
                EvolutionPoints = 0
            };
            await _entityRepo.CreateAsync(entity);
        }

        // Spawn HumanHeroes gần Sacred sites
        var sacredTiles = tiles
            .Where(t => t.Type == TileType.Sacred)
            .OrderBy(_ => _rng.Next())
            .Take(4)
            .ToList();

        foreach (var tile in sacredTiles)
        {
            var entity = new EvolutionEntityDocument
            {
                WorldId = worldId,
                Stage = EvolutionStage.HumanHero,
                Name = PickName(EvolutionStage.HumanHero),
                X = tile.X,
                Y = tile.Y,
                Power = StagePower[EvolutionStage.HumanHero],
                EvolutionPoints = 0
            };
            await _entityRepo.CreateAsync(entity);
        }

        _logger.LogInformation("Spawned {Count} initial entities cho world {WorldId}",
            landTiles.Count + harshTiles.Count + sacredTiles.Count, worldId);
    }

    // ─── Tick ────────────────────────────────────────────────

    public async Task<List<DeltaEvent>> TickAsync(string worldId, long tick)
    {
        var entities = await _entityRepo.GetByWorldAsync(worldId);
        var gods = await _godRepo.GetByWorldAsync(worldId);
        var civs = await _civRepo.GetByWorldAsync(worldId);
        var deltas = new List<DeltaEvent>();

        foreach (var entity in entities)
        {
            bool changed = false;

            // Tích lũy evolution points tự nhiên
            int naturalGain = entity.Stage switch
            {
                EvolutionStage.WildAnimal or EvolutionStage.Monster => _rng.Next(1, 4),
                EvolutionStage.HumanHero  => _rng.Next(2, 6),
                _ => _rng.Next(1, 3)
            };

            // Bonus nếu được god influence
            if (entity.GodInfluenceId != null)
            {
                var god = gods.FirstOrDefault(g => g.Id == entity.GodInfluenceId);
                if (god != null)
                {
                    float faithBonus = god.Faith / 200f;
                    naturalGain = (int)(naturalGain * (1f + faithBonus));
                }
            }

            // Bonus nếu đứng trên Sacred tile
            var world = await _worldRepo.GetByIdAsync(worldId);
            var tile = world?.Tiles.FirstOrDefault(t => t.X == entity.X && t.Y == entity.Y);
            if (tile?.Type == TileType.Sacred)
                naturalGain = (int)(naturalGain * 1.5f);

            entity.EvolutionPoints += naturalGain;
            changed = true;

            // Kiểm tra evolve tự động
            if (EvolutionThresholds.TryGetValue(entity.Stage, out int threshold)
                && entity.EvolutionPoints >= threshold)
            {
                var nextStage = GetNextStage(entity.Stage);
                if (nextStage.HasValue)
                {
                    var oldStage = entity.Stage;
                    entity.Stage = nextStage.Value;
                    entity.Name = PickName(nextStage.Value);
                    entity.Power = StagePower[nextStage.Value];
                    entity.EvolutionPoints = 0;

                    deltas.Add(new DeltaEvent
                    {
                        Type = WorldEventType.EvolutionOccurred,
                        TargetId = entity.Id,
                        X = entity.X,
                        Y = entity.Y,
                        Description = $"{entity.Name} tiến hóa từ {oldStage} → {nextStage.Value}!"
                    });

                    _logger.LogInformation("Entity {Name} evolved: {Old} → {New}",
                        entity.Name, oldStage, nextStage.Value);

                    // Apex entities gây sự kiện thế giới
                    if (nextStage.Value is EvolutionStage.ApocalypticEntity or EvolutionStage.CelestialGuardian)
                        await HandleApexEventAsync(worldId, entity, civs, deltas);
                }
            }

            // Di chuyển entity ngẫu nhiên
            if (tick % 10 == 0)
            {
                MoveEntity(entity, world);
                changed = true;
            }

            // Entity tấn công civilization lân cận (chỉ Monster/Titan/Apocalyptic)
            if (tick % 20 == 0 && IsAggressiveStage(entity.Stage))
            {
                var attacked = await AttackNearbyCivAsync(entity, civs, deltas);
                if (attacked) changed = true;
            }

            if (changed)
                await _entityRepo.UpdateAsync(entity);
        }

        return deltas;
    }

    // ─── Manual Evolve (god action) ─────────────────────────

    public async Task<bool> EvolveEntityAsync(string worldId, string entityId, string godId)
    {
        var entity = await _entityRepo.GetByIdAsync(entityId);
        var god = await _godRepo.GetByIdAsync(godId);
        if (entity == null || god == null) return false;
        if (entity.WorldId != worldId) return false;

        // Tốn Faith để force evolve
        float cost = 50f;
        if (god.Faith < cost) return false;

        var nextStage = GetNextStage(entity.Stage);
        if (!nextStage.HasValue) return false;

        entity.Stage = nextStage.Value;
        entity.Name = PickName(nextStage.Value);
        entity.Power = StagePower[nextStage.Value];
        entity.EvolutionPoints = 0;
        entity.GodInfluenceId = godId;

        await _entityRepo.UpdateAsync(entity);
        await _godRepo.UpdateFaithAsync(godId, god.Faith - cost, god.Trust, god.Fear, god.FollowerCount);

        _logger.LogInformation("God {GodId} force-evolved {Entity} to {Stage}", godId, entity.Name, nextStage.Value);
        return true;
    }

    // ─── Create Divine Beast (miracle action) ───────────────

    public async Task<EvolutionEntityDocument?> CreateDivineBeastAsync(string worldId, string godId, int x, int y)
    {
        var entity = new EvolutionEntityDocument
        {
            WorldId = worldId,
            GodInfluenceId = godId,
            Stage = EvolutionStage.DivineBeast,
            Name = PickName(EvolutionStage.DivineBeast),
            X = x,
            Y = y,
            Power = StagePower[EvolutionStage.DivineBeast],
            EvolutionPoints = 0
        };
        await _entityRepo.CreateAsync(entity);

        _logger.LogInformation("God {GodId} tạo Divine Beast tại ({X},{Y})", godId, x, y);
        return entity;
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static EvolutionStage? GetNextStage(EvolutionStage current) => current switch
    {
        EvolutionStage.WildAnimal  => EvolutionStage.DivineBeast,
        EvolutionStage.DivineBeast => EvolutionStage.CelestialGuardian,
        EvolutionStage.HumanHero   => EvolutionStage.Saint,
        EvolutionStage.Saint       => EvolutionStage.FallenDemonLord,
        EvolutionStage.Monster     => EvolutionStage.Titan,
        EvolutionStage.Titan       => EvolutionStage.ApocalypticEntity,
        _ => null // Đã đến max stage
    };

    private static bool IsAggressiveStage(EvolutionStage stage) =>
        stage is EvolutionStage.Monster or EvolutionStage.Titan
            or EvolutionStage.ApocalypticEntity or EvolutionStage.FallenDemonLord;

    private void MoveEntity(EvolutionEntityDocument entity, WorldDocument? world)
    {
        if (world == null) return;
        int dx = _rng.Next(-2, 3);
        int dy = _rng.Next(-2, 3);
        entity.X = Math.Clamp(entity.X + dx, 0, world.Width - 1);
        entity.Y = Math.Clamp(entity.Y + dy, 0, world.Height - 1);
    }

    private async Task<bool> AttackNearbyCivAsync(
        EvolutionEntityDocument entity,
        List<CivilizationDocument> civs,
        List<DeltaEvent> deltas)
    {
        var nearby = civs.FirstOrDefault(c =>
            c.State != CivilizationState.Fallen &&
            c.ControlledTiles.Any(t => MathF.Abs(t.X - entity.X) <= 5 && MathF.Abs(t.Y - entity.Y) <= 5));

        if (nearby == null) return false;

        float damage = entity.Power * 0.1f;
        nearby.Military = MathF.Max(0f, nearby.Military - damage);
        nearby.Population -= (int)(damage * 2f);

        if (nearby.Population < 0) nearby.Population = 0;
        await _civRepo.UpdateAsync(nearby);

        deltas.Add(new DeltaEvent
        {
            Type = WorldEventType.MiraclePerformed,
            TargetId = nearby.Id,
            X = entity.X,
            Y = entity.Y,
            Description = $"{entity.Name} tấn công {nearby.Name}! Military -{damage:F0}, Population -{(int)(damage * 2)}"
        });

        return true;
    }

    private async Task HandleApexEventAsync(
        string worldId, EvolutionEntityDocument entity,
        List<CivilizationDocument> civs, List<DeltaEvent> deltas)
    {
        // Apex entity gây hoảng loạn cho tất cả civ trong bán kính 15
        var nearbyCivs = civs.Where(c =>
            c.ControlledTiles.Any(t =>
                MathF.Sqrt(MathF.Pow(t.X - entity.X, 2) + MathF.Pow(t.Y - entity.Y, 2)) <= 15f))
            .ToList();

        foreach (var civ in nearbyCivs)
        {
            civ.Military = MathF.Max(0f, civ.Military - 20f);
            civ.Economy = MathF.Max(0f, civ.Economy - 15f);
            await _civRepo.UpdateAsync(civ);
        }

        deltas.Add(new DeltaEvent
        {
            Type = WorldEventType.EvolutionOccurred,
            TargetId = entity.Id,
            X = entity.X,
            Y = entity.Y,
            Description = $"⚠️ {entity.Name} xuất hiện! {nearbyCivs.Count} civilization kinh hoàng!"
        });
    }

    private string PickName(EvolutionStage stage)
    {
        if (!StageNames.TryGetValue(stage, out var names)) return stage.ToString();
        return names[_rng.Next(names.Length)];
    }
}
