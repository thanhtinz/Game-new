using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;
using WorldFaith.Shared.Contracts;

namespace WorldFaith.Server.Services.Simulation;

public interface IMiracleService
{
    Task<MiracleResultEvent> PerformMiracleAsync(string worldId, string godId, MiracleType miracle, int x, int y, string? targetCivId);
    Task<MiracleResultEvent?> CounterMiracleAsync(string worldId, string godId, string miracleEventId, MiracleType counter);
}

public class MiracleService : IMiracleService
{
    private readonly IGodRepository _godRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly IMiracleEventRepository _miracleRepo;
    private readonly IFaithService _faithService;
    private readonly ILogger<MiracleService> _logger;
    private readonly Random _rng = new();

    // Lưu pending miracles để counter
    private readonly Dictionary<string, (MiracleEventDocument doc, DateTime expiry)> _pendingMiracles = new();

    public MiracleService(
        IGodRepository godRepo,
        ICivilizationRepository civRepo,
        IReligionRepository religionRepo,
        IMiracleEventRepository miracleRepo,
        IFaithService faithService,
        ILogger<MiracleService> logger)
    {
        _godRepo = godRepo;
        _civRepo = civRepo;
        _religionRepo = religionRepo;
        _miracleRepo = miracleRepo;
        _faithService = faithService;
        _logger = logger;
    }

