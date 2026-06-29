using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;

namespace WorldFaith.Server.Services.NPC;

/// <summary>
/// Sinh NPC theo tier when civilization was tạo.
/// Tier 1-2 sinh ngầm (not có document riêng, đại diện bởi population).
/// Tier 3-5 có NpcDocument riêng with tên and stats cụ thể.
/// </summary>
public interface INpcSpawnService
{
    Task SpawnForCivilizationAsync(string worldId, CivilizationDocument civ);
    Task SpawnAdventureGuildAsync(string worldId, string civId);
    Task<NpcDocument?> PromoteToChampionAsync(string worldId, string npcId, string godId);
}

public class NpcSpawnService : INpcSpawnService
{
    private readonly INpcRepository _npcRepo;
    private readonly IOrganizationRepository _orgRepo;
    private readonly ILogger<NpcSpawnService> _logger;
    private readonly Random _rng = new();

    // Sample NPC names
    private static readonly string[] NpcFirstNames =
    {
        "Aldric", "Brynn", "Caelan", "Dara", "Erin", "Fionn", "Gwen", "Hadwin",
        "Isla", "Joran", "Kira", "Lorcan", "Mira", "Nolan", "Orla", "Phelan",
        "Quinn", "Rowan", "Sable", "Tavish", "Una", "Vance", "Wren", "Xander",
        "Yael", "Zara", "Ardan", "Brea", "Cullen", "Dwyn"
    };

    private static readonly string[] NoblesFirstNames =
    {
        "Lord Aldric", "Lady Brynn", "Duke Caelan", "Duchess Dara", "Baron Erin",
        "Baroness Fionn", "Count Gwen", "Countess Hadwin", "Viscount Isla", "Lady Mira"
    };

    private static readonly string[] RoyalTitles =
    {
        "King", "Queen", "Crown Prince", "Royal Advisor", "Grand Vizier"
    };

    public NpcSpawnService(
        INpcRepository npcRepo,
        IOrganizationRepository orgRepo,
        ILogger<NpcSpawnService> logger)
    {
        _npcRepo = npcRepo;
        _orgRepo = orgRepo;
        _logger = logger;
    }

    public async Task SpawnForCivilizationAsync(string worldId, CivilizationDocument civ)
    {
        var tasks = new List<Task>
        {
            SpawnRoyaltyAsync(worldId, civ),
            SpawnNobleHousesAsync(worldId, civ),
            SpawnServantsAsync(worldId, civ),
            SpawnAdventureGuildAsync(worldId, civ.Id),
        };
        await Task.WhenAll(tasks);
        _logger.LogInformation("Spawned NPC tier 2-5 for civ {CivName}", civ.Name);
    }

    // ─── Royalty ──────────────────────────────────────────

    private async Task SpawnRoyaltyAsync(string worldId, CivilizationDocument civ)
    {
        // Create Royal Court Organization
        var court = new OrganizationDocument
        {
            WorldId = worldId,
            CivilizationId = civ.Id,
            Name = $"Royal Court of {civ.Name}",
            Type = OrganizationType.RoyalCourt,
            Power = 80f,
            Wealth = 70f,
            Loyalty = 80f
        };
        await _orgRepo.CreateAsync(court);

        // Create King/Queen
        var king = new NpcDocument
        {
            WorldId = worldId,
            CivilizationId = civ.Id,
            OrganizationId = court.Id,
            Name = $"{RoyalTitles[0]} {PickName(NpcFirstNames)}",
            Tier = NpcTier.Royalty,
            Personality = PickPersonality(),
            Loyalty = 80f,
            Ambition = _rng.Next(20, 60),
            Piety = _rng.Next(30, 80),
            Wealth = 90f
        };
        await _npcRepo.CreateAsync(king);

        court.LeaderNpcId = king.Id;
        court.Members.Add(new OrgMember { NpcId = king.Id, Role = OrgRole.Leader, RoleTitle = "King" });

        // 4 Royal Advisors (Chancellor, General, High Priest, Spymaster)
        var advisorTitles = new[] { "Chancellor", "General", "High Priest", "Spymaster" };
        foreach (var title in advisorTitles)
        {
            var advisor = new NpcDocument
            {
                WorldId = worldId,
                CivilizationId = civ.Id,
                OrganizationId = court.Id,
                Name = $"{title} {PickName(NpcFirstNames)}",
                Tier = NpcTier.Royalty,
                Personality = title == "Spymaster" ? NpcPersonality.Corrupt : PickPersonality(),
                Loyalty = _rng.Next(50, 90),
                Ambition = _rng.Next(30, 80),
                Piety = title == "High Priest" ? _rng.Next(70, 100) : _rng.Next(20, 60),
                Wealth = 70f
            };
            advisor.Relationships.Add(new NpcRelationship
            {
                NpcId = king.Id,
                Type = RelationshipType.Liege,
                Strength = advisor.Loyalty
            });
            await _npcRepo.CreateAsync(advisor);
            court.Members.Add(new OrgMember
            {
                NpcId = advisor.Id,
                Role = OrgRole.Senior,
                RoleTitle = title
            });
        }
        await _orgRepo.UpdateAsync(court);
    }

