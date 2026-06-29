using WorldFaith.Server.Models;
using WorldFaith.Server.Repositories;
using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Simulation;

/// <summary>
/// Government System — GDD v1.0 Section 11.
/// Government type ảnh hưởng AI priority, faith spread speed,
/// royal support modifier, and rebellion risk.
/// </summary>
public interface IGovernmentService
{
    GovernmentBehavior GetBehavior(GovernmentType gov);
    Task<GovernmentType> EvolveGovernmentAsync(CivilizationDocument civ);
    Task ApplyGovernmentModifiersAsync(CivilizationDocument civ, long tick);
    float GetReligionSpreadModifier(GovernmentType gov);
    float GetRoyalConversionImpact(GovernmentType gov);
    float GetRebellionRisk(CivilizationDocument civ);
}

public class GovernmentBehavior
{
    public GovernmentType Type { get; set; }
    public float PolicySpeed { get; set; }          // How fast royal decisions take effect
    public float ReligiousUnityBonus { get; set; }  // Bonus to religious unity
    public float MilitaryEfficiency { get; set; }
    public float EconomyEfficiency { get; set; }
    public float CrimeSuppressionBonus { get; set; }
    public float SchismRisk { get; set; }           // Internal religious conflict chance
    public string StrengthNote { get; set; } = string.Empty;
    public string WeaknessNote { get; set; } = string.Empty;
}

public class GovernmentService : IGovernmentService
{
    private readonly ICivilizationRepository _civRepo;
    private readonly ILogger<GovernmentService> _logger;
    private readonly Random _rng = new();

    private static readonly Dictionary<GovernmentType, GovernmentBehavior> Behaviors = new()
    {
        [GovernmentType.Monarchy] = new()
        {
            Type = GovernmentType.Monarchy,
            PolicySpeed = 1.5f,         // Fast — king decides
            ReligiousUnityBonus = 0.3f, // Royal faith spreads through decree
            MilitaryEfficiency = 1.2f,
            EconomyEfficiency = 1.0f,
            CrimeSuppressionBonus = 0.8f,
            SchismRisk = 0.05f,         // Low internal conflict
            StrengthNote = "Fast policy from royal decision",
            WeaknessNote = "Succession crisis risk"
        },
        [GovernmentType.Theocracy] = new()
        {
            Type = GovernmentType.Theocracy,
            PolicySpeed = 1.0f,
            ReligiousUnityBonus = 0.6f, // Priests control doctrine
            MilitaryEfficiency = 0.9f,
            EconomyEfficiency = 0.8f,
            CrimeSuppressionBonus = 1.3f, // Heresy = crime
            SchismRisk = 0.15f,           // Internal priesthood conflict
            StrengthNote = "High religious unity; priests dominate conversion",
            WeaknessNote = "Low tolerance, easy schism trigger"
        },
        [GovernmentType.NobleCouncil] = new()
        {
            Type = GovernmentType.NobleCouncil,
            PolicySpeed = 0.7f,         // Slow — council must agree
            ReligiousUnityBonus = 0.1f, // Nobles compete on doctrine
            MilitaryEfficiency = 1.1f,
            EconomyEfficiency = 1.2f,
            CrimeSuppressionBonus = 0.7f,
            SchismRisk = 0.20f,         // Factional religious conflict
            StrengthNote = "Regional stability and economic focus",
            WeaknessNote = "Factional conflict slows policy"
        },
        [GovernmentType.TribalClan] = new()
        {
            Type = GovernmentType.TribalClan,
            PolicySpeed = 1.2f,
            ReligiousUnityBonus = 0.4f, // Chief/shaman authority
            MilitaryEfficiency = 1.4f,
            EconomyEfficiency = 0.7f,
            CrimeSuppressionBonus = 1.0f,
            SchismRisk = 0.08f,
            StrengthNote = "High loyalty and war readiness",
            WeaknessNote = "Weak bureaucracy and infrastructure"
        },
        [GovernmentType.MerchantState] = new()
        {
            Type = GovernmentType.MerchantState,
            PolicySpeed = 0.9f,
            ReligiousUnityBonus = 0.0f, // Faith follows profit
            MilitaryEfficiency = 0.8f,
            EconomyEfficiency = 1.5f,
            CrimeSuppressionBonus = 0.5f, // Less strict
            SchismRisk = 0.10f,
            StrengthNote = "Wealth and trade enable rapid growth",
            WeaknessNote = "Low military unity; faith is pragmatic"
        },
        [GovernmentType.MonsterHorde] = new()
        {
            Type = GovernmentType.MonsterHorde,
            PolicySpeed = 1.8f,         // Alpha decides instantly
            ReligiousUnityBonus = 0.2f,
            MilitaryEfficiency = 1.8f,
            EconomyEfficiency = 0.5f,
            CrimeSuppressionBonus = 0.3f,
            SchismRisk = 0.03f,         // Fear unifies
            StrengthNote = "Rapid expansion; strength gods spread fast",
            WeaknessNote = "No diplomacy; destroys own economy"
        },
    };

    public GovernmentService(ICivilizationRepository civRepo, ILogger<GovernmentService> logger)
    {
        _civRepo = civRepo;
        _logger = logger;
    }

    public GovernmentBehavior GetBehavior(GovernmentType gov) => Behaviors[gov];

