using WorldFaith.Server.Models;
using WorldFaith.Server.Services.Common;

namespace WorldFaith.Server.Services.NPC.Dynasty;

// Mixes ordinary genetic stats (Dynasty Spec §14), kept separate from divine
// bloodline inheritance: a child's genes are the parents' average plus variance.
public interface IGeneMixingService
{
    NpcGeneProfile Mix(NpcGeneProfile a, NpcGeneProfile b);
}

public class GeneMixingService : IGeneMixingService
{
    private readonly IRandomService _rng;

    public GeneMixingService(IRandomService rng) => _rng = rng;

    public NpcGeneProfile Mix(NpcGeneProfile a, NpcGeneProfile b) => new()
    {
        Height               = MixStat(a.Height, b.Height),
        Strength             = MixStat(a.Strength, b.Strength),
        Intelligence         = MixStat(a.Intelligence, b.Intelligence),
        ManaCapacity         = MixStat(a.ManaCapacity, b.ManaCapacity),
        Fertility            = MixStat(a.Fertility, b.Fertility),
        Longevity            = MixStat(a.Longevity, b.Longevity),
        DivineAffinity       = MixStat(a.DivineAffinity, b.DivineAffinity),
        CorruptionResistance = MixStat(a.CorruptionResistance, b.CorruptionResistance),
        MutationChance       = MixStat(a.MutationChance, b.MutationChance),
    };

    private float MixStat(float x, float y)
    {
        float average = (x + y) * 0.5f;
        float variance = -5f + _rng.NextFloat() * 10f;
        return Math.Clamp(average + variance, 0f, 100f);
    }
}
