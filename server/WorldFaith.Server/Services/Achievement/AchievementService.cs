using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.NPC;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Achievement;

public interface IAchievementService
{
    Task<NpcAchievement> EarnAchievementAsync(string npcId, string achievementKey, long tick);
    Task AwakentTalentAsync(string npcId, string talentName, string triggeredByEvent);
    Task<float> CalculateDivineAttentionAsync(string npcId);
    Task<List<GodNoteEntry>> GetGodNoteAsync(string worldId, string godId, GodNoteTab? tab = null, int limit = 20);
    Task<bool> ApplyDivineActionAsync(string npcId, string godId, DivineAction action, long tick);
    Task<ChurchRank> EvaluateChurchPromotionAsync(string npcId, string religionId);
    Task TickAchievementSystemAsync(string worldId, long tick);
}

public class AchievementService : IAchievementService
{
    private readonly INpcRepository _npcRepo;
    private readonly IGodRepository _godRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IDoctrineIntegrityService _doctrineIntegrity;
    private readonly ILogger<AchievementService> _logger;
    private readonly Random _rng = new();

    // ─── Achievement Catalogue ────────────────────────────
    // GDD §3.1 — định nghĩa sẵn các achievements có thể earn
    private static readonly Dictionary<string, (string name, string desc, AchievementCategory cat, AchievementRarity rarity, int weight)> AchievementCatalogue = new()
    {
        // Common Life
        ["donated_food"]         = ("Generous Soul",     "Donated food to the temple",            AchievementCategory.CommonLife,      AchievementRarity.Common,    3),
        ["survived_famine"]      = ("Survival Strength", "Survived a famine",                        AchievementCategory.CommonLife,      AchievementRarity.Uncommon, 10),
        ["saved_family"]         = ("Family Pillar",  "Saved a family member from danger", AchievementCategory.CommonLife,      AchievementRarity.Uncommon, 12),
        ["rebuilt_home"]         = ("Reconstruction",          "Rebuilt home after disaster",               AchievementCategory.CommonLife,      AchievementRarity.Common,    5),

        // Religious Devotion
        ["daily_prayer"]         = ("Steadfast Faith", "Prayed every day for 30 ticks",          AchievementCategory.ReligiousDevot,  AchievementRarity.Common,    6),
        ["maintained_shrine"]    = ("Shrine Keeper",     "Maintained and cared for the shrine",                 AchievementCategory.ReligiousDevot,  AchievementRarity.Uncommon, 10),
        ["converted_neighbors"]  = ("Faith Messenger",   "Converted at least 3 neighbors",            AchievementCategory.ReligiousDevot,  AchievementRarity.Uncommon, 15),
        ["completed_pilgrimage"] = ("Holy Pilgrimage",  "Completed a pilgrimage journey",             AchievementCategory.ReligiousDevot,  AchievementRarity.Rare,     25),
        ["survived_plague_prayer"]= ("Miracle Survival", "Survived a plague through faith",         AchievementCategory.ReligiousDevot,  AchievementRarity.Rare,     30),

        // Adventurer
        ["cleared_dungeon"]      = ("Conqueror of Darkness","Successfully cleared a dungeon",            AchievementCategory.Adventurer,      AchievementRarity.Rare,     20),
        ["slew_monster_leader"]  = ("Evil Slayer",        "Slew the monster leader",                 AchievementCategory.Adventurer,      AchievementRarity.Rare,     25),
        ["rescued_village"]      = ("Village Hero",     "Saved an entire village from attack",               AchievementCategory.Adventurer,      AchievementRarity.Epic,     40),
        ["found_relic"]          = ("Relic Discovery",   "Found a Relic in ancient ruins",            AchievementCategory.Adventurer,      AchievementRarity.Epic,     45),
        ["survived_divine_trial"]= ("Holy Witness",  "Survived a divine trial",            AchievementCategory.Adventurer,      AchievementRarity.Legendary,90),

        // Royal / Noble Service
        ["protected_royal"]      = ("Royal Guard",  "Protected a royal family member",                 AchievementCategory.RoyalService,    AchievementRarity.Epic,     50),
        ["built_temple"]         = ("Temple Founder","Funded temple construction",                    AchievementCategory.RoyalService,    AchievementRarity.Rare,     30),
        ["converted_noble_house"]= ("Faith Ambassador",    "Converted an entire noble house",              AchievementCategory.RoyalService,    AchievementRarity.Epic,     60),
        ["saved_royal_heir"]     = ("Saved the Crown Prince",       "Saved the royal heir",              AchievementCategory.RoyalService,    AchievementRarity.Epic,     70),

        // Miracle Exposure
        ["witnessed_revelation"] = ("Witnessed the Divine","Directly witnessed a Revelation miracle",     AchievementCategory.MiracleExposure, AchievementRarity.Rare,     25),
        ["healed_by_miracle"]    = ("Divinely Healed",    "Directly healed by divine power",           AchievementCategory.MiracleExposure, AchievementRarity.Uncommon, 15),
        ["survived_disaster"]    = ("Disaster Survivor", "Survived a Volcano or Earthquake miracle", AchievementCategory.MiracleExposure, AchievementRarity.Rare,     20),

        // Dark / Forbidden
        ["led_cult"]             = ("Cult Leader",  "Led an underground cult",               AchievementCategory.DarkForbidden,   AchievementRarity.Rare,     25),
        ["opened_cursed_relic"]  = ("Seal Breaker",       "Opened a cursed Relic",                  AchievementCategory.DarkForbidden,   AchievementRarity.Epic,     35),
        ["betrayed_temple"]      = ("Temple Betrayer",      "Betrayed the temple and religion",                AchievementCategory.DarkForbidden,   AchievementRarity.Uncommon, 12),
        ["survived_curse"]       = ("Curse Bearer",   "Survived a Curse miracle",                  AchievementCategory.DarkForbidden,   AchievementRarity.Rare,     20),
    };

