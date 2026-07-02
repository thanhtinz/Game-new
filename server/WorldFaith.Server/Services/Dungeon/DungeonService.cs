using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Dungeon;

public interface IDungeonService
{
    Task<DungeonDocument> SpawnDungeonAsync(string worldId, int x, int y, DungeonType type, string? godId = null);
    Task<List<DeltaEvent>> TickAsync(string worldId, long tick);
    Task<GuildMissionDocument> StartMissionAsync(string worldId, string dungeonId, string orgId, List<string> adventurerIds);
    Task<GuildMissionDocument> ResolveMissionAsync(string missionId);
    Task<RelicDocument> CreateRelicAsync(string worldId, string godId, RelicType type);
}

public class DungeonService : IDungeonService
{
    private readonly IDungeonRepository _dungeonRepo;
    private readonly IRelicRepository _relicRepo;
    private readonly IGuildMissionRepository _missionRepo;
    private readonly INpcRepository _npcRepo;
    private readonly IOrganizationRepository _orgRepo;
    private readonly IGodRepository _godRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IBalanceConfigService _balance;
    private readonly ILogger<DungeonService> _logger;
    private readonly Random _rng = new();

    private static readonly string[] RelicNames =
    {
        "Memory Stone", "Ancient Scripture", "Divine Shard", "Mythic Weapon",
        "Forgotten Idol", "Sacred Bone", "Mystic Gem", "Divine Relic",
        "Faith Vessel", "Old Crusade Banner"
    };

    private static readonly Dictionary<DungeonType, (float minDanger, float maxDanger, float reward)> DungeonStats = new()
    {
        [DungeonType.AncientRuins]      = (20f, 50f, 60f),
        [DungeonType.MonstersLair]      = (40f, 70f, 80f),
        [DungeonType.ForbiddenSanctum]  = (60f, 90f, 120f),
        [DungeonType.LostTemple]        = (30f, 60f, 100f),  // Higher relic chance
        [DungeonType.DarkPortal]        = (70f, 100f, 150f),
    };

    public DungeonService(
        IDungeonRepository dungeonRepo,
        IRelicRepository relicRepo,
        IGuildMissionRepository missionRepo,
        INpcRepository npcRepo,
        IOrganizationRepository orgRepo,
        IGodRepository godRepo,
        ICivilizationRepository civRepo,
        IBalanceConfigService balance,
        ILogger<DungeonService> logger)
    {
        _dungeonRepo = dungeonRepo;
        _relicRepo = relicRepo;
        _missionRepo = missionRepo;
        _npcRepo = npcRepo;
        _orgRepo = orgRepo;
        _godRepo = godRepo;
        _civRepo = civRepo;
        _balance = balance;
        _logger = logger;
    }

    // ─── Spawn Dungeon ────────────────────────────────────────

    public async Task<DungeonDocument> SpawnDungeonAsync(
        string worldId, int x, int y, DungeonType type, string? godId = null)
    {
        var (minD, maxD, reward) = DungeonStats[type];
        var dungeon = new DungeonDocument
        {
            WorldId = worldId,
            Type = type,
            X = x, Y = y,
            DangerLevel = (float)(_rng.NextDouble() * (maxD - minD) + minD),
            Reward = reward,
            OriginGodId = godId,
            SpawnedAtTick = 0
        };

        // 40% chance of relic inside
        if (_rng.NextDouble() < 0.4 && godId != null)
        {
            var relic = await CreateRelicAsync(worldId, godId, PickRelicType());
            dungeon.RelicId = relic.Id;
            relic.LocationDungeonId = dungeon.Id;
            await _relicRepo.UpdateAsync(relic);
        }

        await _dungeonRepo.CreateAsync(dungeon);
        _logger.LogInformation("Dungeon spawned: {Type} at ({X},{Y}) danger={Danger:F0}", type, x, y, dungeon.DangerLevel);
        return dungeon;
    }

    // ─── Tick — Auto-spawn and Infest ─────────────────────────

    public async Task<List<DeltaEvent>> TickAsync(string worldId, long tick)
    {
        var deltas = new List<DeltaEvent>();
        var dungeons = await _dungeonRepo.GetByWorldAsync(worldId);

        // 1% chance per 50 ticks to spawn a natural dungeon
        if (tick % 50 == 0 && _rng.NextDouble() < 0.01)
        {
            int x = _rng.Next(0, 64);
            int y = _rng.Next(0, 64);
            var type = (DungeonType)_rng.Next(Enum.GetValues<DungeonType>().Length);
            var d = await SpawnDungeonAsync(worldId, x, y, type);
            deltas.Add(new DeltaEvent
            {
                Type = WorldEventType.DivineConflict,
                Description = $"A {d.Type} dungeon has appeared at ({d.X},{d.Y})!"
            });
        }

        // Check dungeons chưa was clear
        foreach (var dungeon in dungeons.Where(d => d.State == DungeonState.Active))
        {
            // Sau 200 ticks not ai clear → Infested
            long age = tick - dungeon.SpawnedAtTick;
            if (age > 200 && dungeon.State == DungeonState.Active && _rng.NextDouble() < 0.05)
            {
                dungeon.State = DungeonState.Infested;
                dungeon.DangerLevel = Math.Min(100f, dungeon.DangerLevel * 1.3f);
                await _dungeonRepo.UpdateAsync(dungeon);
                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.DivineConflict,
                    Description = $"Dungeon {dungeon.Type} at ({dungeon.X},{dungeon.Y}) has become infested and more dangerous!"
                });
            }

