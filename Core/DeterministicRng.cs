namespace LivingWorld.Core;

/// <summary>
/// Deterministic random number generator for reproducible world generation.
/// Uses Linear Congruential Generator (LCG) algorithm.
/// </summary>
public sealed class DeterministicRng
{
    private ulong _state;

    public DeterministicRng(ulong seed)
    {
        _state = seed == 0 ? 1UL : seed;
    }

    /// <summary>
    /// Generate next random ulong value.
    /// </summary>
    public ulong NextUlong()
    {
        // LCG parameters (Numerical Recipes)
        _state = _state * 6364136223846793005UL + 1442695040888963407UL;
        return _state;
    }

    /// <summary>
    /// Generate random double in range [0, 1).
    /// </summary>
    public double NextDouble()
    {
        return (NextUlong() >> 11) * (1.0 / 9007199254740992.0);
    }

    /// <summary>
    /// Generate random int in range [min, max].
    /// </summary>
    public int NextInt(int min, int max)
    {
        if (min > max) throw new ArgumentException("min must be <= max");
        if (min == max) return min;
        
        long range = (long)max - min + 1;
        return (int)(NextDouble() * range) + min;
    }

    /// <summary>
    /// Generate random float in range [min, max).
    /// </summary>
    public float NextFloat(float min, float max)
    {
        return (float)(NextDouble() * (max - min) + min);
    }

    /// <summary>
    /// Create a new RNG instance with a seed derived from current state.
    /// Useful for creating child generators without affecting parent state.
    /// </summary>
    public DeterministicRng CreateChild()
    {
        return new DeterministicRng(NextUlong());
    }
}