    // ─── Talent Catalogue ─────────────────────────────────
    private static readonly Dictionary<string, (string name, NpcTalentGroup group, int rarityScore)> TalentCatalogue = new()
    {
        // Spiritual
        ["pure_soul"]          = ("Pure Soul", NpcTalentGroup.Spiritual,    90),
        ["strong_faith"]       = ("Strong Faith",   NpcTalentGroup.Spiritual,    70),
        ["divine_listener"]    = ("Divine Listener",NpcTalentGroup.Spiritual,    80),
        ["dream_sensitive"]    = ("Dream Sensitive",NpcTalentGroup.Spiritual,   85),
        ["miracle_vessel"]     = ("Miracle Vessel",  NpcTalentGroup.Spiritual,    95),
        // Combat
        ["sword_genius"]       = ("Sword Genius",NpcTalentGroup.Combat,       75),
        ["warborn"]            = ("Warborn",NpcTalentGroup.Combat,       70),
        ["monster_slayer"]     = ("Monster Slayer",        NpcTalentGroup.Combat,       80),
        ["shield_of_weak"]     = ("Shield of the Weak",     NpcTalentGroup.Combat,       65),
        // Mental
        ["genius"]             = ("Genius",            NpcTalentGroup.Mental,       85),
        ["wise_speaker"]       = ("Wise Speaker",  NpcTalentGroup.Mental,       70),
        ["strategist"]         = ("Strategist",       NpcTalentGroup.Mental,       75),
        // Social
        ["beloved"]            = ("Beloved",         NpcTalentGroup.Social,       65),
        ["hidden_charisma"]    = ("Hidden Charisma",           NpcTalentGroup.Social,       70),
        ["silver_tongue"]      = ("Silver Tongue",         NpcTalentGroup.Social,       72),
        // Dark / Forbidden
        ["cursed_blood"]       = ("Cursed Blood",  NpcTalentGroup.DarkForbidden, 80),
        ["demon_vessel"]       = ("Demon Vessel",   NpcTalentGroup.DarkForbidden, 95),
        ["shadow_mind"]        = ("Shadow Mind",    NpcTalentGroup.DarkForbidden, 85),
        // Resistance
        ["corruption_resistant"]= ("Corruption Resistant",      NpcTalentGroup.Resistance,   75),
        ["unshaken_will"]      = ("Unshaken Will",     NpcTalentGroup.Resistance,   80),
        ["pain_endurance"]     = ("Pain Endurance",   NpcTalentGroup.Resistance,   65),
    };