    public async Task<MiracleResultEvent> PerformMiracleAsync(
        string worldId, string godId, MiracleType miracle, int x, int y, string? targetCivId)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null || !god.IsAlive)
            return FailEvent(godId, miracle, "God không tồn tại hoặc đã chết");

        if (!god.UnlockedMiracles.Contains(miracle))
            return FailEvent(godId, miracle, "Miracle chưa được mở khóa");

        if (!await _faithService.CanPerformMiracleAsync(godId, miracle))
            return FailEvent(godId, miracle, "Không đủ Faith");

        var faithCost = await _faithService.ConsumeFaithAsync(godId, miracle);

        // Thực thi hiệu ứng miracle lên thế giới
        var description = await ApplyMiracleEffectAsync(worldId, godId, miracle, x, y, targetCivId);

        var eventDoc = new MiracleEventDocument
        {
            WorldId = worldId,
            GodId = godId,
            Miracle = miracle,
            X = x,
            Y = y,
            Success = true,
            FaithCost = faithCost
        };
        await _miracleRepo.LogAsync(eventDoc);

        // Đưa vào pending để rival gods có thể counter (5 giây)
        _pendingMiracles[eventDoc.Id] = (eventDoc, DateTime.UtcNow.AddSeconds(5));

        _logger.LogInformation("God {GodId} performed {Miracle} at ({X},{Y})", godId, miracle, x, y);

        return new MiracleResultEvent
        {
            MiracleEventId = eventDoc.Id,
            Miracle = miracle,
            GodId = godId,
            Success = true,
            WasCountered = false,
            FaithCost = faithCost,
            Description = description
        };
    }

    public async Task<MiracleResultEvent?> CounterMiracleAsync(
        string worldId, string counteringGodId, string miracleEventId, MiracleType counter)
    {
        if (!_pendingMiracles.TryGetValue(miracleEventId, out var pending))
            return null;

        if (DateTime.UtcNow > pending.expiry)
        {
            _pendingMiracles.Remove(miracleEventId);
            return null;
        }

        var counterGod = await _godRepo.GetByIdAsync(counteringGodId);
        if (counterGod == null || counterGod.Id == pending.doc.GodId)
            return null;

        if (!await _faithService.CanPerformMiracleAsync(counteringGodId, counter))
            return null;

        // Counter miracle giảm hiệu ứng của miracle gốc
        var counterCost = await _faithService.ConsumeFaithAsync(counteringGodId, counter);

        _pendingMiracles.Remove(miracleEventId);

        // Cập nhật event gốc
        pending.doc.WasCountered = true;
        pending.doc.CounteredByGodId = counteringGodId;

        _logger.LogInformation("God {GodId} countered miracle {MiracleEventId}", counteringGodId, miracleEventId);

        return new MiracleResultEvent
        {
            MiracleEventId = miracleEventId,
            Miracle = counter,
            GodId = counteringGodId,
            Success = true,
            WasCountered = true,
            CounteredByGodId = counteringGodId,
            FaithCost = counterCost,
            Description = $"{counterGod.Name} đã phản phép {pending.doc.Miracle}"
        };
    }

    // ─── Miracle Effects ────────────────────────────────────

    private async Task<string> ApplyMiracleEffectAsync(
        string worldId, string godId, MiracleType miracle, int x, int y, string? targetCivId)
    {
        return miracle switch
        {
            MiracleType.Rain => await ApplyRain(worldId, x, y),
            MiracleType.Dream => await ApplyDream(worldId, godId, targetCivId),
            MiracleType.BlessHarvest => await ApplyBlessHarvest(worldId, targetCivId),
            MiracleType.HealFollower => "Follower được chữa lành, Trust tăng",
            MiracleType.Omen => await ApplyOmen(worldId, godId, targetCivId),
            MiracleType.Storm => await ApplyStorm(worldId, x, y),
            MiracleType.Earthquake => await ApplyEarthquake(worldId, x, y),
            MiracleType.Curse => await ApplyCurse(worldId, targetCivId),
            MiracleType.Portal => "Portal được mở, thương mại liên vùng tăng",
            MiracleType.DivineVoice => await ApplyDivineVoice(worldId, godId, targetCivId),
            MiracleType.Volcano => await ApplyVolcano(worldId, x, y),
            MiracleType.DemonInvasion => await ApplyDemonInvasion(worldId, x, y),
            MiracleType.DivineBeastCreation => "Divine Beast xuất hiện",
            MiracleType.Revelation => await ApplyRevelation(worldId, godId),
            MiracleType.HolyWar => await ApplyHolyWar(worldId, godId),
            _ => "Miracle được thực thi"
        };
    }

    private Task<string> ApplyRain(string worldId, int x, int y)
        => Task.FromResult($"Mưa rơi tại ({x},{y}), độ phì nhiêu tăng +15%");

    private async Task<string> ApplyDream(string worldId, string godId, string? civId)
    {
        if (civId == null) return "Dream gửi đến toàn thế giới";
        var civ = await _civRepo.GetByIdAsync(civId);
        if (civ == null) return "Civilization không tìm thấy";
        civ.AiMemory.GodTrustLevel += 5f;
        civ.AiMemory.LastGodInteraction = godId;
        await _civRepo.UpdateAsync(civ);
        return $"{civ.Name} nhận được giấc mơ thần thánh, Trust tăng";
    }

    private async Task<string> ApplyBlessHarvest(string worldId, string? civId)
    {
        if (civId == null) return "Harvest được ban phước";
        var civ = await _civRepo.GetByIdAsync(civId);
        if (civ == null) return "Không tìm thấy civilization";
        civ.Economy += 20f;
        civ.Population += (int)(civ.Population * 0.05f);
        await _civRepo.UpdateAsync(civ);
        return $"{civ.Name}: Economy +20, Population +5%";
    }

    private async Task<string> ApplyOmen(string worldId, string godId, string? civId)
    {
        if (civId == null) return "Điềm báo xuất hiện khắp thế giới";
        var civ = await _civRepo.GetByIdAsync(civId);
        if (civ == null) return "Không tìm thấy civilization";
        civ.AiMemory.GodTrustLevel += 2f;
        await _civRepo.UpdateAsync(civ);
        return $"Điềm báo thần thánh xuất hiện tại {civ.Name}";
    }

    private async Task<string> ApplyStorm(string worldId, int x, int y)
        => $"Cơn bão tàn phá vùng ({x},{y}), Military -10 các civ lân cận";

    private async Task<string> ApplyEarthquake(string worldId, int x, int y)
        => $"Động đất tại ({x},{y}), hủy diệt công trình và giảm Population";

    private async Task<string> ApplyCurse(string worldId, string? civId)
    {
        if (civId == null) return "Lời nguyền phủ lên thế giới";
        var civ = await _civRepo.GetByIdAsync(civId);
        if (civ == null) return "Không tìm thấy civilization";
        civ.Economy -= 15f;
        civ.AiMemory.GodTrustLevel -= 10f;
        await _civRepo.UpdateAsync(civ);
        return $"{civ.Name} bị nguyền: Economy -15, Trust -10";
    }

    private async Task<string> ApplyDivineVoice(string worldId, string godId, string? civId)
    {
        if (civId == null) return "Tiếng nói thần thánh vang vọng";
        var civ = await _civRepo.GetByIdAsync(civId);
        if (civ == null) return "Không tìm thấy civilization";
        civ.AiMemory.GodTrustLevel += 15f;
        civ.AiMemory.LastGodInteraction = godId;
        await _civRepo.UpdateAsync(civ);
        return $"Tiếng nói thần thánh hướng dẫn {civ.Name}";
    }

    private Task<string> ApplyVolcano(string worldId, int x, int y)
        => Task.FromResult($"Núi lửa phun trào tại ({x},{y}), phá hủy toàn bộ khu vực");

    private async Task<string> ApplyDemonInvasion(string worldId, int x, int y)
        => $"Quỷ dữ xâm nhập từ ({x},{y}), các civ lân cận bị tấn công";

    private async Task<string> ApplyRevelation(string worldId, string godId)
    {
        var gods = await _godRepo.GetByWorldAsync(worldId);
        return $"Khải thị thần thánh lan ra toàn thế giới, Faith +50 mọi believer";
    }

    private async Task<string> ApplyHolyWar(string worldId, string godId)
    {
        var religions = await _religionRepo.GetByGodAsync(godId);
        return $"Thánh chiến bùng nổ nhân danh {(religions.FirstOrDefault()?.Name ?? "thần")}";
    }

    private static MiracleResultEvent FailEvent(string godId, MiracleType miracle, string reason) => new()
    {
        Miracle = miracle,
        GodId = godId,
        Success = false,
        Description = reason
    };
}
