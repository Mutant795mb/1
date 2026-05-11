namespace LivingWorld.Generation;

using LivingWorld.Core;

/// <summary>
/// Immutable temperature layer.
/// Contains temperature data for each grid cell (normalized 0-1, cold-hot).
/// </summary>
public sealed class TemperatureLayer : IGenerationLayer
{
    public string LayerId => "temperature";
    public IReadOnlyList<string> Dependencies => new[] { "height" };
    public int Width { get; }
    public int Height { get; }
    
    /// <summary>
    /// Temperature values normalized to [0, 1] range (0=cold, 1=hot).
    /// </summary>
    public readonly float[,] TemperatureMap;
    
    public TemperatureLayer(int width, int height, float[,] temperatureMap)
    {
        Width = width;
        Height = height;
        TemperatureMap = temperatureMap;
    }
    
    public float GetTemperature(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0.5f;
        return TemperatureMap[x, y];
    }
}

/// <summary>
/// Generates temperature based on latitude and altitude.
/// </summary>
public sealed class TemperatureGenerator : IGenerationModule
{
    private readonly int _width;
    private readonly int _height;

    public string OutputLayerId => "temperature";
    public IReadOnlyList<string> RequiredInputLayers => new[] { "height" };

    public TemperatureGenerator(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public IGenerationLayer Generate(ulong worldSeed, IReadOnlyDictionary<string, IGenerationLayer> inputLayers)
    {
        var heightLayer = (HeightLayer)inputLayers["height"];
        var temperatureMap = new float[_width, _height];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                // Latitude effect: equator (middle) is hotter, poles are colder
                float latitudeFactor = 1f - MathF.Abs((float)y / _height - 0.5f) * 2f;
                
                // Altitude effect: higher = colder
                float heightFactor = 1f - heightLayer.GetNormalizedHeight(x, y);
                
                // Combine factors (latitude is dominant, altitude modifies)
                float temperature = latitudeFactor * 0.7f + heightFactor * 0.3f;
                
                // Add small deterministic variation
                var rng = new DeterministicRng(worldSeed + (ulong)(x * 73856093 ^ y * 19349663));
                temperature += (float)(rng.NextDouble() - 0.5) * 0.1f;
                
                // Clamp to [0, 1]
                temperatureMap[x, y] = MathF.Max(0f, MathF.Min(1f, temperature));
            }
        }

        return new TemperatureLayer(_width, _height, temperatureMap);
    }
}
