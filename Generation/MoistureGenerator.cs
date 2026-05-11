namespace LivingWorld.Generation;

using LivingWorld.Core;

/// <summary>
/// Immutable moisture/humidity layer.
/// Contains moisture data for each grid cell (normalized 0-1, dry-wet).
/// </summary>
public sealed class MoistureLayer : IGenerationLayer
{
    public string LayerId => "moisture";
    public IReadOnlyList<string> Dependencies => new[] { "height", "temperature" };
    public int Width { get; }
    public int Height { get; }
    
    /// <summary>
    /// Moisture values normalized to [0, 1] range (0=dry, 1=wet).
    /// </summary>
    public readonly float[,] MoistureMap;
    
    public MoistureLayer(int width, int height, float[,] moistureMap)
    {
        Width = width;
        Height = height;
        MoistureMap = moistureMap;
    }
    
    public float GetMoisture(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0.5f;
        return MoistureMap[x, y];
    }
}

/// <summary>
/// Generates moisture/precipitation based on temperature, altitude, and distance from water.
/// Uses a simplified rainfall model with prevailing winds.
/// </summary>
public sealed class MoistureGenerator : IGenerationModule
{
    private readonly int _width;
    private readonly int _height;

    public string OutputLayerId => "moisture";
    public IReadOnlyList<string> RequiredInputLayers => new[] { "height", "temperature" };

    public MoistureGenerator(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public IGenerationLayer Generate(ulong worldSeed, IReadOnlyDictionary<string, IGenerationLayer> inputLayers)
    {
        var heightLayer = (HeightLayer)inputLayers["height"];
        var tempLayer = (TemperatureLayer)inputLayers["temperature"];
        var moistureMap = new float[_width, _height];

        // Prevailing wind direction (eastward in this hemisphere)
        const int windDirection = 1;

        for (int y = 0; y < _height; y++)
        {
            float baseMoisture = 0.3f + 0.4f * (1f - MathF.Abs((float)y / _height - 0.5f) * 2f);
            
            for (int x = 0; x < _width; x++)
            {
                float height = heightLayer.GetNormalizedHeight(x, y);
                float temp = tempLayer.GetTemperature(x, y);
                
                // Base moisture from latitude (equator is wetter)
                float moisture = baseMoisture;
                
                // Orographic effect: windward side of mountains gets more rain
                if (x > 0)
                {
                    float prevHeight = heightLayer.GetNormalizedHeight(x - windDirection, y);
                    if (height > prevHeight && height > 0.5f)
                    {
                        moisture += (height - prevHeight) * 0.5f;
                    }
                }
                
                // Rain shadow: leeward side is drier
                if (x < _width - 1 && height < 0.3f)
                {
                    float nextHeight = heightLayer.GetNormalizedHeight(x + windDirection, y);
                    if (nextHeight > 0.6f)
                    {
                        moisture *= 0.5f;
                    }
                }
                
                // Temperature effect: warmer = more evaporation = potentially more precipitation
                moisture *= 0.5f + temp * 0.5f;
                
                // Add deterministic variation
                var rng = new DeterministicRng(worldSeed + (ulong)(x * 73856093 ^ y * 19349663) + 1000);
                moisture += (float)(rng.NextDouble() - 0.5) * 0.15f;
                
                // Clamp to [0, 1]
                moistureMap[x, y] = MathF.Max(0f, MathF.Min(1f, moisture));
            }
        }

        return new MoistureLayer(_width, _height, moistureMap);
    }
}
