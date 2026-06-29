using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Religion;

public interface IReligionService
{
    Task<ReligionDocument> FoundReligionAsync(string worldId, string godId, string name, bool isHidden = false);
    Task<List<ReligionUpdateEvent>> TickAsync(string worldId, long tick);
    Task BuildTempleAsync(string worldId, string religionId, string civId);
}

public class ReligionService : IReligionService
{
    private readonly IReligionRepository _religionRepo;
    private readonly ICivilizationRepository _civRepo;
    private readonly IGodRepository _godRepo;
    private readonly ILogger<ReligionService> _logger;
    private readonly Random _rng = new();

    // Tên schism / heresy ngẫu nhiên
    private static readonly string[] SchismPrefixes = { "Tân", "Cải cách", "Chính thống", "Huyền bí", "Cấp tiến", "Bảo thủ" };
    private static readonly string[] HeresyNames   = { "Dị giáo Bóng Tối", "Tà Đạo Nguyên Thủy", "Giáo Phái Bí Ẩn", "Hội Kín Tro Tàn" };

    public ReligionService(
        IReligionRepository religionRepo,
        ICivilizationRepository civRepo,
        IGodRepository godRepo,
        ILogger<ReligionService> logger)
    {
        _religionRepo = religionRepo;
        _civRepo = civRepo;
        _godRepo = godRepo;
        _logger = logger;
    }

    // ─── Found ──────────────────────────────────────────────

    public async Task<ReligionDocument> FoundReligionAsync(
        string worldId, string godId, string name, bool isHidden = false)
    {
        var religion = new ReligionDocument
        {
            WorldId = worldId,
            GodId = godId,
            Name = name,
            IsHidden = isHidden,
            FollowerCount = 0,
            TempleCount = 0,
            DevotionLevel = 0.3f
        };
        await _religionRepo.CreateAsync(religion);

        _logger.LogInformation("Tôn giáo '{Name}' được sáng lập bởi god {GodId}", name, godId);
        return religion;
    }

    // ─── Tick ───────────────────────────────────────────────

    public async Task<List<ReligionUpdateEvent>> TickAsync(string worldId, long tick)
    {
        var religions = await _religionRepo.GetByWorldAsync(worldId);
        var civs = await _civRepo.GetByWorldAsync(worldId);
        var updates = new List<ReligionUpdateEvent>();

        foreach (var religion in religions)
        {
            bool changed = false;

            // --- Spread mỗi 5 tick ---
            if (tick % 5 == 0)
            {
                var spread = await SpreadReligionAsync(religion, civs);
                if (spread) changed = true;
            }

            // --- Devotion decay / growth ---
            changed |= TickDevotion(religion, civs);

            // --- Kiểm tra Schism (mỗi 50 tick, devotion thấp) ---
            if (tick % 50 == 0 && religion.FollowerCount > 500 && religion.DevotionLevel < 0.35f)
            {
                var schism = await TriggerSchismAsync(worldId, religion);
                if (schism != null)
                {
                    updates.Add(new ReligionUpdateEvent
                    {
                        ReligionId = religion.Id,
                        GodId = religion.GodId,
                        Event = ReligionEvent.Schism,
                        FollowerCount = religion.FollowerCount
                    });
                    updates.Add(new ReligionUpdateEvent
                    {
                        ReligionId = schism.Id,
                        GodId = schism.GodId,
                        Event = ReligionEvent.Founded,
                        FollowerCount = schism.FollowerCount
                    });
                    changed = true;
                }
            }

            // --- Kiểm tra Heresy (mỗi 80 tick) ---
            if (tick % 80 == 0 && religion.FollowerCount > 200 && _rng.NextDouble() < 0.08)
            {
                await TriggerHeresyAsync(worldId, religion);
                changed = true;
            }

            // --- Kiểm tra Crusade (mỗi 100 tick) ---
            if (tick % 100 == 0)
            {
                var crusadeTarget = await CheckCrusadeAsync(worldId, religion, religions, civs);
                if (crusadeTarget != null)
                {
                    updates.Add(new ReligionUpdateEvent
                    {
                        ReligionId = religion.Id,
                        GodId = religion.GodId,
                        Event = ReligionEvent.CrusadeStarted,
                        FollowerCount = religion.FollowerCount
                    });
                    changed = true;
                }
            }

            // --- Kiểm tra bị xóa sổ ---
            if (religion.FollowerCount <= 0 && religion.TempleCount == 0)
            {
                await _religionRepo.EraseAsync(religion.Id);
                updates.Add(new ReligionUpdateEvent
                {
                    ReligionId = religion.Id,
                    GodId = religion.GodId,
                    Event = ReligionEvent.ReligionErased,
                    Erased = true
                });

                // Giảm follower count của god
                var god = await _godRepo.GetByIdAsync(religion.GodId);
                if (god != null)
                    await _godRepo.UpdateFaithAsync(god.Id, god.Faith * 0.7f, god.Trust * 0.5f, god.Fear, 0);

                _logger.LogInformation("Tôn giáo '{Name}' bị xóa sổ", religion.Name);
                continue;
            }

            if (changed)
            {
                await _religionRepo.UpdateAsync(religion);
                updates.Add(new ReligionUpdateEvent
                {
                    ReligionId = religion.Id,
                    GodId = religion.GodId,
                    Event = ReligionEvent.Conversion,
                    FollowerCount = religion.FollowerCount
                });
            }
        }

        return updates;
    }