    // ─── Noble Houses ─────────────────────────────────────

    private async Task SpawnNobleHousesAsync(string worldId, CivilizationDocument civ)
    {
        int houseCount = _rng.Next(3, 7);
        for (int h = 0; h < houseCount; h++)
        {
            var houseName = $"House {PickName(NpcFirstNames)}";
            var house = new OrganizationDocument
            {
                WorldId = worldId,
                CivilizationId = civ.Id,
                Name = houseName,
                Type = OrganizationType.NobleHouse,
                Power = _rng.Next(20, 70),
                Wealth = _rng.Next(30, 80),
                Loyalty = _rng.Next(40, 90)
            };
            await _orgRepo.CreateAsync(house);

            // Head of House
            var head = new NpcDocument
            {
                WorldId = worldId,
                CivilizationId = civ.Id,
                OrganizationId = house.Id,
                Name = $"{PickName(NoblesFirstNames)} of {houseName}",
                Tier = NpcTier.Noble,
                Personality = PickPersonality(),
                Loyalty = house.Loyalty,
                Ambition = _rng.Next(20, 90),
                Piety = _rng.Next(20, 70),
                Wealth = house.Wealth
            };
            await _npcRepo.CreateAsync(head);

            house.LeaderNpcId = head.Id;
            house.Members.Add(new OrgMember { NpcId = head.Id, Role = OrgRole.Leader, RoleTitle = "Head of House" });

            // 2-4 Noble family members
            int memberCount = _rng.Next(2, 5);
            for (int m = 0; m < memberCount; m++)
            {
                var member = new NpcDocument
                {
                    WorldId = worldId,
                    CivilizationId = civ.Id,
                    OrganizationId = house.Id,
                    Name = PickName(NpcFirstNames),
                    Tier = NpcTier.Noble,
                    Personality = PickPersonality(),
                    Loyalty = _rng.Next(40, 80),
                    Ambition = _rng.Next(10, 70),
                    Piety = _rng.Next(20, 60),
                    Wealth = _rng.Next(40, 70)
                };
                member.Relationships.Add(new NpcRelationship
                {
                    NpcId = head.Id,
                    Type = RelationshipType.Liege,
                    Strength = 60f
                });
                await _npcRepo.CreateAsync(member);
                house.Members.Add(new OrgMember { NpcId = member.Id, Role = OrgRole.Member });
            }

            // 3-6 Servants per Noble House
            int servantCount = _rng.Next(3, 7);
            for (int s = 0; s < servantCount; s++)
            {
                var servant = new NpcDocument
                {
                    WorldId = worldId,
                    CivilizationId = civ.Id,
                    OrganizationId = house.Id,
                    Name = PickName(NpcFirstNames),
                    Tier = NpcTier.Servant,
                    Personality = _rng.NextDouble() < 0.3
                        ? NpcPersonality.Ambitious : NpcPersonality.Loyal,
                    Loyalty = _rng.Next(30, 90),
                    Ambition = _rng.Next(5, 60),
                    Piety = _rng.Next(20, 60),
                    Wealth = _rng.Next(10, 30)
                };
                await _npcRepo.CreateAsync(servant);
                house.Members.Add(new OrgMember { NpcId = servant.Id, Role = OrgRole.Member, RoleTitle = "Servant" });
            }

            await _orgRepo.UpdateAsync(house);
        }
    }

    // ─── Servants ─────────────────────────────────────────