    public AchievementService(
        INpcRepository npcRepo,
        IGodRepository godRepo,
        IReligionRepository religionRepo,
        ICivilizationRepository civRepo,
        IDoctrineIntegrityService doctrineIntegrity,
        ILogger<AchievementService> logger)
    {
        _npcRepo = npcRepo;
        _godRepo = godRepo;
        _religionRepo = religionRepo;
        _civRepo = civRepo;
        _doctrineIntegrity = doctrineIntegrity;
        _logger = logger;
    }

    // ─── Earn Achievement ─────────────────────────────────

    public async Task<NpcAchievement> EarnAchievementAsync(string npcId, string achievementKey, long tick)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null) throw new InvalidOperationException($"NPC {npcId} not found");

        // Không for earn trùng
        if (npc.DivineProfile.Achievements.Any(a => a.Name == AchievementCatalogue[achievementKey].name))
        {
            return npc.DivineProfile.Achievements.First(a => a.Name == AchievementCatalogue[achievementKey].name);
        }

        if (!AchievementCatalogue.TryGetValue(achievementKey, out var def))
            throw new ArgumentException($"Unknown achievement: {achievementKey}");

        var achievement = new NpcAchievement
        {
            Name = def.name,
            Description = def.desc,
            Category = def.cat,
            Rarity = def.rarity,
            GodNoteWeight = def.weight,
            EarnedAtTick = tick
        };

        npc.DivineProfile.Achievements.Add(achievement);
        npc.DivineProfile.AchievementValue += def.weight;

        // Update DivineAttentionScore
        await RecalcDivineAttentionAsync(npc);
        await _npcRepo.UpdateAsync(npc);

        // Check saint/prophet/champion candidates sau when earn
        await UpdateCandidacyAsync(npc);
        await _npcRepo.UpdateAsync(npc);

        _logger.LogInformation("NPC {Name} earned [{Rarity}] achievement: {Achievement} (+{Weight} God Note)",
            npc.Name, def.rarity, def.name, def.weight);

        return achievement;
    }

    // ─── Awaken Talent ────────────────────────────────────

    public async Task AwakentTalentAsync(string npcId, string talentName, string triggeredByEvent)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null) return;

        // Resolve to the catalogue's canonical display name so storage and lookups match
        // (the catalogue key is snake_case but talents are stored under the display name).
        var inCatalogue = TalentCatalogue.TryGetValue(talentName, out var def);
        var canonicalName = inCatalogue ? def.name : talentName;

        // Talent has awakened chưa?
        if (npc.DivineProfile.Talents.Any(t => t.Name == canonicalName && t.IsAwakened)) return;

        var existing = npc.DivineProfile.Talents.FirstOrDefault(t => t.Name == canonicalName);
        if (existing != null)
        {
            existing.IsAwakened = true;
            existing.AwakenedByEvent = triggeredByEvent;
        }
        else if (inCatalogue)
        {
            npc.DivineProfile.Talents.Add(new NpcTalent
            {
                Name = def.name,
                Group = def.group,
                RarityScore = def.rarityScore,
                IsAwakened = true,
                AwakenedByEvent = triggeredByEvent
            });
            npc.DivineProfile.TalentRarity += def.rarityScore * 0.5f;
        }

        await RecalcDivineAttentionAsync(npc);
        await UpdateCandidacyAsync(npc);
        await _npcRepo.UpdateAsync(npc);

        _logger.LogInformation("NPC {Name} talent awakened: {Talent} (via {Event})", npc.Name, talentName, triggeredByEvent);
    }

    // ─── Divine Attention Score ───────────────────────────

    public async Task<float> CalculateDivineAttentionAsync(string npcId)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null) return 0f;
        await RecalcDivineAttentionAsync(npc);
        return npc.DivineProfile.DivineAttentionScore;
    }

    private async Task RecalcDivineAttentionAsync(NpcDocument npc)
    {
        // FaithLevel from devotion
        npc.DivineProfile.FaithLevel = npc.DevotionLevel * 100f;

        // TalentRarity = sum of awakened talent rarityScores * 0.5
        npc.DivineProfile.TalentRarity = npc.DivineProfile.Talents
            .Where(t => t.IsAwakened)
            .Sum(t => t.RarityScore * 0.5f);

        // Reputation from tier
        npc.DivineProfile.Reputation = npc.Tier switch
        {
            NpcTier.Royalty    => 50f,
            NpcTier.Noble      => 30f,
            NpcTier.Adventurer => 15f,
            NpcTier.Servant    => 5f,
            _                  => 0f
        };

        // MiracleExposure
        npc.DivineProfile.MiracleExposure = npc.DreamsReceived * 5f
            + npc.DivineProfile.Achievements
                .Count(a => a.Category == AchievementCategory.MiracleExposure) * 10f;

        // CorruptionRisk — derived from dark achievements, traits, and dark divine actions
        npc.DivineProfile.CorruptionRisk =
            npc.DivineProfile.Achievements
                .Count(a => a.Category == AchievementCategory.DarkForbidden) * 8f
            + (npc.Ambition > 70 ? 15f : 0f)
            + (npc.Loyalty < 30 ? 20f : 0f)
            + npc.DivineProfile.ReceivedDivineActions
                .Count(a => a == nameof(DivineAction.Corrupt)) * 25f;
    }

    private async Task UpdateCandidacyAsync(NpcDocument npc)
    {
        var profile = npc.DivineProfile;
        float score = profile.DivineAttentionScore;
        bool hasSpiritualTalent = profile.Talents.Any(t => t.Group == NpcTalentGroup.Spiritual && t.IsAwakened);
        bool hasCombatTalent    = profile.Talents.Any(t => t.Group == NpcTalentGroup.Combat && t.IsAwakened);
        bool hasDarkTalent      = profile.Talents.Any(t => t.Group == NpcTalentGroup.DarkForbidden && t.IsAwakened);
        bool hasMajorAchiev     = profile.Achievements.Any(a => a.Rarity >= AchievementRarity.Rare);
        float faithLevel        = npc.DevotionLevel;

        // Saint/Saintess: faith cao + spiritual talent + major achievement + low corruption
        profile.IsSaintCandidate = faithLevel > 0.85f
            && hasSpiritualTalent && hasMajorAchiev
            && profile.CorruptionRisk < 15f;

        // Prophet: Dream Sensitive + high faith + miracle exposure
        bool isDreamSensitive = profile.Talents.Any(t => t.Name.Contains("Dream") && t.IsAwakened);
        profile.IsProphetCandidate = isDreamSensitive
            && profile.MiracleExposure > 20f
            && faithLevel > 0.7f;

        // Champion: combat talent + adventurer tier + achievements
        profile.IsChampionCandidate = hasCombatTalent
            && npc.Tier == NpcTier.Adventurer
            && profile.Achievements.Any(a => a.Category == AchievementCategory.Adventurer);

        // Dark path: dark talent hoặc dark achievements cao
        profile.IsDarkPathCandidate = hasDarkTalent
            || profile.Achievements.Count(a => a.Category == AchievementCategory.DarkForbidden) >= 2;
    }

    // ─── God Note ─────────────────────────────────────────

    public async Task<List<GodNoteEntry>> GetGodNoteAsync(
        string worldId, string godId, GodNoteTab? tab = null, int limit = 20)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null) return new();

        // Get tất cả NPC of world này (tier 2-5)
        var allNpcs = await _npcRepo.GetByWorldAsync(worldId);
        var relevantNpcs = allNpcs
            .Where(n => n.Tier >= NpcTier.Servant && n.State == NpcState.Alive)
            .Where(n => n.PersonalReligionId != null || n.GodInfluenceId == godId)
            .ToList();

        // Tính divine attention and sort
        var scored = new List<(NpcDocument npc, float score)>();
        foreach (var npc in relevantNpcs)
        {
            await RecalcDivineAttentionAsync(npc);
            scored.Add((npc, npc.DivineProfile.DivineAttentionScore));
        }

        var entries = new List<GodNoteEntry>();
        var candidates = scored.OrderByDescending(x => x.score).Take(limit * 3).ToList();

        foreach (var (npc, score) in candidates)
        {
            var profile = npc.DivineProfile;
            var entry = new GodNoteEntry
            {
                NpcId = npc.Id,
                Name = npc.Name,
                Race = npc.Tier >= NpcTier.Noble ? RaceType.Human : RaceType.Human, // simplified
                SocialClass = npc.Tier,
                FaithPercent = npc.DevotionLevel * 100f,
                TalentNames = profile.Talents.Where(t => t.IsAwakened).Select(t => t.Name).ToList(),
                AchievementNames = profile.Achievements.OrderByDescending(a => a.GodNoteWeight).Take(3).Select(a => a.Name).ToList(),
                DivineAttentionScore = score
            };

            // Determine tab
            entry.Tab = DetermineTab(npc, profile);

            // Potential label
            entry.Potential = BuildPotentialLabel(profile);

            // Risk
            entry.Risk = BuildRiskLabel(profile, npc);

            // Recommended actions
            entry.RecommendedActions = BuildRecommendedActions(profile, npc, godId);

            // Filter by tab if specified
            if (tab == null || entry.Tab == tab)
                entries.Add(entry);
        }

        return entries.OrderByDescending(e => e.DivineAttentionScore).Take(limit).ToList();
    }

    private static GodNoteTab DetermineTab(NpcDocument npc, NpcDivineProfile profile)
    {
        if (npc.GodInfluenceId != null && profile.IsDarkPathCandidate) return GodNoteTab.HiddenCultAssets;
        if (profile.CorruptionRisk > 30f || npc.Ambition > 75f)        return GodNoteTab.DangerousFollowers;
        if (npc.IsChampion || profile.IsChampionCandidate)              return GodNoteTab.Champions;
        if (profile.IsSaintCandidate)                                    return GodNoteTab.SaintCandidates;
        if (profile.IsProphetCandidate)                                  return GodNoteTab.ProphetCandidates;
        if (profile.ChurchRank >= ChurchRank.TempleHelper)               return GodNoteTab.PotentialPriests;
        if (profile.Talents.Any(t => t.IsAwakened))                      return GodNoteTab.RisingTalents;
        return GodNoteTab.TopFaithful;
    }

    private static string BuildPotentialLabel(NpcDivineProfile profile)
    {
        if (profile.IsSaintCandidate) return "Saint / Saintess Candidate";
        if (profile.IsProphetCandidate) return "Prophet Candidate";
        if (profile.IsChampionCandidate) return "Champion Candidate";
        if (profile.IsDarkPathCandidate) return "Dark Path / Fallen Candidate";
        if (profile.ChurchRank >= ChurchRank.Priest) return "Established Church Member";
        return "Faithful Follower";
    }

    private static string BuildRiskLabel(NpcDivineProfile profile, NpcDocument npc)
    {
        var risks = new List<string>();
        if (profile.CorruptionRisk > 40f) risks.Add("High corruption risk");
        if (npc.Ambition > 75f) risks.Add("Highly ambitious — political danger");
        if (profile.IsDarkPathCandidate) risks.Add("Dark path potential");
        if (npc.Loyalty < 30f) risks.Add("Low loyalty — betrayal risk");
        return risks.Any() ? string.Join(", ", risks) : "Low risk";
    }

    private static List<string> BuildRecommendedActions(NpcDivineProfile profile, NpcDocument npc, string godId)
    {
        var actions = new List<string>();
        if (profile.IsSaintCandidate)
        {
            actions.Add("Send Dream to inspire");
            actions.Add("Bless to protect and elevate");
            actions.Add("Promote through church");
        }
        else if (profile.IsProphetCandidate)
        {
            actions.Add("Send Dream with prophecy");
            actions.Add("Test to unlock prophet path");
        }
        else if (profile.IsChampionCandidate)
        {
            actions.Add("Mark as Chosen");
            actions.Add("Bless before dungeon mission");
        }
        else if (profile.CorruptionRisk > 30f || npc.Ambition > 70f)
        {
            actions.Add("Protect from rival manipulation");
            actions.Add("Punish if betrayal detected");
        }

        if (npc.GodInfluenceId != godId && npc.GodInfluenceId != null)
            actions.Add("Corrupt — NPC belongs to rival god");
        if (actions.Count == 0) actions.Add("Bless to increase devotion");
        return actions;
    }

    // ─── Divine Action ────────────────────────────────────

    public async Task<bool> ApplyDivineActionAsync(string npcId, string godId, DivineAction action, long tick)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        var god = await _godRepo.GetByIdAsync(godId);
        if (npc == null || god == null) return false;

        bool changed = false;

        switch (action)
        {
            case DivineAction.Bless:
                // Increase devotion, giảm corruption risk, có thể awaken talent
                npc.DevotionLevel = Math.Min(1f, npc.DevotionLevel + 0.1f);
                npc.DivineProfile.CorruptionRisk = Math.Max(0f, npc.DivineProfile.CorruptionRisk - 10f);
                npc.GodTrustLevel = Math.Min(100f, npc.GodTrustLevel + 10f);
                // 20% chance awaken a spiritual talent
                if (_rng.NextDouble() < 0.2f)
                    await AwakentTalentAsync(npcId, "strong_faith", "Bless from God");
                changed = true;
                break;

            case DivineAction.SendDream:
                npc.DreamsReceived++;
                npc.DivineProfile.MiracleExposure += 5f;
                npc.GodTrustLevel = Math.Min(100f, npc.GodTrustLevel + 5f);
                // Dream-sensitive NPCs react more
                bool isDreamSens = npc.DivineProfile.Talents.Any(t => t.Name.Contains("Dream") && t.IsAwakened);
                if (isDreamSens) npc.GodTrustLevel = Math.Min(100f, npc.GodTrustLevel + 10f);
                changed = true;
                break;

            case DivineAction.Test:
                // Divine trial — 70% unlock saint/prophet path or earn achievement
                if (_rng.NextDouble() < 0.7f)
                {
                    await EarnAchievementAsync(npcId, "survived_divine_trial", tick);
                    npc.DivineProfile.DestinyModifier += 20f;
                    // Passed trial = integrity gain
                    await _doctrineIntegrity.ApplyResistanceAsync(npcId, "Passed divine trial", tick);
                }
                else
                {
                    // Failed → moderate doctrine violation (doubt emerged)
                    npc.DivineProfile.CorruptionRisk += 10f;
                    await _doctrineIntegrity.ApplyViolationAsync(npcId, "", ViolationSeverity.ModerateViolation,
                        "Failed divine trial — doubt emerged", false, null, tick);
                }
                changed = true;
                break;

            case DivineAction.MarkAsChosen:
                npc.DivineProfile.ChosenByGodId = godId;
                npc.DivineProfile.DestinyModifier += 30f;
                npc.DivineProfile.Reputation += 20f;
                // Rival gods will notice
                changed = true;
                break;

            case DivineAction.Protect:
                // Reduce accident/assassination risk → tracked via CorruptionRisk proxy
                npc.DivineProfile.CorruptionRisk = Math.Max(0f, npc.DivineProfile.CorruptionRisk - 15f);
                changed = true;
                break;

            case DivineAction.Punish:
                npc.DivineProfile.CorruptionRisk = Math.Max(0f, npc.DivineProfile.CorruptionRisk - 5f);
                npc.Loyalty = Math.Max(0f, npc.Loyalty - 5f);  // Bị phạt → bớt tự tin nhưng loyalty thay đổi
                changed = true;
                break;

            case DivineAction.Corrupt:
                // Dark god action — triggers severe doctrine violation
                npc.DivineProfile.CorruptionRisk += 25f;
                npc.DivineProfile.IsDarkPathCandidate = true;
                await AwakentTalentAsync(npcId, "cursed_blood", "Corrupted by Dark God");
                await _doctrineIntegrity.ApplyViolationAsync(npcId, "", ViolationSeverity.SevereBetrayal,
                    "Corrupted by a dark god's influence", false, godId, tick);
                changed = true;
                break;

            case DivineAction.Promote:
                var newRank = await EvaluateChurchPromotionAsync(npcId, npc.PersonalReligionId ?? "");
                changed = newRank != npc.DivineProfile.ChurchRank;
                break;

            case DivineAction.Ignore:
                // Do nothing — saves faith cost
                return true;
        }

        npc.DivineProfile.ReceivedDivineActions.Add(action.ToString());
        await RecalcDivineAttentionAsync(npc);
        await UpdateCandidacyAsync(npc);
        if (changed) await _npcRepo.UpdateAsync(npc);

        _logger.LogInformation("Divine action {Action} applied to {Name} by God {GodId}", action, npc.Name, godId);
        return changed;
    }

    // ─── Church Promotion ─────────────────────────────────

    public async Task<ChurchRank> EvaluateChurchPromotionAsync(string npcId, string religionId)
    {
        var npc = await _npcRepo.GetByIdAsync(npcId);
        if (npc == null) return ChurchRank.Believer;

        var profile = npc.DivineProfile;
        ChurchRank current = profile.ChurchRank;
        ChurchRank next = current;

        // Dark path check first
        if (profile.IsDarkPathCandidate)
        {
            next = current switch
            {
                ChurchRank.Believer        => ChurchRank.SecretCultist,
                ChurchRank.SecretCultist   => ChurchRank.ForbiddenShrineKeeper,
                ChurchRank.ForbiddenShrineKeeper => ChurchRank.DarkPriest,
                ChurchRank.DarkPriest      => ChurchRank.HereticProphet,
                ChurchRank.HereticProphet  => ChurchRank.BloodSaint,
                ChurchRank.BloodSaint      => ChurchRank.DemonVessel,
                _ => current
            };
        }
        else
        {
            // Holy path — requires all conditions
            next = current switch
            {
                ChurchRank.Believer when npc.DevotionLevel > 0.5f
                    => ChurchRank.DevoutBeliever,

                ChurchRank.DevoutBeliever when profile.Achievements.Any(a => a.Category == AchievementCategory.ReligiousDevot)
                    => ChurchRank.TempleHelper,

                ChurchRank.TempleHelper when profile.DivineAttentionScore > 40f
                    => ChurchRank.Priest,

                ChurchRank.Priest when profile.DivineAttentionScore > 80f && npc.Tier >= NpcTier.Servant
                    => ChurchRank.HighPriest,

                ChurchRank.HighPriest when profile.IsProphetCandidate && profile.MiracleExposure > 30f
                    => ChurchRank.Prophet,

                ChurchRank.Prophet when profile.IsSaintCandidate
                    => ChurchRank.Saint,

                ChurchRank.Saint when profile.DestinyModifier > 80f && profile.DivineAttentionScore > 200f
                    => ChurchRank.DivineAvatar,

                _ => current
            };
        }

        if (next != current)
        {
            profile.ChurchRank = next;
            profile.ChurchRankEarnedAt = 0; // tick sẽ was set bởi caller
            await _npcRepo.UpdateAsync(npc);
            _logger.LogInformation("NPC {Name} church promoted: {Old} → {New}", npc.Name, current, next);
        }

        return next;
    }

    // ─── System Tick ──────────────────────────────────────

    public async Task TickAchievementSystemAsync(string worldId, long tick)
    {
        // Tier 3-5 NPCs get passive achievement checks
        var npcs = await _npcRepo.GetByWorldAsync(worldId);
        var eligibleNpcs = npcs.Where(n => n.Tier >= NpcTier.Adventurer && n.State == NpcState.Alive).ToList();

        foreach (var npc in eligibleNpcs)
        {
            // Daily prayer → achievement
            if (npc.DevotionLevel > 0.6f && tick % 30 == 0 && _rng.NextDouble() < 0.1)
                await EarnAchievementAsync(npc.Id, "daily_prayer", tick);

            // Maintained shrine (servants in religious institutions)
            if (npc.Tier == NpcTier.Servant && npc.Piety > 70f && tick % 50 == 0 && _rng.NextDouble() < 0.08)
                await EarnAchievementAsync(npc.Id, "maintained_shrine", tick);

            // Awaken hidden talent via events
            if (tick % 100 == 0 && _rng.NextDouble() < 0.03)
            {
                // Random talent awakening based on personality
                string? talentToAwaken = npc.Personality switch
                {
                    NpcPersonality.Pious     when _rng.NextDouble() < 0.5 => "pure_soul",
                    NpcPersonality.Idealistic                               => "strong_faith",
                    NpcPersonality.Ambitious                                => "strategist",
                    NpcPersonality.Loyal                                    => "unshaken_will",
                    NpcPersonality.Corrupt                                  => "shadow_mind",
                    _                                                        => null
                };
                if (talentToAwaken != null)
                    await AwakentTalentAsync(npc.Id, talentToAwaken, "Personality expression");
            }

            // Church promotion check every 200 ticks
            if (tick % 200 == 0 && npc.PersonalReligionId != null)
                await EvaluateChurchPromotionAsync(npc.Id, npc.PersonalReligionId);
        }
    }
}
