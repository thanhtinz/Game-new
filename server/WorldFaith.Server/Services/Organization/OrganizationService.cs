using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Shared.Contracts;

namespace WorldFaith.Server.Services.Organization;

public interface IOrganizationService
{
    Task<List<DeltaEvent>> TickAsync(string worldId, long tick);
    Task<OrganizationDocument?> CreateUndergroundOrgAsync(string worldId, string civId, string godId);
}

public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly INpcRepository _npcRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IGodRepository _godRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IBalanceConfigService _balance;
    private readonly ILogger<OrganizationService> _logger;
    private readonly Random _rng = new();

    public OrganizationService(
        IOrganizationRepository orgRepo,
        INpcRepository npcRepo,
        ICivilizationRepository civRepo,
        IGodRepository godRepo,
        IReligionRepository religionRepo,
        IBalanceConfigService balance,
        ILogger<OrganizationService> logger)
    {
        _orgRepo = orgRepo;
        _npcRepo = npcRepo;
        _civRepo = civRepo;
        _godRepo = godRepo;
        _religionRepo = religionRepo;
        _balance = balance;
        _logger = logger;
    }

    public async Task<List<DeltaEvent>> TickAsync(string worldId, long tick)
    {
        var deltas = new List<DeltaEvent>();
        var orgs = await _orgRepo.GetByWorldAsync(worldId);

        foreach (var org in orgs)
        {
            switch (org.Type)
            {
                case OrganizationType.NobleHouse:
                    deltas.AddRange(await TickNobleHouseAsync(worldId, org, tick));
                    break;
                case OrganizationType.RoyalCourt:
                    deltas.AddRange(await TickRoyalCourtAsync(worldId, org, tick));
                    break;
                case OrganizationType.AdventureGuild:
                    deltas.AddRange(await TickAdventureGuildAsync(worldId, org, tick));
                    break;
                case OrganizationType.ReligiousInstitution:
                    deltas.AddRange(await TickReligiousInstitutionAsync(worldId, org, tick));
                    break;
                case OrganizationType.UndergroundOrg:
                    deltas.AddRange(await TickUndergroundOrgAsync(worldId, org, tick));
                    break;
            }
        }
        return deltas;
    }

    // ─── Noble House ──────────────────────────────────────

    private async Task<List<DeltaEvent>> TickNobleHouseAsync(
        string worldId, OrganizationDocument org, long tick)
    {
        var deltas = new List<DeltaEvent>();
        if (org.LeaderNpcId == null) return deltas;

        var head = await _npcRepo.GetByIdAsync(org.LeaderNpcId);
        if (head == null || head.State != NpcState.Alive) return deltas;

        // 1. Loyalty decay nếu King approval thấp
        var civ = await _civRepo.GetByCivilizationByIdAsync(org.CivilizationId);
        if (civ != null && civ.AiMemory.GodTrustLevel < 30f)
        {
            head.Loyalty -= 2f;
            head.Ambition += 1f;
            await _npcRepo.UpdateAsync(head);
        }

        // 2. Rival Noble House — nếu hai nhà có Power tương đương
        var otherHouses = (await _orgRepo.GetByCivilizationAsync(org.CivilizationId))
            .Where(o => o.Type == OrganizationType.NobleHouse && o.Id != org.Id).ToList();

        foreach (var rival in otherHouses)
        {
            if (MathF.Abs(org.Power - rival.Power) < 10f && _rng.NextDouble() < 0.05)
            {
                // Tạo rivalry
                if (org.RivalOrgId == null)
                {
                    org.RivalOrgId = rival.Id;
                    rival.RivalOrgId = org.Id;
                    await _orgRepo.UpdateAsync(org);
                    await _orgRepo.UpdateAsync(rival);

                    deltas.Add(new DeltaEvent
                    {
                        Type = WorldEventType.DivineConflict,
                        TargetId = org.CivilizationId,
                        Description = $"{org.Name} và {rival.Name} trở thành đối thủ tranh giành ảnh hưởng!"
                    });
                    _logger.LogInformation("Noble rivalry formed: {A} vs {B}", org.Name, rival.Name);
                }
            }
        }

        // 3. Noble House tries to convert Head to God's religion
        if (head.GodInfluenceId != null && head.GodTrustLevel > 60f)
        {
            if (org.GodInfluenceId == null)
            {
                org.GodInfluenceId = head.GodInfluenceId;
                await _orgRepo.UpdateAsync(org);
                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.MiraclePerformed,
                    SourceGodId = head.GodInfluenceId,
                    TargetId = org.CivilizationId,
                    Description = $"{org.Name} trở thành đồng minh của Thần Linh!"
                });
            }
        }

        return deltas;
    }

    // ─── Royal Court ──────────────────────────────────────

    private async Task<List<DeltaEvent>> TickRoyalCourtAsync(
        string worldId, OrganizationDocument org, long tick)
    {
        var deltas = new List<DeltaEvent>();
        if (org.LeaderNpcId == null) return deltas;

        var king = await _npcRepo.GetByIdAsync(org.LeaderNpcId);
        if (king == null || king.State != NpcState.Alive) return deltas;

        // Advisor conflict: 2 advisors influenced by rival gods
        var members = new List<NpcDocument>();
        foreach (var m in org.Members.Where(m => m.Role == OrgRole.Senior))
        {
            var npc = await _npcRepo.GetByIdAsync(m.NpcId);
            if (npc != null) members.Add(npc);
        }

        var advisorsByGod = members
            .Where(a => a.GodInfluenceId != null)
            .GroupBy(a => a.GodInfluenceId)
            .ToList();

        if (advisorsByGod.Count >= 2)
        {
            // Policy deadlock — civ weaker
            var civ = await _civRepo.GetByCivilizationByIdAsync(org.CivilizationId);
            if (civ != null && _rng.NextDouble() < 0.1)
            {
                civ.Economy -= 3f;
                civ.Military -= 3f;
                await _civRepo.UpdateAsync(civ);
                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.DivineConflict,
                    TargetId = org.CivilizationId,
                    Description = "Triều đình bất đồng vì các quan đại thần phục vụ thần khác nhau. Vương quốc suy yếu!"
                });
            }
        }

        // King adopts religion of most devout advisor
        var spymasterOrHighPriest = members
            .Where(a => a.Piety > 75f && a.GodInfluenceId != null)
            .OrderByDescending(a => a.Piety).FirstOrDefault();

        if (spymasterOrHighPriest != null && king.PersonalReligionId == null
            && _rng.NextDouble() < 0.03)
        {
            var religion = await _religionRepo.GetByGodAsync(
                worldId, spymasterOrHighPriest.GodInfluenceId!);
            if (religion != null)
            {
                king.PersonalReligionId = religion.Id;
                king.GodInfluenceId = spymasterOrHighPriest.GodInfluenceId;
                king.GodTrustLevel = 50f;
                await _npcRepo.UpdateAsync(king);

                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.MiraclePerformed,
                    SourceGodId = spymasterOrHighPriest.GodInfluenceId,
                    TargetId = org.CivilizationId,
                    Description = $"Vua {king.Name} chính thức theo tôn giáo {religion.Name}!"
                });
            }
        }

        return deltas;
    }

    // ─── Adventure Guild ──────────────────────────────────

    private async Task<List<DeltaEvent>> TickAdventureGuildAsync(
        string worldId, OrganizationDocument org, long tick)
    {
        var deltas = new List<DeltaEvent>();
        var members = new List<NpcDocument>();
        foreach (var m in org.Members)
        {
            var npc = await _npcRepo.GetByIdAsync(m.NpcId);
            if (npc != null && npc.State == NpcState.Alive) members.Add(npc);
        }

        if (!members.Any()) return deltas;

        // Adventurers spread religion when they travel (every 20 ticks)
        if (tick % 20 == 0 && org.OfficialReligionId != null)
        {
            var traveler = members
                .Where(m => m.Tier == NpcTier.Adventurer && !m.IsChampion)
                .OrderBy(_ => _rng.Next()).FirstOrDefault();

            if (traveler != null)
            {
                // Spread religion to a random civ
                var allCivs = await _civRepo.GetByWorldAsync(worldId);
                var targetCiv = allCivs
                    .Where(c => c.Id != org.CivilizationId)
                    .OrderBy(_ => _rng.Next()).FirstOrDefault();

                if (targetCiv != null)
                {
                    deltas.Add(new DeltaEvent
                    {
                        Type = WorldEventType.MiraclePerformed,
                        SourceGodId = org.GodInfluenceId,
                        TargetId = targetCiv.Id,
                        Description = $"{traveler.Name} mang đức tin đến {targetCiv.Name} trong chuyến hành trình!"
                    });
                }
            }
        }

        // Guild quest: Adventurer encounters Evolution Entity
        if (_rng.NextDouble() < 0.08)
        {
            var questers = members
                .Where(m => m.Tier == NpcTier.Adventurer && !m.IsChampion)
                .Take(3).ToList();

            if (questers.Any())
            {
                bool survived = _rng.NextDouble() < 0.7;
                if (survived)
                {
                    var hero = questers.OrderBy(_ => _rng.Next()).First();
                    hero.EvolutionPoints += 30;
                    await _npcRepo.UpdateAsync(hero);
                    deltas.Add(new DeltaEvent
                    {
                        Type = WorldEventType.EvolutionOccurred,
                        TargetId = org.CivilizationId,
                        Description = $"{hero.Name} chiến thắng và tích lũy sức mạnh thần thánh! ({hero.EvolutionPoints} pts)"
                    });

                    // Champion promotion check
                    if (hero.EvolutionPoints >= 150 && hero.IsChampion == false
                        && hero.GodInfluenceId != null && hero.GodTrustLevel >= 70f)
                    {
                        hero.IsChampion = true;
                        await _npcRepo.UpdateAsync(hero);
                        deltas.Add(new DeltaEvent
                        {
                            Type = WorldEventType.EvolutionOccurred,
                            SourceGodId = hero.GodInfluenceId,
                            TargetId = org.CivilizationId,
                            Description = $"🌟 {hero.Name} đã trở thành Champion của Thần Linh!"
                        });
                    }
                }
                else
                {
                    var fallen = questers.First();
                    fallen.State = NpcState.Dead;
                    await _npcRepo.UpdateAsync(fallen);
                    org.Members.RemoveAll(m => m.NpcId == fallen.Id);
                    await _orgRepo.UpdateAsync(org);
                    deltas.Add(new DeltaEvent
                    {
                        Type = WorldEventType.GodFaded,
                        TargetId = org.CivilizationId,
                        Description = $"{fallen.Name} hy sinh trong quest. Guild mất đi một thành viên tài năng."
                    });
                }
            }
        }

        return deltas;
    }

    // ─── Religious Institution ────────────────────────────

    private async Task<List<DeltaEvent>> TickReligiousInstitutionAsync(
        string worldId, OrganizationDocument org, long tick)
    {
        var deltas = new List<DeltaEvent>();

        // Corruption scandal in religious institution
        if (org.LeaderNpcId != null)
        {
            var highPriest = await _npcRepo.GetByIdAsync(org.LeaderNpcId);
            if (highPriest != null && highPriest.Loyalty < 30f && _rng.NextDouble() < 0.04)
            {
                org.Power -= 10f;
                org.Loyalty -= 15f;
                highPriest.Piety -= 20f;
                await _npcRepo.UpdateAsync(highPriest);
                await _orgRepo.UpdateAsync(org);
                deltas.Add(new DeltaEvent
                {
                    Type = WorldEventType.DivineConflict,
                    TargetId = org.CivilizationId,
                    Description = $"Giáo chủ {highPriest.Name} bị phát hiện tham nhũng! Uy tín tôn giáo sụt giảm nghiêm trọng."
                });
            }
        }

        // Heresy Trial — religious institution suppresses rival cult
        if (_rng.NextDouble() < 0.03 && tick % 80 == 0)
        {
            deltas.Add(new DeltaEvent
            {
                Type = WorldEventType.DivineConflict,
                TargetId = org.CivilizationId,
                Description = "Giáo hội mở phiên tòa dị giáo. Tín đồ chính thống tăng devotion, dị giáo bị đàn áp."
            });
            var civ = await _civRepo.GetByCivilizationByIdAsync(org.CivilizationId);
            if (civ != null)
            {
                civ.AiMemory.GodTrustLevel += 5f;
                await _civRepo.UpdateAsync(civ);
            }
        }

        return deltas;
    }

    // ─── Underground Org ──────────────────────────────────

    private async Task<List<DeltaEvent>> TickUndergroundOrgAsync(
        string worldId, OrganizationDocument org, long tick)
    {
        var deltas = new List<DeltaEvent>();

        // Tăng Heat mỗi tick (risk of exposure)
        org.HeatLevel += 0.5f;

        // Hoạt động: tạo Fear cho dark god
        if (org.GodInfluenceId != null)
        {
            var god = await _godRepo.GetByIdAsync(org.GodInfluenceId);
            if (god != null && (god.Archetype is Shared.Enums.GodArchetype.Darkness
                or Shared.Enums.GodArchetype.Death or Shared.Enums.GodArchetype.Chaos))
            {
                float fearGain = org.Power * 0.02f;
                await _godRepo.UpdateFaithAsync(
                    god.Id, god.Faith + fearGain * 0.5f, god.Trust, god.Fear + fearGain, god.FollowerCount);
            }
        }

        // Exposure
        if (org.HeatLevel >= 100f || _rng.NextDouble() < 0.02)
        {
            org.HeatLevel = 0f;
            org.IsHidden = false;
            org.Power -= 30f;
            await _orgRepo.UpdateAsync(org);
            deltas.Add(new DeltaEvent
            {
                Type = WorldEventType.DivineConflict,
                TargetId = org.CivilizationId,
                Description = $"Tổ chức ngầm '{org.Name}' bị phát hiện! Vương quốc tiến hành trấn áp."
            });
            return deltas;
        }

        // Criminal operations — economy drain from target civ
        if (_rng.NextDouble() < 0.06)
        {
            var civs = await _civRepo.GetByWorldAsync(worldId);
            var target = civs.OrderBy(_ => _rng.Next()).FirstOrDefault();
            if (target != null)
            {
                target.Economy -= org.Power * 0.05f;
                await _civRepo.UpdateAsync(target);
            }
        }

        await _orgRepo.UpdateAsync(org);
        return deltas;
    }

    // ─── Create Underground Org ───────────────────────────

    public async Task<OrganizationDocument?> CreateUndergroundOrgAsync(
        string worldId, string civId, string godId)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null) return null;

        // Chỉ dark gods có thể tạo underground org
        bool isDark = god.Archetype is Shared.Enums.GodArchetype.Darkness
            or Shared.Enums.GodArchetype.Death or Shared.Enums.GodArchetype.Chaos;
        if (!isDark) return null;

        var org = new OrganizationDocument
        {
            WorldId = worldId,
            CivilizationId = civId,
            Name = GenerateUndergroundName(),
            Type = OrganizationType.UndergroundOrg,
            IsHidden = true,
            Power = 20f,
            Wealth = 30f,
            Loyalty = 10f,
            GodInfluenceId = godId,
            HeatLevel = 0f
        };
        await _orgRepo.CreateAsync(org);
        _logger.LogInformation("Dark god {GodId} created underground org '{Name}'", godId, org.Name);
        return org;
    }

    private string GenerateUndergroundName()
    {
        var prefixes = new[] { "Shadow", "Crimson", "Silent", "Midnight", "Iron", "Veiled" };
        var suffixes = new[] { "Brotherhood", "Circle", "Order", "Covenant", "Syndicate", "Cabal" };
        return $"{prefixes[_rng.Next(prefixes.Length)]} {suffixes[_rng.Next(suffixes.Length)]}";
    }
}