    // ─── Build Temple ────────────────────────────────────────

    public async Task BuildTempleAsync(string worldId, string religionId, string civId)
    {
        var religion = await _religionRepo.GetByIdAsync(religionId);
        var civ = await _civRepo.GetByIdAsync(civId);
        if (religion == null || civ == null) return;

        religion.TempleCount++;
        religion.DevotionLevel = MathF.Min(1f, religion.DevotionLevel + 0.05f);
        if (!religion.CivilizationIds.Contains(civId))
            religion.CivilizationIds.Add(civId);

        await _religionRepo.UpdateAsync(religion);

        // Cập nhật faith cho god
        var god = await _godRepo.GetByIdAsync(religion.GodId);
        if (god != null)
            await _godRepo.UpdateFaithAsync(god.Id, god.Faith + 20f, god.Trust + 2f, god.Fear, god.FollowerCount);

        _logger.LogInformation("Temple '{Religion}' xây tại {Civ}", religion.Name, civ.Name);
    }

    // ─── Spread Logic ────────────────────────────────────────

    private async Task<bool> SpreadReligionAsync(ReligionDocument religion, List<CivilizationDocument> civs)
    {
        // Tính xác suất spread dựa vào devotion, temple count, follower count
        float spreadChance = religion.DevotionLevel * 0.3f
            + religion.TempleCount * 0.05f
            + MathF.Log10(MathF.Max(1, religion.FollowerCount)) * 0.02f;

        // Các civ chưa theo tôn giáo này
        var targetCivs = civs
            .Where(c => !religion.CivilizationIds.Contains(c.Id) && c.State != CivilizationState.Fallen)
            .OrderBy(_ => _rng.Next())
            .Take(3)
            .ToList();

        bool changed = false;
        foreach (var civ in targetCivs)
        {
            // Fanatic civ dễ convert hơn
            float civBonus = civ.Personality == CivilizationPersonality.Fanatic ? 0.2f : 0f;
            // Civ đã có tôn giáo khác thì khó hơn
            float resistance = civ.ReligionIds.Any() ? 0.3f : 0f;

            float chance = spreadChance + civBonus - resistance;
            if ((float)_rng.NextDouble() < chance)
            {
                // Conversion thành công
                int newFollowers = (int)(civ.Population * _rng.Next(5, 20) * 0.01f);
                religion.FollowerCount += newFollowers;

                if (!religion.CivilizationIds.Contains(civ.Id))
                    religion.CivilizationIds.Add(civ.Id);

                if (!civ.ReligionIds.Contains(religion.Id))
                {
                    civ.ReligionIds.Add(religion.Id);
                    // Nếu civ chưa có tôn giáo ruling, set luôn
                    if (civ.RulingReligionId == null)
                        civ.RulingReligionId = religion.Id;
                    await _civRepo.UpdateAsync(civ);
                }

                // Cập nhật follower count cho god
                var god = await _godRepo.GetByIdAsync(religion.GodId);
                if (god != null)
                    await _godRepo.UpdateFaithAsync(god.Id, god.Faith, god.Trust + 1f, god.Fear,
                        god.FollowerCount + newFollowers);

                changed = true;
                _logger.LogDebug("Tôn giáo '{Name}' spread sang {Civ} (+{Followers})",
                    religion.Name, civ.Name, newFollowers);
            }
        }

        // Tăng follower từ civ đang theo (sinh sản / ảnh hưởng nội bộ)
        foreach (var civId in religion.CivilizationIds)
        {
            var civ = civs.FirstOrDefault(c => c.Id == civId);
            if (civ == null || civ.State == CivilizationState.Fallen) continue;

            int growth = (int)(civ.Population * religion.DevotionLevel * 0.001f);
            if (growth > 0)
            {
                religion.FollowerCount += growth;
                changed = true;
            }
        }

        return changed;
    }

