using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC;

/// <summary>
/// Escort & Retinue System — Add-On v1.2.
/// Important NPCs (saints, prophets, high priests, top God Note candidates)
/// attract escorts based on their rank, fame, and danger level.
/// Escort strength = NPC Religious Rank + God Note Rank + Public Fame + Danger + Kingdom Support + Church Wealth
/// </summary>
public interface IEscortService
{
    Task<EscortGroup?> GenerateEscortAsync(string worldId, NpcDocument vip, long tick);
    Task<List<DeltaEvent>> TickEscortsAsync(string worldId, long tick);
    Task<bool> AttemptKidnapAsync(string worldId, NpcDocument vip, string attackerOrgId, long tick);
    Task DisbandEscortAsync(string npcId);
    Task CheckEscortBetrayalAsync(string worldId, NpcDocument vip, long tick);
}

public class EscortService : IEscortService
{
    private readonly INpcRepository _npcRepo;
    private readonly IOrganizationRepository _orgRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IGodRepository _godRepo;
    private readonly IDoctrineIntegrityService _doctrineService;
    private readonly ILogger<EscortService> _logger;
    private readonly Random _rng = new();

    // Escort size by church rank (GDD §10 table)
    private static readonly Dictionary<ChurchRank, (int min, int max, EscortRole[] roles)> EscortConfig = new()
    {
        [ChurchRank.Priest]        = (1, 3,  new[] { EscortRole.GuardKnight }),
        [ChurchRank.HighPriest]    = (3, 8,  new[] { EscortRole.GuardKnight, EscortRole.Scribe, EscortRole.Disciple }),
        [ChurchRank.Prophet]       = (5, 20, new[] { EscortRole.GuardKnight, EscortRole.PilgrimFollower, EscortRole.Disciple }),
        [ChurchRank.Saint]         = (8, 30, new[] { EscortRole.GuardKnight, EscortRole.Healer, EscortRole.Disciple, EscortRole.Fanatic }),
        [ChurchRank.DivineAvatar]  = (20, 50,new[] { EscortRole.GuardKnight, EscortRole.Fanatic, EscortRole.Healer, EscortRole.Disciple }),
        // Dark equivalents
        [ChurchRank.DarkPriest]    = (2, 6,  new[] { EscortRole.CultistAgent }),
        [ChurchRank.BloodSaint]    = (5, 20, new[] { EscortRole.CultistAgent, EscortRole.CorruptedGuard }),
        [ChurchRank.DemonVessel]   = (10, 40,new[] { EscortRole.CultistAgent, EscortRole.CorruptedGuard, EscortRole.Fanatic }),
    };

    public EscortService(
        INpcRepository npcRepo,
        IOrganizationRepository orgRepo,
        ICivilizationRepository civRepo,
        IGodRepository godRepo,
        IDoctrineIntegrityService doctrineService,
        ILogger<EscortService> logger)
    {
        _npcRepo = npcRepo;
        _orgRepo = orgRepo;
        _civRepo = civRepo;
        _godRepo = godRepo;
        _doctrineService = doctrineService;
        _logger = logger;
    }

    // ─── Generate Escort ──────────────────────────────────