    private async Task SpawnServantsAsync(string worldId, CivilizationDocument civ)
    {
        // Servant trong Religious Institution
        var religiousInst = new OrganizationDocument
        {
            WorldId = worldId,
            CivilizationId = civ.Id,
            Name = $"Temple of {civ.Name}",
            Type = OrganizationType.ReligiousInstitution,
            Power = 30f,
            Wealth = 40f,
            Loyalty = 70f
        };
        await _orgRepo.CreateAsync(religiousInst);

        // High Priest
        var highPriest = new NpcDocument
        {
            WorldId = worldId,
            CivilizationId = civ.Id,
            OrganizationId = religiousInst.Id,
            Name = $"High Priest {PickName(NpcFirstNames)}",
            Tier = NpcTier.Servant,
            Personality = NpcPersonality.Pious,
            Loyalty = 70f,
            Ambition = _rng.Next(10, 50),
            Piety = _rng.Next(70, 100),
            Wealth = 40f
        };
        await _npcRepo.CreateAsync(highPriest);
        religiousInst.LeaderNpcId = highPriest.Id;
        religiousInst.Members.Add(new OrgMember
        {
            NpcId = highPriest.Id,
            Role = OrgRole.Leader,
            RoleTitle = "High Priest"
        });
        await _orgRepo.UpdateAsync(religiousInst);
    }

    // ─── Adventure Guild ──────────────────────────────────

    public async Task SpawnAdventureGuildAsync(string worldId, string civId)
    {
        // Check if Guild already exists for this world
        var existing = await _orgRepo.GetByTypeAsync(worldId, OrganizationType.AdventureGuild);
        OrganizationDocument guild;

        if (!existing.Any())
        {
            guild = new OrganizationDocument
            {
                WorldId = worldId,
                CivilizationId = civId,
                Name = "Wanderers' Guild",
                Type = OrganizationType.AdventureGuild,
                Power = 40f,
                Wealth = 50f,
                Loyalty = 50f  // not trung thành with kingdom cụ thể nào
            };
            await _orgRepo.CreateAsync(guild);
        }
        else
        {
            guild = existing.First();
        }

        // Guild Master
        var master = new NpcDocument
        {
            WorldId = worldId,
            CivilizationId = civId,
            OrganizationId = guild.Id,
            Name = $"Guildmaster {PickName(NpcFirstNames)}",
            Tier = NpcTier.Adventurer,
            Personality = NpcPersonality.Idealistic,
            Loyalty = 60f,
            Ambition = _rng.Next(40, 80),
            Piety = _rng.Next(30, 70),
            Wealth = 50f
        };
        await _npcRepo.CreateAsync(master);

        guild.LeaderNpcId = master.Id;
        guild.Members.Add(new OrgMember { NpcId = master.Id, Role = OrgRole.Leader, RoleTitle = "Guildmaster" });

        // 4-8 Adventurers
        int advCount = _rng.Next(4, 9);
        for (int i = 0; i < advCount; i++)
        {
            var adv = new NpcDocument
            {
                WorldId = worldId,
                CivilizationId = civId,
                OrganizationId = guild.Id,
                Name = PickName(NpcFirstNames),
                Tier = NpcTier.Adventurer,
                Personality = PickPersonality(),
                Loyalty = _rng.Next(40, 80),
                Ambition = _rng.Next(30, 90),
                Piety = _rng.Next(20, 70),
                Wealth = _rng.Next(20, 50)
            };
            await _npcRepo.CreateAsync(adv);
            guild.Members.Add(new OrgMember { NpcId = adv.Id, Role = OrgRole.Member });
        }
        await _orgRepo.UpdateAsync(guild);
    }

    // ─── Champion Promotion ───────────────────────────────

    public async Task<NpcDocument?> PromoteToChampionAsync(string worldId, string npcId, string godId)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null || npc.Tier != NpcTier.Adventurer) return null;
        if (npc.GodTrustLevel < 70f) return null;

        // Upgrade Adventurer → HumanHero (stored as Champion)
        npc.IsChampion = true;
        npc.GodInfluenceId = godId;
        npc.Name = $"Champion {npc.Name.Split(' ').LastOrDefault() ?? npc.Name}";
        // Champion path set after further evolution
        await _npcRepo.UpdateAsync(npc);

        _logger.LogInformation("Adventurer {Name} promoted to Champion by God {GodId}", npc.Name, godId);
        return npc;
    }

    // ─── Helpers ──────────────────────────────────────────

    private string PickName(string[] pool) => pool[_rng.Next(pool.Length)];

    private NpcPersonality PickPersonality() => (NpcPersonality)_rng.Next(
        Enum.GetValues<NpcPersonality>().Length);
}