    private bool TickDevotion(ReligionDocument religion, List<CivilizationDocument> civs)
    {
        float oldDevotion = religion.DevotionLevel;

        // Temple tăng devotion
        float templeBonus = religion.TempleCount * 0.002f;
        // Civ fanatic tăng thêm
        int fanaticCount = civs.Count(c => c.RulingReligionId == religion.Id
            && c.Personality == CivilizationPersonality.Fanatic);
        float fanaticBonus = fanaticCount * 0.005f;
        // Tự nhiên decay nếu không có temple
        float decay = religion.TempleCount == 0 ? -0.003f : -0.001f;

        religion.DevotionLevel = Clamp01(religion.DevotionLevel + templeBonus + fanaticBonus + decay);
        return MathF.Abs(religion.DevotionLevel - oldDevotion) > 0.001f;
    }

    // ─── Schism ──────────────────────────────────────────────

    private async Task<ReligionDocument?> TriggerSchismAsync(string worldId, ReligionDocument parent)
    {
        if (_rng.NextDouble() > 0.25) return null; // 25% xác suất schism

        int splitFollowers = parent.FollowerCount / 3;
        parent.FollowerCount -= splitFollowers;

        string schismName = $"{SchismPrefixes[_rng.Next(SchismPrefixes.Length)]} {parent.Name}";

        var schism = new ReligionDocument
        {
            WorldId = worldId,
            GodId = parent.GodId, // Vẫn thuộc god gốc nhưng biến thể
            Name = schismName,
            FollowerCount = splitFollowers,
            DevotionLevel = 0.5f, // Schism bắt đầu với devotion cao (nhiệt huyết)
            TempleCount = 0,
            SchismIds = new List<string> { parent.Id }
        };

        await _religionRepo.CreateAsync(schism);

        // Cập nhật parent
        parent.SchismIds.Add(schism.Id);

        _logger.LogInformation("Schism: '{Parent}' tách ra '{Schism}'", parent.Name, schismName);
        return schism;
    }

    // ─── Heresy ──────────────────────────────────────────────

    private async Task TriggerHeresyAsync(string worldId, ReligionDocument parent)
    {
        // Heresy là cult ẩn - không gắn với bất kỳ god nào
        string heresyName = HeresyNames[_rng.Next(HeresyNames.Length)];
        int cultFollowers = (int)(parent.FollowerCount * 0.1f);
        parent.FollowerCount -= cultFollowers;

        var heresy = new ReligionDocument
        {
            WorldId = worldId,
            GodId = parent.GodId,
            Name = heresyName,
            IsHidden = true, // Cult ẩn
            FollowerCount = cultFollowers,
            DevotionLevel = 0.8f // Cực kỳ devoted
        };

        await _religionRepo.CreateAsync(heresy);
        _logger.LogInformation("Heresy '{Name}' hình thành từ '{Parent}'", heresyName, parent.Name);
    }

    // ─── Crusade ─────────────────────────────────────────────

    private async Task<CivilizationDocument?> CheckCrusadeAsync(
        string worldId, ReligionDocument religion,
        List<ReligionDocument> allReligions, List<CivilizationDocument> civs)
    {
        // Chỉ crusade nếu devotion cao + có đủ quân sự
        if (religion.DevotionLevel < 0.7f) return null;
        if (religion.FollowerCount < 300) return null;

        // Tìm civ thuộc religion này có military mạnh
        var crusaderCiv = civs
            .Where(c => c.RulingReligionId == religion.Id && c.Military > 60f)
            .OrderByDescending(c => c.Military)
            .FirstOrDefault();

        if (crusaderCiv == null) return null;

        // Tìm civ theo tôn giáo đối địch
        var targetReligions = allReligions
            .Where(r => r.Id != religion.Id && r.GodId != religion.GodId)
            .ToList();

        if (!targetReligions.Any()) return null;

        var targetReligion = targetReligions[_rng.Next(targetReligions.Count)];
        var targetCiv = civs
            .Where(c => c.RulingReligionId == targetReligion.Id)
            .OrderBy(_ => _rng.Next())
            .FirstOrDefault();

        if (targetCiv == null) return null;

        // Bắt đầu chiến tranh
        crusaderCiv.IsAtWar = true;
        crusaderCiv.AiMemory.CurrentTarget = targetCiv.Id;
        await _civRepo.UpdateAsync(crusaderCiv);

        targetCiv.IsAtWar = true;
        await _civRepo.UpdateAsync(targetCiv);

        _logger.LogInformation("Thánh chiến: '{Attacker}' tấn công '{Target}' vì tôn giáo '{Religion}'",
            crusaderCiv.Name, targetCiv.Name, religion.Name);

        return targetCiv;
    }

    private static float Clamp01(float v) => MathF.Max(0f, MathF.Min(1f, v));
}
