using WorldFaith.Shared.Enums;

namespace WorldFaith.Server.Services.Gameplay;

// ─── Soft Population Capacity (Gameplay Spec §3.2) ────────────
// The world uses a soft capacity from food, safe land, housing, health and
// stability. When population exceeds it, people migrate, compete, or decline —
// they never multiply without limit.

/// <summary>Each factor 0-100 = how well the location supplies that need.</summary>
public record CapacityFactors(float Food, float SafeLand, float Housing, float Health, float Stability);

public interface IPopulationPressureService
{
    /// <summary>Soft capacity = base capacity scaled by the location's factors (the scarcest matters most).</summary>
    float ComputeSoftCapacity(CapacityFactors f, float baseCapacity);

    PopulationOutcome Evaluate(int population, float softCapacity);
}

public class PopulationPressureService : IPopulationPressureService
{
    public float ComputeSoftCapacity(CapacityFactors f, float baseCapacity)
    {
        float avg = (f.Food + f.SafeLand + f.Housing + f.Health + f.Stability) / 5f / 100f;
        float min = MathF.Min(f.Food, MathF.Min(f.SafeLand, MathF.Min(f.Housing,
                        MathF.Min(f.Health, f.Stability)))) / 100f;
        // The scarcest resource caps growth (Liebig-style), blended with the average.
        float factor = Math.Clamp(avg * 0.5f + min * 0.5f, 0f, 1f);
        return MathF.Max(0f, baseCapacity) * factor;
    }

    public PopulationOutcome Evaluate(int population, float softCapacity)
    {
        if (softCapacity <= 0f) return PopulationOutcome.Declining;

        float ratio = population / softCapacity;
        return ratio switch
        {
            < 0.70f => PopulationOutcome.Growing,
            < 1.00f => PopulationOutcome.Stable,
            < 1.25f => PopulationOutcome.Migrating,  // over capacity → leave first
            < 1.60f => PopulationOutcome.Competing,  // then fight over resources
            _       => PopulationOutcome.Declining,  // then conditions worsen
        };
    }
}
