using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Religion;

/// <summary>
/// Believer Type System — GDD v1.0 Section 8.
/// Each religion has 5 believer types, each with different faith output.
/// Types shift based on events, doctrine, trust, and trauma.
/// </summary>
public interface IBelieverTypeService
{
    Task<float> CalculateFaithFromBelieversAsync(ReligionDocument religion);
    Task ShiftBelieverTypesAsync(string religionId, string eventType, float magnitude);
    Task ConvertCasualToDevoutAsync(string religionId, int count);
    Task CreateCultistFromTraumaAsync(string religionId, int count);
    (int casual, int devout, int fanatic, int cultist, int heretic) GetDistribution(ReligionDocument religion);
}

public class BelieverTypeService : IBelieverTypeService
{
    private readonly IReligionRepository _religionRepo;
    private readonly ILogger<BelieverTypeService> _logger;
    private readonly Random _rng = new();

    // Faith output multipliers per believer type (GDD v1.0)
    private static readonly Dictionary<BelieverType, float> FaithOutput = new()
    {
        [BelieverType.Casual]   = 0.5f,   // Low output, easy to lose
        [BelieverType.Devout]   = 1.0f,   // Baseline
        [BelieverType.Fanatic]  = 2.0f,   // High but risky (extremism)
        [BelieverType.Cultist]  = 1.5f,   // Hidden and stable
        [BelieverType.Heretic]  = 0.3f,   // Variable, unstable
    };

    // Stability risk per type
    private static readonly Dictionary<BelieverType, float> StabilityRisk = new()
    {
        [BelieverType.Casual]   = 0.0f,
        [BelieverType.Devout]   = 0.0f,
        [BelieverType.Fanatic]  = 0.15f,  // May cause extremism
        [BelieverType.Cultist]  = 0.05f,  // If discovered
        [BelieverType.Heretic]  = 0.20f,  // Schism risk
    };

    public BelieverTypeService(IReligionRepository religionRepo, ILogger<BelieverTypeService> logger)
    {
        _religionRepo = religionRepo;
        _logger = logger;
    }

    // ─── Faith Calculation ────────────────────────────────────

    public async Task<float> CalculateFaithFromBelieversAsync(ReligionDocument religion)
    {
        var (casual, devout, fanatic, cultist, heretic) = GetDistribution(religion);

        float total =
            casual   * FaithOutput[BelieverType.Casual]   * 0.01f +
            devout   * FaithOutput[BelieverType.Devout]   * 0.01f +
            fanatic  * FaithOutput[BelieverType.Fanatic]  * 0.01f +
            cultist  * FaithOutput[BelieverType.Cultist]  * 0.01f +
            heretic  * FaithOutput[BelieverType.Heretic]  * 0.01f;

        return total * religion.DevotionLevel;
    }

    public (int casual, int devout, int fanatic, int cultist, int heretic) GetDistribution(ReligionDocument religion)
    {
        int total = religion.FollowerCount;
        if (total == 0) return (0, 0, 0, 0, 0);

        // Use stored counts if available
        int stored = religion.CasualCount + religion.DevoutCount + religion.FanaticCount
                   + religion.CultistCount + religion.HereticCount;

        if (stored == 0)
        {
            // Initialize distribution: 50% casual, 35% devout, 10% fanatic, 3% cultist, 2% heretic
            return (
                casual:  (int)(total * 0.50f),
                devout:  (int)(total * 0.35f),
                fanatic: (int)(total * 0.10f),
                cultist: (int)(total * 0.03f),
                heretic: (int)(total * 0.02f)
            );
        }

        return (religion.CasualCount, religion.DevoutCount, religion.FanaticCount,
                religion.CultistCount, religion.HereticCount);
    }

    // ─── Believer Shifts ──────────────────────────────────────

    /// <summary>
    /// Events shift believer type ratios.
    /// Miracle success → casual → devout.
    /// Failed miracle → devout → casual or heretic.
    /// Holy war → devout → fanatic.
    /// Persecution → devout → cultist.
    /// </summary>
    public async Task ShiftBelieverTypesAsync(string religionId, string eventType, float magnitude)
    {
        var religion = await _religionRepo.GetByIdAsync(religionId);
        if (religion == null) return;

        // Ensure distribution is initialized
        var (cas, dev, fan, cul, her) = GetDistribution(religion);
        int shift = MathF.Max(1, (int)(religion.FollowerCount * magnitude * 0.05f));

        switch (eventType)
        {
            case "MiracleSuccess":
                // Casual → Devout
                int toDevout = MathF.Min(shift, cas);
                cas -= toDevout; dev += toDevout;
                break;

            case "FailedMiracle":
                // Devout → Casual (doubt) + some → Heretic (crisis)
                int toCasual  = MathF.Min(shift, dev);
                int toHeretic = MathF.Min(shift / 3, dev - toCasual);
                dev -= (toCasual + toHeretic);
                cas += toCasual;
                her += toHeretic;
                break;

            case "HolyWar":
                // Devout → Fanatic (zealotry)
                int toFanatic = MathF.Min(shift, dev);
                dev -= toFanatic; fan += toFanatic;
                break;

            case "HeresyTrial":
                // Heretic → either executed (removed) or driven underground (Cultist)
                int hereticsRemoved = MathF.Min(shift / 2, her);
                int toCultist = MathF.Min(shift / 2, her - hereticsRemoved);
                her -= (hereticsRemoved + toCultist);
                cul += toCultist;
                religion.FollowerCount = MathF.Max(0, religion.FollowerCount - hereticsRemoved);
                break;

            case "Persecution":
                // Public believers go underground → Devout/Fanatic → Cultist
                int toCult = MathF.Min(shift, dev + fan);
                int fromDev = MathF.Min(toCult, dev);
                dev -= fromDev; fan -= (toCult - fromDev);
                cul += toCult;
                break;

            case "MassConversion":
                // New casual believers flood in
                cas += shift * 3;
                religion.FollowerCount += shift * 3;
                break;

            case "ProphecyFulfilled":
                // Trust surge: casual → devout
                int toDevout2 = MathF.Min(shift * 2, cas);
                cas -= toDevout2; dev += toDevout2;
                break;
        }

        // Ensure no negatives
        religion.CasualCount   = MathF.Max(0, cas);
        religion.DevoutCount   = MathF.Max(0, dev);
        religion.FanaticCount  = MathF.Max(0, fan);
        religion.CultistCount  = MathF.Max(0, cul);
        religion.HereticCount  = MathF.Max(0, her);

        await _religionRepo.UpdateAsync(religion);
        _logger.LogDebug("Believer shift '{Event}' in {Religion}: C={Cas} D={Dev} F={Fan} Cu={Cul} H={Her}",
            eventType, religion.Name, religion.CasualCount, religion.DevoutCount,
            religion.FanaticCount, religion.CultistCount, religion.HereticCount);
    }

    public async Task ConvertCasualToDevoutAsync(string religionId, int count)
        => await ShiftBelieverTypesAsync(religionId, "ProphecyFulfilled", count / 100f);

    public async Task CreateCultistFromTraumaAsync(string religionId, int count)
        => await ShiftBelieverTypesAsync(religionId, "Persecution", count / 100f);
}