    public async Task<EscortGroup?> GenerateEscortAsync(string worldId, NpcDocument vip, long tick)
    {
        var profile = vip.DivineProfile;

        // Only generate for church-rank NPCs (GDD: only God Note / church / noble / adventurer elite)
        if (!EscortConfig.ContainsKey(profile.ChurchRank) && !vip.IsChampion)
            return null;

        // Skip if already has escort
        if (profile.AssignedEscort?.IsActive == true) return profile.AssignedEscort;

        float strength = CalculateEscortStrength(vip);
        var config = EscortConfig.TryGetValue(profile.ChurchRank, out var cfg)
            ? cfg : (min: 2, max: 5, roles: new[] { EscortRole.GuardKnight });

        int size = _rng.Next(config.min, config.max + 1);

        var escort = new EscortGroup
        {
            ProtectedNpcId = vip.Id,
            GroupStrength = strength,
            DangerLevel = 30f,
            IsActive = true,
            LastKnownLocationCivId = vip.CivilizationId,
            FormedAtTick = tick,
        };

        // Generate escort members from pool of NPCs in civ
        var potentialEscorts = await _npcRepo.GetByCivilizationAsync(vip.CivilizationId ?? worldId);
        var available = potentialEscorts
            .Where(n => n.Id != vip.Id && n.Tier <= NpcTier.Servant && n.State == NpcState.Alive)
            .OrderByDescending(n => n.Loyalty)
            .Take(size * 2)
            .ToList();

        for (int i = 0; i < Math.Min(size, available.Count); i++)
        {
            var escortNpc = available[i];
            var role = config.roles[i % config.roles.Length];
            float loyalty = escortNpc.Loyalty + (float)_rng.NextDouble() * 20f;

            // 3% chance escort is secretly corrupted by rival (only for saint/prophet level)
            bool corrupted = profile.ChurchRank >= ChurchRank.Saint && _rng.NextDouble() < 0.03;

            escort.Members.Add(new EscortMember
            {
                NpcId = escortNpc.Id,
                Role = role,
                CurrentBehavior = EscortBehavior.Follow,
                Loyalty = MathF.Clamp(loyalty, 0f, 100f),
                IsCorrupted = corrupted,
            });
        }

        vip.DivineProfile.AssignedEscort = escort;
        await _npcRepo.UpdateAsync(vip);
        await _doctrineService.UpdateWarningTagsAsync(vip.Id);

        _logger.LogInformation("Escort formed for {Name} ({Rank}): {Size} members, strength={Strength:F0}",
            vip.Name, profile.ChurchRank, escort.Members.Count, strength);

        return escort;
    }

    // ─── Tick ─────────────────────────────────────────────

    public async Task<List<DeltaEvent>> TickEscortsAsync(string worldId, long tick)
    {
        var deltas = new List<DeltaEvent>();
        var allNpcs = await _npcRepo.GetByWorldAsync(worldId);

        // Only VIP-class NPCs have escorts
        var vips = allNpcs.Where(n =>
            n.DivineProfile.AssignedEscort?.IsActive == true ||
            n.DivineProfile.ChurchRank >= ChurchRank.Priest
        ).ToList();

        foreach (var vip in vips)
        {
            // Auto-generate escort if missing
            if (vip.DivineProfile.AssignedEscort == null || !vip.DivineProfile.AssignedEscort.IsActive)
            {
                if (ShouldHaveEscort(vip))
                    await GenerateEscortAsync(worldId, vip, tick);
                continue;
            }

            var escort = vip.DivineProfile.AssignedEscort;

            // Check betrayal
            await CheckEscortBetrayalAsync(worldId, vip, tick);

            // 2% chance of assassination attempt per 50 ticks
            if (tick % 50 == 0 && _rng.NextDouble() < 0.02 && vip.DivineProfile.ChurchRank >= ChurchRank.Prophet)
            {
                var orgs = await _orgRepo.GetByWorldAsync(worldId);
                var undergroundOrg = orgs.FirstOrDefault(o => o.Type == OrganizationType.UndergroundOrg);
                if (undergroundOrg != null)
                {
                    var result = await AttemptKidnapAsync(worldId, vip, undergroundOrg.Id, tick);
                    if (result)
                    {
                        deltas.Add(new DeltaEvent
                        {
                            Type = WorldEventType.DivineConflict,
                            SourceGodId = vip.GodInfluenceId,
                            TargetId = vip.CivilizationId,
                            Description = $"{vip.Name} has been kidnapped! Holy war or rescue mission may trigger."
                        });
                    }
                    else
                    {
                        deltas.Add(new DeltaEvent
                        {
                            Type = WorldEventType.MiraclePerformed,
                            SourceGodId = vip.GodInfluenceId,
                            Description = $"Escort protected {vip.Name} from assassination. Faith and fame increase."
                        });
                        // Successful protection → faith gain
                        var god = vip.GodInfluenceId != null ? await _godRepo.GetByIdAsync(vip.GodInfluenceId) : null;
                        if (god != null)
                        {
                            god.Faith = MathF.Min(1000f, god.Faith + 20f);
                            god.Trust = MathF.Min(100f, god.Trust + 5f);
                            await _godRepo.UpdateAsync(god);
                        }
                    }
                }
            }
        }

        return deltas;
    }

