namespace WorldFaith.Server.Services.Common;

/// <summary>
/// Abstraction over randomness so simulation/decision services can be tested
/// deterministically (NPC Master Spec §14: "Use deterministic random seeds
/// for simulation tests").
/// </summary>
public interface IRandomService
{
    /// <summary>Uniform float in [0, 1).</summary>
    float NextFloat();

    /// <summary>Uniform double in [0, 1).</summary>
    double NextDouble();

    /// <summary>Integer in [minInclusive, maxExclusive).</summary>
    int Next(int minInclusive, int maxExclusive);
}

public class RandomService : IRandomService
{
    private readonly Random _rng;

    public RandomService() => _rng = new Random();

    /// <summary>Seeded constructor for deterministic tests.</summary>
    public RandomService(int seed) => _rng = new Random(seed);

    public float NextFloat() => (float)_rng.NextDouble();
    public double NextDouble() => _rng.NextDouble();
    public int Next(int minInclusive, int maxExclusive) => _rng.Next(minInclusive, maxExclusive);
}
