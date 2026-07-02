using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Achievement;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.NPC;

public interface INpcInteractionService
{
    Task<List<NpcEventDocument>> TickAsync(string worldId, long tick);
    Task<NpcEventDocument?> RespondToEventAsync(string worldId, string eventId, string godId, string miracleType);
}

public class NpcInteractionService : INpcInteractionService
{
    private readonly INpcRepository _npcRepo;
    private readonly INpcEventRepository _eventRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IGodRepository _godRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IOrganizationRepository _orgRepo;
    private readonly IBalanceConfigService _balance;
    private readonly IAchievementService _achievementService;
    private readonly IDoctrineIntegrityService _doctrineIntegrity;
    private readonly ILogger<NpcInteractionService> _logger;
    private readonly Random _rng = new();

    public NpcInteractionService(
        INpcRepository npcRepo,
        INpcEventRepository eventRepo,
        ICivilizationRepository civRepo,
        IGodRepository godRepo,
        IReligionRepository religionRepo,
        IOrganizationRepository orgRepo,
        IBalanceConfigService balance,
        IAchievementService achievementService,
        IDoctrineIntegrityService doctrineIntegrity,
        ILogger<NpcInteractionService> logger)
    {
        _npcRepo = npcRepo;
        _eventRepo = eventRepo;
        _civRepo = civRepo;
        _godRepo = godRepo;
        _religionRepo = religionRepo;
        _orgRepo = orgRepo;
        _balance = balance;
        _achievementService = achievementService;
        _doctrineIntegrity = doctrineIntegrity;
        _logger = logger;
    }

    public async Task<List<NpcEventDocument>> TickAsync(string worldId, long tick)
    {
        var events = new List<NpcEventDocument>();
        var civs = await _civRepo.GetByWorldAsync(worldId);

        foreach (var civ in civs)
        {
            if (civ.State == CivilizationState.Fallen) continue;

            // Crime events
            events.AddRange(await CheckCrimeEventsAsync(worldId, civ, tick));

            // Accidents
            events.AddRange(await CheckAccidentEventsAsync(worldId, civ, tick));

            // Social events (marriage, betrayal) — less frequent
            if (tick % 50 == 0)
                events.AddRange(await CheckSocialEventsAsync(worldId, civ, tick));

            // Political events — rare
            if (tick % 100 == 0)
                events.AddRange(await CheckPoliticalEventsAsync(worldId, civ, tick));

            // Luck events
            events.AddRange(await CheckLuckEventsAsync(worldId, civ, tick));

            // v1.2: Temptation events for high-value NPCs
            if (tick % 30 == 0)
                events.AddRange(await CheckTemptationEventsAsync(worldId, civ, tick));
        }

        return events;
    }

    // ─── Temptation Events (v1.2) ─────────────────────────

    private async Task<List<NpcEventDocument>> CheckTemptationEventsAsync(
        string worldId, CivilizationDocument civ, long tick)
    {
        var events = new List<NpcEventDocument>();
        var allNpcs = await _npcRepo.GetByCivilizationAsync(civ.Id);

        // Only check church-rank and high-tier NPCs (performance: not every NPC)
        var vips = allNpcs.Where(n =>
            n.State == NpcState.Alive &&
            (n.DivineProfile.ChurchRank >= ChurchRank.TempleHelper ||
             n.DivineProfile.IsSaintCandidate ||
             n.DivineProfile.IsProphetCandidate)
        ).ToList();

        foreach (var npc in vips)
        {
            if (_rng.NextDouble() > 0.08) continue;  // 8% per check

            // Roll temptation type based on god archetype
            var god = npc.GodInfluenceId != null ? await _godRepo.GetByIdAsync(npc.GodInfluenceId) : null;
            string temptType = god?.Archetype switch
            {
                GodArchetype.Light  => "purity",
                GodArchetype.War    => "cowardice",
                GodArchetype.Order  => "rebellion",
                _                   => "reckless"
            };

            bool resisted = _rng.NextDouble() < (0.3f + npc.DivineProfile.DoctrineIntegrity.Score / 200f);

            if (resisted)
            {
                await _achievementService.AwakentTalentAsync(npc.Id, "unshaken_will", "Resisted temptation");
                await _doctrineIntegrity.ApplyResistanceAsync(npc.Id, $"Resisted {temptType} temptation", tick);

                events.Add(await LogEventAsync(worldId, civ.Id, NpcEventType.LuckGood, tick,
                    $"{npc.Name} resisted {temptType} temptation — faith and integrity increase.",
                    faithImpact: 5f, economyImpact: 0f, stabilityImpact: 0f));
            }
            else
            {
                // Determine severity
                var severity = npc.DivineProfile.ChurchRank >= ChurchRank.Saint
                    ? ViolationSeverity.MajorViolation
                    : ViolationSeverity.ModerateViolation;

                bool isPublic = _rng.NextDouble() < 0.4f;
                await _doctrineIntegrity.ApplyViolationAsync(
                    npc.Id, worldId, severity,
                    $"Gave in to {temptType} temptation", isPublic, null, tick);

                string scandalNote = isPublic ? " — public scandal!" : " (private)";
                events.Add(await LogEventAsync(worldId, civ.Id, NpcEventType.Betrayal, tick,
                    $"{npc.Name} succumbed to {temptType} temptation{scandalNote}",
                    faithImpact: isPublic ? -15f : -5f, economyImpact: 0f, stabilityImpact: isPublic ? -8f : 0f));
            }
        }

        return events;
    }

