namespace LivingWorld.Generation;

using LivingWorld.Core;

/// <summary>
/// Generates height map using multi-octave Perlin noise.
/// This is the foundation layer that all other layers depend on.
/// </summary>
public sealed class HeightMapGenerator : IGenerationModule
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _octaves;
    private readonly float _persistence;
    private readonly float _scale;

    public string OutputLayerId => "height";
    public IReadOnlyList<string> RequiredInputLayers => Array.Empty<string>();

    /// <summary>
    /// Create a new height map generator.
    /// </summary>
    /// <param name="width">World width in cells.</param>
    /// <param name="height">World height in cells.</param>
    /// <param name="octaves">Number of noise octaves (detail level).</param>
    /// <param name="persistence">Amplitude decrease per octave.</param>
    /// <param name="scale">Noise scale (lower = more zoomed in).</param>
    public HeightMapGenerator(int width, int height, int octaves = 6, float persistence = 0.5f, float scale = 0.02f)
    {
        _width = width;
        _height = height;
        _octaves = octaves;
        _persistence = persistence;
        _scale = scale;
    }

    public IGenerationLayer Generate(ulong worldSeed, IReadOnlyDictionary<string, IGenerationLayer> inputLayers)
    {
        var rng = new DeterministicRng(worldSeed);
        var heightMap = new float[_width, _height];

        // Generate permutation table for deterministic noise
        var permutation = GeneratePermutation(rng);

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                heightMap[x, y] = GenerateMultiOctaveNoise(x, y, permutation);
            }
        }

        return new HeightLayer(_width, _height, heightMap);
    }

    private int[] GeneratePermutation(DeterministicRng rng)
    {
        var p = Enumerable.Range(0, 256).ToArray();
        
        // Fisher-Yates shuffle with deterministic RNG
        for (int i = 255; i > 0; i--)
        {
            int j = rng.NextInt(0, i);
            (p[i], p[j]) = (p[j], p[i]);
        }
        
        // Duplicate for overflow handling
        var result = new int[512];
        Array.Copy(p, 0, result, 0, 256);
        Array.Copy(p, 0, result, 256, 256);
        
        return result;
    }

    private float GenerateMultiOctaveNoise(int x, int y, int[] permutation)
    {
        float value = 0f;
        float amplitude = 1f;
        float frequency = _scale;
        float maxValue = 0f;

        for (int i = 0; i < _octaves; i++)
        {
            value += amplitude * PerlinNoise(x * frequency, y * frequency, permutation);
            maxValue += amplitude;
            amplitude *= _persistence;
            frequency *= 2f;
        }

        return value / maxValue;
    }

    private static float PerlinNoise(float x, float y, int[] permutation)
    {
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;
        
        float xf = x - MathF.Floor(x);
        float yf = y - MathF.Floor(y);

        // Smoothstep interpolation
        float u = Smoothstep(xf);
        float v = Smoothstep(yf);

        // Hash coordinates
        int aa = permutation[permutation[xi] + yi];
        int ab = permutation[permutation[xi] + yi + 1];
        int ba = permutation[permutation[xi + 1] + yi];
        int bb = permutation[permutation[xi + 1] + yi + 1];

        // Gradient vectors and dot products
        float x1 = Grad(aa, xf, yf);
        float x2 = Grad(ba, xf - 1, yf);
        float y1 = Grad(ab, xf, yf - 1);
        float y2 = Grad(bb, xf - 1, yf - 1);

        // Interpolate
        float r1 = Lerp(u, x1, x2);
        float r2 = Lerp(u, y1, y2);
        
        return Lerp(v, r1, r2);
    }

    private static float Grad(int hash, float x, float y)
    {
        int h = hash & 7;
        float u = h < 4 ? x : y;
        float v = h < 4 ? y : x;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    private static float Smoothstep(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    private static float Lerp(float t, float a, float b)
    {
        return a + t * (b - a);
    }
}