    /// <summary>
    /// Government evolves based on civ state:
    /// Tribal → Monarchy (when population grows)
    /// Monarchy → NobleCouncil (when stability falls)
    /// Theocracy possible when ReligiousUnity > 80
    /// MonsterHorde for orc/beastfolk military civs
    /// </summary>
    public async Task<GovernmentType> EvolveGovernmentAsync(CivilizationDocument civ)
    {
        var old = civ.Government;
        GovernmentType next = civ.Government;

        switch (civ.Government)
        {
            case GovernmentType.TribalClan when civ.Population > 500 && civ.Economy > 40:
                next = GovernmentType.Monarchy;
                break;

            case GovernmentType.Monarchy when civ.Stability < 30 && civ.Economy > 60:
                // Nobles gain power when king is weak but civ is wealthy
                next = _rng.NextDouble() < 0.4 ? GovernmentType.NobleCouncil : GovernmentType.Monarchy;
                break;

            case GovernmentType.Monarchy when civ.ReligiousUnity > 80 && civ.AiMemory.GodTrustLevel > 70:
                // Religion becomes so dominant it takes over
                next = _rng.NextDouble() < 0.25 ? GovernmentType.Theocracy : GovernmentType.Monarchy;
                break;

            case GovernmentType.NobleCouncil when civ.Stability < 20:
                // Crisis → strong man takes over
                next = _rng.NextDouble() < 0.5 ? GovernmentType.Monarchy : GovernmentType.TribalClan;
                break;
        }

        // Race-specific overrides
        if ((civ.PrimaryRace == RaceType.Orc || civ.PrimaryRace == RaceType.Beastfolk)
            && civ.Military > 80 && _rng.NextDouble() < 0.1)
            next = GovernmentType.MonsterHorde;

        if (next != old)
        {
            civ.Government = next;
            await _civRepo.UpdateAsync(civ);
            _logger.LogInformation("Civ {Name} government: {Old} → {New}", civ.Name, old, next);
        }
        return next;
    }

    /// <summary>
    /// Apply government modifiers each tick to civ stats.
    /// Theocracy: boosts ReligiousUnity. MerchantState: boosts Economy.
    /// MonsterHorde: constant military growth but economy decays.
    /// </summary>
    public async Task ApplyGovernmentModifiersAsync(CivilizationDocument civ, long tick)
    {
        var b = GetBehavior(civ.Government);
        bool changed = false;

        // Economy
        if (b.EconomyEfficiency != 1f)
        {
            civ.Economy *= b.EconomyEfficiency;
            civ.Economy = MathF.Clamp(civ.Economy, 0f, 200f);
            changed = true;
        }

        // Religious unity bonus
        if (b.ReligiousUnityBonus > 0f && tick % 10 == 0)
        {
            civ.ReligiousUnity = MathF.Min(100f, civ.ReligiousUnity + b.ReligiousUnityBonus);
            changed = true;
        }

        // Theocracy: heresy suppression boosts stability slightly
        if (civ.Government == GovernmentType.Theocracy)
        {
            civ.Stability = MathF.Min(100f, civ.Stability + 0.1f);
            changed = true;
        }

        // MonsterHorde: economy decay, military growth
        if (civ.Government == GovernmentType.MonsterHorde && tick % 5 == 0)
        {
            civ.Economy   = MathF.Max(0f, civ.Economy - 1f);
            civ.Military  = MathF.Min(200f, civ.Military + 0.5f);
            changed = true;
        }

        // Merchant State: happiness drives conversion
        if (civ.Government == GovernmentType.MerchantState && tick % 20 == 0)
        {
            civ.Happiness = MathF.Min(100f, civ.Happiness + civ.Economy * 0.01f);
            changed = true;
        }

        if (changed) await _civRepo.UpdateAsync(civ);
    }

    public float GetReligionSpreadModifier(GovernmentType gov)
    {
        var b = GetBehavior(gov);
        // Theocracy spreads religion fastest; MerchantState is neutral
        return 1f + b.ReligiousUnityBonus;
    }

    public float GetRoyalConversionImpact(GovernmentType gov)
    {
        // If the king/chief converts, how much does it affect the civ?
        return gov switch
        {
            GovernmentType.Monarchy      => 0.8f,  // King conversion = huge impact
            GovernmentType.Theocracy     => 1.0f,  // High priest = maximum impact
            GovernmentType.TribalClan    => 0.7f,  // Chief matters a lot
            GovernmentType.NobleCouncil  => 0.4f,  // Nobles share power
            GovernmentType.MerchantState => 0.3f,  // Money matters more
            GovernmentType.MonsterHorde  => 0.5f,
            _ => 0.5f
        };
    }

    public float GetRebellionRisk(CivilizationDocument civ)
    {
        float base_risk = 0f;

        // Core factors from GDD v1.0: Low Stability × Noble Ambition × Food × Religion × Crime × Fear
        if (civ.Stability < 30) base_risk += (30f - civ.Stability) * 0.01f;
        if (civ.Food < 20)      base_risk += 0.15f; // Famine = rebellion
        if (civ.ReligiousUnity < 20) base_risk += 0.10f;

        // Government modifier
        base_risk *= civ.Government switch
        {
            GovernmentType.NobleCouncil => 1.3f,  // Factional conflict
            GovernmentType.Monarchy     => 0.9f,
            GovernmentType.Theocracy    => 0.7f,  // Religion keeps people in line
            GovernmentType.MonsterHorde => 0.5f,  // Fear unifies
            GovernmentType.TribalClan   => 0.8f,
            _ => 1.0f
        };

        return MathF.Clamp(base_risk, 0f, 0.95f);
    }
}