            // DarkPortal: có thể spawn entities nếu not was seal
            if (dungeon.Type == DungeonType.DarkPortal && dungeon.State == DungeonState.Active
                && tick % 30 == 0 && _rng.NextDouble() < 0.15)
            {
                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.DivineConflict,
                    Description = $"⚠️ Dark Portal at ({dungeon.X},{dungeon.Y}) is leaking dark energy!"
                });
            }
        }

        return deltas;
    }

    // ─── Guild Missions ───────────────────────────────────────

    public async Task<GuildMissionDocument> StartMissionAsync(
        string worldId, string dungeonId, string orgId, List<string> adventurerIds)
    {
        var mission = new GuildMissionDocument
        {
            WorldId = worldId,
            DungeonId = dungeonId,
            OrganizationId = orgId,
            AdventurerIds = adventurerIds,
            State = GuildMissionState.Active,
            StartedAtTick = 0
        };
        await _missionRepo.CreateAsync(mission);
        _logger.LogInformation("Guild mission started: {AdventurerCount} adventurers → dungeon {DungeonId}",
            adventurerIds.Count, dungeonId);
        return mission;
    }

    public async Task<GuildMissionDocument> ResolveMissionAsync(string missionId)
    {
        var mission = await _missionRepo.GetActiveByOrgAsync(missionId) ??
            throw new InvalidOperationException("Mission not found");

        var dungeon = await _dungeonRepo.GetByIdAsync(mission.DungeonId);
        if (dungeon == null) { mission.State = GuildMissionState.Failed; return mission; }

        // Calculate survival chance from adventurer stats vs danger
        var adventurers = new List<NpcDocument>();
        foreach (var id in mission.AdventurerIds)
        {
            var npc = await _npcRepo.GetByIdAsync(id);
            if (npc != null) adventurers.Add(npc);
        }

        float partyStrength = adventurers.Sum(a => a.EvolutionPoints + a.Piety * 0.5f) / Math.Max(1, adventurers.Count);
        float survivalChance = Math.Clamp(partyStrength / dungeon.DangerLevel, 0.1f, 0.95f);

        bool succeeded = _rng.NextDouble() < survivalChance;

        if (succeeded)
        {
            mission.State = GuildMissionState.Success;
            dungeon.State = DungeonState.Cleared;

            // Relic discovery
            if (dungeon.RelicId != null)
            {
                mission.DiscoveredRelicId = dungeon.RelicId;
                var relic = await _relicRepo.GetByIdAsync(dungeon.RelicId);
                if (relic != null)
                {
                    relic.LocationDungeonId = null;
                    relic.CurrentOwnerId = adventurers.FirstOrDefault()?.Id;
                    await _relicRepo.UpdateAsync(relic);
                }
            }

            // EXP for adventurers
            foreach (var adv in adventurers)
            {
                adv.EvolutionPoints += (int)(dungeon.DangerLevel * 0.5f);
                await _npcRepo.UpdateAsync(adv);
            }

            float faithGain = dungeon.Reward;
            mission.FaithImpact = faithGain;
            mission.OutcomeDescription = $"The party succeeded! Gained {faithGain:F0} faith. {(dungeon.RelicId != null ? "Relic discovered!" : "")}";
        }
        else
        {
            // Failure — some adventurers may die
            mission.State = _rng.NextDouble() < 0.3
                ? GuildMissionState.Corrupted  // was corrupt bởi dungeon
                : GuildMissionState.Failed;

            int deaths = _rng.Next(1, Math.Min(3, adventurers.Count) + 1);
            var fallen = adventurers.OrderBy(_ => _rng.Next()).Take(deaths).ToList();
            foreach (var adv in fallen)
            {
                adv.State = NpcState.Dead;
                await _npcRepo.UpdateAsync(adv);
            }

            mission.FaithImpact = -dungeon.Reward * 0.3f; // faith loss từ deaths
            mission.OutcomeDescription = $"{deaths} fell in the dungeon. Faith -{MathF.Abs(mission.FaithImpact):F0}.";
        }

        mission.CompletedAtTick = 0;
        await _dungeonRepo.UpdateAsync(dungeon);
        await _missionRepo.UpdateAsync(mission);

        _logger.LogInformation("Mission resolved: {State} — {Description}", mission.State, mission.OutcomeDescription);
        return mission;
    }

    // ─── Relic Creation ───────────────────────────────────────

    public async Task<RelicDocument> CreateRelicAsync(string worldId, string godId, RelicType type)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        string relicName = $"{RelicNames[_rng.Next(RelicNames.Length)]} of {god?.Name ?? "Thần Vô Danh"}";

        var relic = new RelicDocument
        {
            WorldId = worldId,
            Type = type,
            Name = relicName,
            OriginGodId = godId,
            MemoryPower = (float)(_rng.NextDouble() * 50f + 25f),  // 25-75
            FaithBonus  = (float)(_rng.NextDouble() * 10f + 2f),   // 2-12/tick
            IsActive = true
        };

        // Link back to god
        if (god != null)
        {
            god.RelicIds.Add(relic.Id);
            await _godRepo.UpdateAsync(god);
        }

        await _relicRepo.CreateAsync(relic);
        _logger.LogInformation("Relic created: '{Name}' (type={Type}, faith={Faith:F1}/tick)", relic.Name, type, relic.FaithBonus);
        return relic;
    }

    private RelicType PickRelicType() => (RelicType)_rng.Next(Enum.GetValues<RelicType>().Length);
}
