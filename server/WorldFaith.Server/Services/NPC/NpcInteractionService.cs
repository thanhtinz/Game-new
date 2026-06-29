using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Server.Services.Admin;
using WorldFaith.Shared.Contracts;

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
        ILogger<NpcInteractionService> logger)
    {
        _npcRepo = npcRepo;
        _eventRepo = eventRepo;
        _civRepo = civRepo;
        _godRepo = godRepo;
        _religionRepo = religionRepo;
        _orgRepo = orgRepo;
        _balance = balance;
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
        }

        return events;
    }

    // ─── Crime Events ─────────────────────────────────────

    private async Task<List<NpcEventDocument>> CheckCrimeEventsAsync(
        string worldId, CivilizationDocument civ, long tick)
    {
        var events = new List<NpcEventDocument>();
        var npcs = await _npcRepo.GetByCivilizationAsync(civ.Id);

        // Theft: khi economy thấp, Commoner-level crime
        if (civ.Economy < 20f && _rng.NextDouble() < 0.08)
        {
            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Theft, tick,
                "Một vụ trộm xảy ra trong chợ vì nạn đói lan rộng.",
                faithImpact: -2f, economyImpact: -3f, stabilityImpact: -1f);
            events.Add(evt);
        }

        // Corruption Scandal: Noble với Loyalty thấp
        var corruptNoble = npcs
            .Where(n => n.Tier == NpcTier.Noble && n.Loyalty < 35f)
            .OrderBy(_ => _rng.Next()).FirstOrDefault();
        if (corruptNoble != null && _rng.NextDouble() < 0.05)
        {
            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.CorruptionScandal, tick,
                $"{corruptNoble.Name} bị phát hiện nhận hối lộ từ thương nhân nước ngoài.",
                faithImpact: -10f, economyImpact: -5f, stabilityImpact: -8f,
                involvedIds: new List<string> { corruptNoble.Id });

            // Noble mất ảnh hưởng
            corruptNoble.Loyalty -= 10f;
            corruptNoble.Wealth -= 15f;
            await _npcRepo.UpdateAsync(corruptNoble);
            events.Add(evt);
        }

        // Assassination attempt: Royalty khi Noble Ambition cao
        var assassin = npcs
            .Where(n => n.Tier == NpcTier.Noble && n.Ambition > 75f && n.Loyalty < 40f)
            .OrderBy(_ => _rng.Next()).FirstOrDefault();
        var king = npcs.FirstOrDefault(n => n.Tier == NpcTier.Royalty);

        if (assassin != null && king != null && _rng.NextDouble() < 0.02)
        {
            bool success = _rng.NextDouble() < 0.3;
            string desc = success
                ? $"{assassin.Name} tổ chức ám sát thành công {king.Name}! Khủng hoảng kế vị xảy ra."
                : $"{assassin.Name} âm mưu ám sát {king.Name} nhưng thất bại và bị xử tử.";

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

        // Extortion: Servant biết bí mật Noble
        var servantWithSecret = npcs
            .FirstOrDefault(n => n.Tier == NpcTier.Servant && n.KnownSecretAboutNpcId != null);
        if (servantWithSecret != null && _rng.NextDouble() < 0.04)
        {
            var target = await _npcRepo.GetByIdAsync(servantWithSecret.KnownSecretAboutNpcId!);
            if (target != null)
            {
                var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Extortion, tick,
                    $"{servantWithSecret.Name} tống tiền {target.Name} bằng bí mật nguy hiểm.",
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
                    "Mùa màng thất bát. Người dân cầu nguyện thần linh ban phước.",
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
                "Bệnh dịch bùng phát trong thành phố. Dân số giảm, mọi người tìm đến đền thờ.",
                faithImpact: 8f, economyImpact: -5f, stabilityImpact: -10f);
            civ.Population = (int)(civ.Population * 0.9f);
            await _civRepo.UpdateAsync(civ);
            events.Add(evt);
        }

        // Building Collapse
        if (_rng.NextDouble() < 0.01 && tick % 100 == 0)
        {
            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.BuildingCollapse, tick,
                "Công trình đổ sập khiến nhiều người thiệt mạng.",
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

        // Rebellion khi King approval thấp (thể hiện qua civ.AiMemory.GodTrustLevel proxy)
        float approval = civ.AiMemory.GodTrustLevel;
        if (approval < 20f && _rng.NextDouble() < 0.1)
        {
            var rebel = npcs
                .Where(n => n.Tier == NpcTier.Noble && n.Ambition > 60f)
                .OrderByDescending(n => n.Ambition).FirstOrDefault();

            string desc = rebel != null
                ? $"{rebel.Name} dẫn đầu nổi loạn chống lại triều đình. Vương quốc bất ổn."
                : "Người dân nổi loạn vì bất mãn với vua.";

            var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.Rebellion, tick,
                desc, faithImpact: -15f, economyImpact: -12f, stabilityImpact: -30f,
                involvedIds: rebel != null ? new List<string> { rebel.Id } : new());
            civ.Military -= 10f;
            await _civRepo.UpdateAsync(civ);
            events.Add(evt);
        }

        // Coronation khi có King mới (sau assassination)
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
                    $"{successor.Name} được tôn làm vua mới. Vương quốc bước sang trang mới.",
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

        // Tính luck roll với devotion bonus
        float devotionBonus = civ.AiMemory.GodTrustLevel / 10f;
        float luckRoll = (float)(_rng.NextDouble() * 100f + devotionBonus);

        if (luckRoll >= 88f)
        {
            // Lucky: Treasure Found
            if (_rng.NextDouble() < 0.3)
            {
                var evt = await LogEventAsync(worldId, civ.Id, NpcEventType.TreasureFound, tick,
                    "Người dân phát hiện kho báu cổ xưa! Họ coi đây là phước lành từ thần linh.",
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
                    "Một thảm họa xảy đến không báo trước. Người dân bắt đầu hoài nghi về thần linh.",
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
            $"{n1.Name} và {n2.Name} kết hôn. Hai gia tộc liên minh.",
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
            ? $"Quý tộc {betrayer.Name} bí mật liên kết với thế lực ngoại bang để phản lại vương quốc!"
            : $"Người hầu {betrayer.Name} bán bí mật hoàng gia cho gián điệp.";

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
