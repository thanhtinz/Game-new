using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Contracts;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Faith;

/// <summary>
/// God Rank System — GDD v1.0 Section 7.
/// Rank tăng theo cumulative faith milestones.
/// Rank modifier ảnh hưởng miracle efficiency và faith generation.
/// Forgotten god: 0 followers nhưng còn relics/cults → survive in diminished form.
/// </summary>
public interface IGodRankService
{
    Task<GodRank> UpdateRankAsync(string godId);
    Task<bool> CheckForgottenStateAsync(string worldId, string godId);
    Task<float> GetRankMultiplierAsync(string godId);
    Task RecordMemoryAsync(string godId, GodMemoryEntry memory);
    Task<List<GodMemoryEntry>> GetMemoriesAsync(string godId, int limit = 20);
}

public class GodRankService : IGodRankService
{
    private readonly IGodRepository _godRepo;
    private readonly IRelicRepository _relicRepo;
    private readonly IReligionRepository _religionRepo;
    private readonly ILogger<GodRankService> _logger;

    // Faith thresholds cho mỗi rank
    private static readonly Dictionary<GodRank, int> RankThresholds = new()
    {
        [GodRank.Nascent]     = 0,
        [GodRank.Awakened]    = 5_000,
        [GodRank.Established] = 25_000,
        [GodRank.Revered]     = 100_000,
        [GodRank.Exalted]     = 400_000,
        [GodRank.Ancient]     = 1_000_000,
    };

    public GodRankService(
        IGodRepository godRepo,
        IRelicRepository relicRepo,
        IReligionRepository religionRepo,
        ILogger<GodRankService> logger)
    {
        _godRepo = godRepo;
        _relicRepo = relicRepo;
        _religionRepo = religionRepo;
        _logger = logger;
    }

    // ─── Rank Update ─────────────────────────────────────────

    public async Task<GodRank> UpdateRankAsync(string godId)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null) return GodRank.Forgotten;

        // Accumulate total faith
        god.RankData.TotalFaithEarned += (int)god.Faith;

        var newRank = CalculateRank(god.RankData.TotalFaithEarned);

        if (newRank != god.RankData.Rank)
        {
            var oldRank = god.RankData.Rank;
            god.RankData.Rank = newRank;

            // Unlock miracles theo rank
            UnlockMiraclesForRank(god, newRank);

            _logger.LogInformation("God {Name} ranked up: {OldRank} → {NewRank}", god.Name, oldRank, newRank);

            // Record memory
            await RecordMemoryAsync(godId, new GodMemoryEntry
            {
                Type = MemoryType.MiracleSuccess,
                Description = $"{god.Name} đạt cấp độ {newRank}",
                Tick = 0,
                TrustImpact = 5f
            });
        }

        await _godRepo.UpdateAsync(god);
        return god.RankData.Rank;
    }

    private static GodRank CalculateRank(int totalFaith)
    {
        GodRank rank = GodRank.Nascent;
        foreach (var (r, threshold) in RankThresholds.OrderBy(kv => kv.Value))
            if (totalFaith >= threshold) rank = r;
        return rank;
    }

    private static void UnlockMiraclesForRank(GodDocument god, GodRank rank)
    {
        var toUnlock = rank switch
        {
            GodRank.Awakened    => new[] { MiracleType.Omen, MiracleType.HealFollower, MiracleType.Storm },
            GodRank.Established => new[] { MiracleType.Curse, MiracleType.DivineVoice, MiracleType.Earthquake, MiracleType.Portal },
            GodRank.Revered     => new[] { MiracleType.Volcano, MiracleType.Revelation, MiracleType.DemonInvasion },
            GodRank.Exalted     => new[] { MiracleType.DivineBeastCreation, MiracleType.HolyWar },
            _ => Array.Empty<MiracleType>()
        };

        foreach (var m in toUnlock)
            if (!god.UnlockedMiracles.Contains(m))
                god.UnlockedMiracles.Add(m);
    }

    // ─── Forgotten God ────────────────────────────────────────

    /// <summary>
    /// Kiểm tra và cập nhật trạng thái Forgotten.
    /// God với 0 followers có thể survive nếu còn relics hoặc hidden cults.
    /// Returns true nếu god vẫn sống sót (dù Forgotten).
    /// </summary>
    public async Task<bool> CheckForgottenStateAsync(string worldId, string godId)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null) return false;

        if (god.FollowerCount > 0)
        {
            // Không Forgotten
            if (god.IsForgotten)
            {
                god.IsForgotten = false;
                god.RankData.Rank = CalculateRank(god.RankData.TotalFaithEarned);
                await _godRepo.UpdateAsync(god);
                _logger.LogInformation("God {Name} recovered from Forgotten state!", god.Name);
            }
            return true;
        }

        // 0 followers — kiểm tra relics và hidden cults
        var relics = await _relicRepo.GetByGodAsync(worldId, godId);
        var hiddenReligions = await _religionRepo.GetByGodAsync(godId);
        var activeCults = hiddenReligions.Where(r => r.IsHidden && r.FollowerCount > 0).ToList();

        bool hasRelics = relics.Any(r => r.IsActive);
        bool hasCults  = activeCults.Any();

        if (hasRelics || hasCults)
        {
            // Survive as Forgotten — giảm Faith gen xuống 10%
            god.IsForgotten = true;
            god.RankData.Rank = GodRank.Forgotten;

            // Faith từ relics và hidden cults
            float relicFaith = relics.Sum(r => r.FaithBonus);
            float cultFaith  = activeCults.Sum(c => c.FollowerCount * 0.01f);
            god.Faith = MathF.Min(god.Faith + relicFaith + cultFaith, 500f); // cap thấp khi Forgotten

            await _godRepo.UpdateAsync(god);

            _logger.LogInformation("God {Name} is Forgotten but survives via {Relics} relics and {Cults} cults",
                god.Name, relics.Count, activeCults.Count);
            return true;
        }

        // Thực sự biến mất
        god.IsAlive = false;
        god.IsForgotten = true;
        god.RankData.Rank = GodRank.Forgotten;
        await _godRepo.UpdateAsync(god);
        _logger.LogInformation("God {Name} has been completely forgotten and eliminated", god.Name);
        return false;
    }

    // ─── Multiplier ───────────────────────────────────────────

    public async Task<float> GetRankMultiplierAsync(string godId)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        return god?.RankData.RankMultiplier ?? 1f;
    }

    // ─── Memory ───────────────────────────────────────────────

    public async Task RecordMemoryAsync(string godId, GodMemoryEntry memory)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        if (god == null) return;

        god.Memories.Insert(0, memory);
        if (god.Memories.Count > 50) // Giữ 50 memories gần nhất
            god.Memories = god.Memories.Take(50).ToList();

        await _godRepo.UpdateAsync(god);
    }

    public async Task<List<GodMemoryEntry>> GetMemoriesAsync(string godId, int limit = 20)
    {
        var god = await _godRepo.GetByIdAsync(godId);
        return god?.Memories.Take(limit).ToList() ?? new();
    }

    // Helper để GodRepository UpdateAsync
    private async Task UpdateGodAsync(GodDocument god)
    {
        // GodRepository cần UpdateAsync method — implement nếu chưa có
    }
}
