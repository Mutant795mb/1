namespace LivingWorld.Generation;

using LivingWorld.Core;

/// <summary>
/// Immutable fertility layer.
/// Contains soil fertility data for each grid cell (normalized 0-1, barren-fertile).
/// </summary>
public sealed class FertilityLayer : IGenerationLayer
{
    public string LayerId => "fertility";
    public IReadOnlyList<string> Dependencies => new[] { "height", "temperature", "moisture", "erosion" };
    public int Width { get; }
    public int Height { get; }
    
    /// <summary>
    /// Fertility values normalized to [0, 1] range.
    /// </summary>
    public readonly float[,] FertilityMap;
    
    public FertilityLayer(int width, int height, float[,] fertilityMap)
    {
        Width = width;
        Height = height;
        FertilityMap = fertilityMap;
    }
    
    public float GetFertility(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0f;
        return FertilityMap[x, y];
    }
}

/// <summary>
/// Generates soil fertility based on climate, erosion, and terrain.
/// Fertility is highest in warm, moist areas with moderate erosion (sediment deposition).
/// </summary>
public sealed class FertilityGenerator : IGenerationModule
{
    private readonly int _width;
    private readonly int _height;
    
    public string OutputLayerId => "fertility";
    public IReadOnlyList<string> RequiredInputLayers => new[] { "height", "temperature", "moisture", "erosion" };
    
    public FertilityGenerator(int width, int height)
    {
        _width = width;
        _height = height;
    }
    
    public IGenerationLayer Generate(ulong worldSeed, IReadOnlyDictionary<string, IGenerationLayer> inputLayers)
    {
        var heightLayer = (HeightLayer)inputLayers["height"];
        var tempLayer = (TemperatureLayer)inputLayers["temperature"];
        var moistureLayer = (MoistureLayer)inputLayers["moisture"];
        var erosionLayer = (ErosionLayer)inputLayers["erosion"];
        
        var fertilityMap = new float[_width, _height];
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                float height = heightLayer.GetNormalizedHeight(x, y);
                float temp = tempLayer.GetTemperature(x, y);
                float moisture = moistureLayer.GetMoisture(x, y);
                float erosion = erosionLayer.GetErosion(x, y);
                
                // Base fertility from temperature and moisture (optimal: warm + moist)
                float tempFactor = 1f - MathF.Abs(temp - 0.6f) * 1.5f; // Optimal around 0.6
                float moistureFactor = 1f - MathF.Abs(moisture - 0.5f) * 1.5f; // Optimal around 0.5
                
                float baseFertility = MathF.Max(0f, (tempFactor + moistureFactor) / 2f);
                
                // Erosion effect: moderate erosion adds nutrients, too much removes them
                float erosionFactor = 1f - MathF.Abs(erosion - 0.4f) * 1.2f; // Optimal around 0.4
                erosionFactor = MathF.Max(0f, erosionFactor);
                
                // Altitude penalty: very high altitudes have poor soil
                float altitudeFactor = height > 0.7f ? 1f - (height - 0.7f) * 2f : 1f;
                altitudeFactor = MathF.Max(0.2f, altitudeFactor);
                
                // Combine factors
                float fertility = baseFertility * (0.5f + erosionFactor * 0.5f) * altitudeFactor;
                
                // Water bodies have zero fertility (cannot farm)
                if (height < 0.3f)
                    fertility = 0f;
                
                // Beaches have low fertility (sandy soil)
                if (height >= 0.3f && height < 0.35f)
                    fertility *= 0.3f;
                
                fertilityMap[x, y] = MathF.Max(0f, MathF.Min(1f, fertility));
            }
        }
        
        return new FertilityLayer(_width, _height, fertilityMap);
    }
}