    // ─── Kidnap / Assassination ───────────────────────────

    public async Task<bool> AttemptKidnapAsync(string worldId, NpcDocument vip, string attackerOrgId, long tick)
    {
        var escort = vip.DivineProfile.AssignedEscort;
        float escortStrength = escort?.GroupStrength ?? 0f;
        var attackerOrg = await _orgRepo.GetByIdAsync(attackerOrgId);
        if (attackerOrg == null) return false;

        float attackStrength = attackerOrg.Power * 0.5f + (float)_rng.NextDouble() * 30f;

        if (escort != null && escortStrength > attackStrength)
        {
            // Escort wins — VIP protected
            // Check if any escort members died
            if (_rng.NextDouble() < 0.3 && escort.Members.Any())
            {
                var fallen = escort.Members.OrderBy(_ => _rng.Next()).First();
                if (fallen.Role == EscortRole.Fanatic) // fanatics sacrifice willingly
                {
                    var fallenNpc = await _npcRepo.GetByIdAsync(fallen.NpcId);
                    if (fallenNpc != null) { fallenNpc.State = NpcState.Dead; await _npcRepo.UpdateAsync(fallenNpc); }
                    escort.Members.Remove(fallen);
                    _logger.LogInformation("Escort fanatic sacrificed to protect {Name}", vip.Name);
                }
            }
            vip.DivineProfile.AssignedEscort = escort;
            await _npcRepo.UpdateAsync(vip);
            return false;  // kidnap failed
        }
        else
        {
            // Kidnap succeeded — VIP captured
            vip.State = NpcState.Captured;
            if (escort != null) escort.IsActive = false;
            await _npcRepo.UpdateAsync(vip);

            // Faith crisis for god
            if (vip.GodInfluenceId != null)
            {
                var god = await _godRepo.GetByIdAsync(vip.GodInfluenceId);
                if (god != null)
                {
                    god.Faith = MathF.Max(0f, god.Faith - 50f);
                    god.Trust = MathF.Max(0f, god.Trust - 15f);
                    await _godRepo.UpdateAsync(god);
                }
            }
            return true;  // kidnap succeeded
        }
    }

    // ─── Disband ──────────────────────────────────────────

    public async Task DisbandEscortAsync(string npcId)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc?.DivineProfile.AssignedEscort == null) return;
        npc.DivineProfile.AssignedEscort.IsActive = false;
        await _doctrineService.UpdateWarningTagsAsync(npcId);
        await _npcRepo.UpdateAsync(npc);
    }

    // ─── Betrayal Check ───────────────────────────────────

    public async Task CheckEscortBetrayalAsync(string worldId, NpcDocument vip, long tick)
    {
        var escort = vip.DivineProfile.AssignedEscort;
        if (escort == null) return;

        foreach (var member in escort.Members.Where(m => m.IsCorrupted && !m.IsCorrupted))
        {
            // Already flagged corrupted — check if they act
            if (_rng.NextDouble() < 0.02)
            {
                member.CurrentBehavior = EscortBehavior.Betray;
                _logger.LogWarning("Corrupted escort member betrayed {Name}!", vip.Name);
                // Remove them — they become enemy agent
                escort.Members.Remove(member);
                escort.GroupStrength *= 0.8f;
                await _npcRepo.UpdateAsync(vip);
                return;
            }
        }
    }

    // ─── Helpers ──────────────────────────────────────────

    private static bool ShouldHaveEscort(NpcDocument npc)
    {
        return npc.DivineProfile.ChurchRank >= ChurchRank.Priest
            || npc.DivineProfile.IsSaintCandidate
            || npc.DivineProfile.IsProphetCandidate
            || npc.IsChampion;
    }

    private static float CalculateEscortStrength(NpcDocument vip)
    {
        var profile = vip.DivineProfile;
        // GDD §12: Strength = NPC Religious Rank + God Note Rank + Public Fame + Danger Level + Kingdom Support + Church Wealth
        float rankScore = (int)profile.ChurchRank * 10f;
        float attentionScore = MathF.Min(profile.DivineAttentionScore * 0.2f, 50f);
        float tierScore = (int)vip.Tier * 5f;
        return rankScore + attentionScore + tierScore + 20f; // base 20
    }
}