    // ─── Crime Events ─────────────────────────────────────

    private async Task<List<NpcEventDocument>> CheckCrimeEventsAsync(
        string worldId, CivilizationDocument civ, long tick)
    {
        var events = new List<NpcEventDocument>();
        var npcs = await _npcRepo.GetByCivilizationAsync(civ.Id);

        // Theft: when economy is low, Commoner-level crime
        if (civ.Economy < 20f && _rng.NextDouble() < 0.08)
        {
            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Theft, tick,
                "A theft occurred in the marketplace due to widespread famine.",
                faithImpact: -2f, economyImpact: -3f, stabilityImpact: -1f);
            events.Add(evt);
        }

        // Corruption Scandal: Noble with low Loyalty
        var corruptNoble = npcs
            .Where(n => n.Tier == NpcTier.Noble && n.Loyalty < 35f)
            .OrderBy(_ => _rng.Next()).FirstOrDefault();
        if (corruptNoble != null && _rng.NextDouble() < 0.05)
        {
            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.CorruptionScandal, tick,
                $"{corruptNoble.Name} was discovered accepting bribes from foreign merchants.",
                faithImpact: -10f, economyImpact: -5f, stabilityImpact: -8f,
                involvedIds: new List<string> { corruptNoble.Id });

            // Noble loses influence
            corruptNoble.Loyalty -= 10f;
            corruptNoble.Wealth -= 15f;
            await _npcRepo.UpdateAsync(corruptNoble);
            events.Add(evt);
        }

        // Assassination attempt: Royalty when Noble Ambition cao
        var assassin = npcs
            .Where(n => n.Tier == NpcTier.Noble && n.Ambition > 75f && n.Loyalty < 40f)
            .OrderBy(_ => _rng.Next()).FirstOrDefault();
        var king = npcs.FirstOrDefault(n => n.Tier == NpcTier.Royalty);

        if (assassin != null && king != null && _rng.NextDouble() < 0.02)
        {
            bool success = _rng.NextDouble() < 0.3;
            string desc = success
                ? $"{assassin.Name} successfully orchestrated an assassination of {king.Name}! A succession crisis has erupted."
                : $"{assassin.Name} conspired to assassinate {king.Name} but failed and was executed.";

            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Assassination, tick,
                desc, faithImpact: success ? -20f : 5f, economyImpact: -10f, stabilityImpact: -25f,
                involvedIds: new List<string> { assassin.Id, king.Id });

            if (success)
            {
                king.State = NpcState.Dead;
                await _npcRepo.UpdateAsync(king);
                // Civ destabilized
                civ.AiMemory.GodTrustLevel -= 20f;
            }
            else
            {
                assassin.State = NpcState.Dead;
                await _npcRepo.UpdateAsync(assassin);
            }
            await _civRepo.UpdateAsync(civ);
            events.Add(evt);
        }

        // Extortion: Servant knows Noble's secret
        var servantWithSecret = npcs
            .FirstOrDefault(n => n.Tier == NpcTier.Servant && n.KnownSecretAboutNpcId != null);
        if (servantWithSecret != null && _rng.NextDouble() < 0.04)
        {
            var target = await _npcRepo.GetByIdAsync(servantWithSecret.KnownSecretAboutNpcId!);
            if (target != null)
            {
                var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Extortion, tick,
                    $"{servantWithSecret.Name} is extorting {target.Name} with a dangerous secret.",
                    faithImpact: -5f, economyImpact: -8f, stabilityImpact: -5f,
                    involvedIds: new List<string> { servantWithSecret.Id, target.Id });
                target.Wealth -= 15f;
                await _npcRepo.UpdateAsync(target);
                events.Add(evt);
            }
        }

        return events;
    }

    // ─── Accident Events ──────────────────────────────────

    private async Task<List<NpcEventDocument>> CheckAccidentEventsAsync(
        string worldId, CivilizationDocument civ, long tick)
    {
        var events = new List<NpcEventDocument>();

        // Crop Failure
        if (civ.Economy < 30f || _rng.NextDouble() < 0.05)
        {
            var fertility = GetTileFertility(worldId, civ);
            if (fertility < 0.3f && _rng.NextDouble() < 0.05)
            {
                var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.CropFailure, tick,
                    "The harvest has failed. People pray to the gods for blessings.",
                    faithImpact: -5f, economyImpact: -8f, stabilityImpact: -3f);
                civ.Economy -= 8f;
                await _civRepo.UpdateAsync(civ);
                events.Add(evt);
            }
        }

        // Disease Outbreak
        if (_rng.NextDouble() < 0.03 && tick % 50 == 0)
        {
            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.DiseaseOutbreak, tick,
                "A plague has broken out in the city. Population falls as people flock to the temples.",
                faithImpact: 8f, economyImpact: -5f, stabilityImpact: -10f);
            civ.Population = (int)(civ.Population * 0.9f);
            await _civRepo.UpdateAsync(civ);

            // Pious NPCs with high piety can earn the achievement "survived_plague_prayer"
            var piousNpcs = await _npcRepo.GetByTierAsync(civ.WorldId, NpcTier.Servant);
            var survivor = piousNpcs.Where(n => n.CivilizationId == civ.Id && n.Piety > 70f)
                .OrderBy(_ => _rng.Next()).FirstOrDefault();
            if (survivor != null && _rng.NextDouble() < 0.3)
                await _achievementService.EarnAchievementAsync(survivor.Id, "survived_plague_prayer", tick);

            events.Add(evt);
        }

        // Building Collapse
        if (_rng.NextDouble() < 0.01 && tick % 100 == 0)
        {
            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.BuildingCollapse, tick,
                "A building collapse has killed many people.",
                faithImpact: -3f, economyImpact: -5f, stabilityImpact: -3f);
            events.Add(evt);
        }

        return events;
    }

    // ─── Social Events ────────────────────────────────────

    private async Task<List<NpcEventDocument>> CheckSocialEventsAsync(
        string worldId, CivilizationDocument civ, long tick)
    {
        var events = new List<NpcEventDocument>();
        var npcs = await _npcRepo.GetByCivilizationAsync(civ.Id);
        var nobles = npcs.Where(n => n.Tier == NpcTier.Noble && n.SpouseId == null).ToList();

        // Political Marriage
        if (nobles.Count >= 2 && _rng.NextDouble() < 0.15)
        {
            var n1 = nobles[_rng.Next(nobles.Count)];
            var remaining = nobles.Where(n => n.Id != n1.Id).ToList();
            if (remaining.Any())
            {
                var n2 = remaining[_rng.Next(remaining.Count)];
                await ProcessMarriageAsync(worldId, civ, n1, n2, MarriageType.Political, tick, events);
            }
        }

        // Noble Betrayal
        var betrayalCandidate = npcs
            .Where(n => n.Tier is NpcTier.Noble or NpcTier.Servant
                     && n.Ambition > 70f && n.Loyalty < 35f)
            .OrderBy(_ => _rng.Next()).FirstOrDefault();

        if (betrayalCandidate != null && _rng.NextDouble() < 0.08)
            await ProcessBetrayalAsync(worldId, civ, betrayalCandidate, tick, events);

        // Servant discovers Noble secret
        var servants = npcs.Where(n => n.Tier == NpcTier.Servant && n.KnownSecretAboutNpcId == null).ToList();
        var secretNobles = npcs.Where(n => n.Tier == NpcTier.Noble && n.Loyalty < 50f).ToList();
        if (servants.Any() && secretNobles.Any() && _rng.NextDouble() < 0.06)
        {
            var servant = servants[_rng.Next(servants.Count)];
            var noble = secretNobles[_rng.Next(secretNobles.Count)];
            servant.KnownSecretAboutNpcId = noble.Id;
            servant.SecretType = _rng.NextDouble() < 0.5 ? "corruption" : "heresy";
            await _npcRepo.UpdateAsync(servant);
            _logger.LogInformation("Servant {S} discovered secret of Noble {N}", servant.Name, noble.Name);
        }

        return events;
    }

    // ─── Political Events ─────────────────────────────────

    private async Task<List<NpcEventDocument>> CheckPoliticalEventsAsync(
        string worldId, CivilizationDocument civ, long tick)
    {
        var events = new List<NpcEventDocument>();
        var npcs = await _npcRepo.GetByCivilizationAsync(civ.Id);
        var king = npcs.FirstOrDefault(n => n.Tier == NpcTier.Royalty);

        // Rebellion when King approval is low (approximated by civ.AiMemory.GodTrustLevel)
        float approval = civ.AiMemory.GodTrustLevel;
        if (approval < 20f && _rng.NextDouble() < 0.1)
        {
            var rebel = npcs
                .Where(n => n.Tier == NpcTier.Noble && n.Ambition > 60f)
                .OrderByDescending(n => n.Ambition).FirstOrDefault();

            string desc = rebel != null
                ? $"{rebel.Name} leads a rebellion against the court. The kingdom is destabilized."
                : "The people have risen in revolt against the king.";

            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Rebellion, tick,
                desc, faithImpact: -15f, economyImpact: -12f, stabilityImpact: -30f,
                involvedIds: rebel != null ? new List<string> { rebel.Id } : new());
            civ.Military -= 10f;
            await _civRepo.UpdateAsync(civ);
            events.Add(evt);
        }

        // Coronation when a new King emerges (after assassination)
        bool noKing = !npcs.Any(n => n.Tier == NpcTier.Royalty && n.State == NpcState.Alive);
        if (noKing)
        {
            var successor = npcs
                .Where(n => n.Tier == NpcTier.Noble && n.Ambition > 50f)
                .OrderByDescending(n => n.Ambition + n.Piety).FirstOrDefault();

            if (successor != null)
            {
                successor.Tier = NpcTier.Royalty;
                successor.Name = $"King {successor.Name.Split(' ').LastOrDefault()}";
                await _npcRepo.UpdateAsync(successor);

                var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Coronation, tick,
                    $"{successor.Name} has been crowned the new king. The kingdom enters a new era.",
                    faithImpact: 10f, economyImpact: 0f, stabilityImpact: 15f,
                    involvedIds: new List<string> { successor.Id });
                events.Add(evt);
            }
        }

        return events;
    }

    // ─── Luck Events ──────────────────────────────────────

    private async Task<List<NpcEventDocument>> CheckLuckEventsAsync(
        string worldId, CivilizationDocument civ, long tick)
    {
        var events = new List<NpcEventDocument>();

        // Tính luck roll with devotion bonus
        float devotionBonus = civ.AiMemory.GodTrustLevel / 10f;
        float luckRoll = (float)(_rng.NextDouble() * 100f + devotionBonus);

        if (luckRoll >= 88f)
        {
            // Lucky: Treasure Found
            if (_rng.NextDouble() < 0.3)
            {
                var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.TreasureFound, tick,
                    "The people discovered ancient treasure! They consider it a blessing from the gods.",
                    faithImpact: 15f, economyImpact: 20f, stabilityImpact: 5f);
                civ.Economy += 20f;
                await _civRepo.UpdateAsync(civ);
                events.Add(evt);
            }
        }
        else if (luckRoll <= 12f)
        {
            // Unlucky: Crisis of Faith
            if (_rng.NextDouble() < 0.2)
            {
                var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.CrisisOfFaith, tick,
                    "An unexpected disaster strikes. People begin to doubt the gods.",
                    faithImpact: -12f, economyImpact: -5f, stabilityImpact: -5f);
                events.Add(evt);
            }
        }

        return events;
    }

    // ─── God Response ─────────────────────────────────────

    public async Task<NpcEventDocument?> RespondToEventAsync(
        string worldId, string eventId, string godId, string miracleType)
    {
        var recentEvents = await _eventRepo.GetRecentAsync(worldId, 100);
        var evt = recentEvents.FirstOrDefault(e => e.Id == eventId);
        if (evt == null || evt.GodResponded) return null;

        var god = await _godRepo.GetByIdAsync(godId);
        var civ = await _civRepo.GetByIdAsync(evt.CivilizationId);
        if (god == null || civ == null) return null;

        // God responds → followers gain Trust
        evt.GodResponded = true;
        evt.RespondingGodId = godId;

        float trustGain = miracleType switch
        {
            "HealFollower" or "BlessHarvest" when evt.Type == NpcEventType.DiseaseOutbreak => 20f,
            "BlessHarvest" when evt.Type == NpcEventType.CropFailure => 25f,
            "Curse" when evt.Type is NpcEventType.Theft or NpcEventType.CorruptionScandal => 15f,
            _ => 8f
        };

        civ.AiMemory.GodTrustLevel = MathF.Min(100f, civ.AiMemory.GodTrustLevel + trustGain);
        await _civRepo.UpdateAsync(civ);

        _logger.LogInformation("God {GodId} responded to event {EventType} with {Miracle} → Trust +{Trust}",
            godId, evt.Type, miracleType, trustGain);

        return evt;
    }

    // ─── Helpers ──────────────────────────────────────────

    private async Task ProcessMarriageAsync(
        string worldId, CivilizationDocument civ,
        NpcDocument n1, NpcDocument n2,
        MarriageType type, long tick,
        List<NpcEventDocument> events)
    {
        n1.SpouseId = n2.Id;
        n2.SpouseId = n1.Id;
        n1.Relationships.Add(new NpcRelationship { NpcId = n2.Id, Type = RelationshipType.Spouse, Strength = 70f });
        n2.Relationships.Add(new NpcRelationship { NpcId = n1.Id, Type = RelationshipType.Spouse, Strength = 70f });
        await _npcRepo.UpdateAsync(n1);
        await _npcRepo.UpdateAsync(n2);

        var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Marriage, tick,
            $"{n1.Name} and {n2.Name} kết hôn. Hai gia tộc liên minh.",
            faithImpact: 10f, economyImpact: 5f, stabilityImpact: 8f,
            involvedIds: new List<string> { n1.Id, n2.Id });
        events.Add(evt);
    }

    private async Task ProcessBetrayalAsync(
        string worldId, CivilizationDocument civ,
        NpcDocument betrayer, long tick,
        List<NpcEventDocument> events)
    {
        string description = betrayer.Tier == NpcTier.Noble
            ? $"Noble {betrayer.Name} has secretly allied with foreign powers to betray the kingdom!"
            : $"Servant {betrayer.Name} sold royal secrets to spies.";

        var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Betrayal, tick,
            description, faithImpact: -8f, economyImpact: -5f, stabilityImpact: -20f,
            involvedIds: new List<string> { betrayer.Id });

        betrayer.State = NpcState.Exiled;
        await _npcRepo.UpdateAsync(betrayer);
        civ.Military -= 5f;
        await _civRepo.UpdateAsync(civ);
        events.Add(evt);
    }

    private async Task<NpcEventDocument> LogEventAsync(
        string worldId, string civId, NpcEventType type, long tick,
        string description, float faithImpact, float economyImpact,
        float stabilityImpact, List<string>? involvedIds = null)
    {
        var doc = new NpcEventDocument
        {
            WorldId = worldId,
            CivilizationId = civId,
            Type = type,
            InvolvedNpcIds = involvedIds ?? new(),
            Description = description,
            FaithImpact = faithImpact,
            EconomyImpact = economyImpact,
            StabilityImpact = stabilityImpact,
            Tick = tick
        };
        await _eventRepo.LogAsync(doc);
        return doc;
    }

    private float GetTileFertility(string worldId, CivilizationDocument civ)
        => 0.5f; // placeholder — thực tế lấy từ WorldDocument tiles
}
