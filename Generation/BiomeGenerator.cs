namespace LivingWorld.Generation;

using LivingWorld.Core;

/// <summary>
/// Biome types in the world.
/// </summary>
public enum BiomeType
{
    Ocean,
    Beach,
    Desert,
    Grassland,
    SeasonalForest,
    Forest,
    Rainforest,
    Savanna,
    Taiga,
    Tundra,
    Mountain,
    SnowMountain,
    Swamp,
    Lake
}

/// <summary>
/// Immutable biome layer.
/// Contains biome classification for each grid cell.
/// </summary>
public sealed class BiomeLayer : IGenerationLayer
{
    public string LayerId => "biome";
    public IReadOnlyList<string> Dependencies => new[] { "height", "temperature", "moisture" };
    public int Width { get; }
    public int Height { get; }
    
    /// <summary>
    /// Biome type for each cell.
    /// </summary>
    public readonly BiomeType[,] BiomeMap;
    
    public BiomeLayer(int width, int height, BiomeType[,] biomeMap)
    {
        Width = width;
        Height = height;
        BiomeMap = biomeMap;
    }
    
    public BiomeType GetBiome(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return BiomeType.Ocean;
        return BiomeMap[x, y];
    }
}

/// <summary>
/// Generates biomes based on Whittaker diagram (temperature vs precipitation).
/// </summary>
public sealed class BiomeGenerator : IGenerationModule
{
    private readonly float _seaLevel;
    private readonly float _beachLevel;

    public string OutputLayerId => "biome";
    public IReadOnlyList<string> RequiredInputLayers => new[] { "height", "temperature", "moisture" };

    /// <summary>
    /// Create biome generator.
    /// </summary>
    /// <param name="seaLevel">Height threshold for ocean (0-1).</param>
    /// <param name="beachLevel">Height threshold for beach (0-1).</param>
    public BiomeGenerator(float seaLevel = 0.3f, float beachLevel = 0.35f)
    {
        _seaLevel = seaLevel;
        _beachLevel = beachLevel;
    }

    public IGenerationLayer Generate(ulong worldSeed, IReadOnlyDictionary<string, IGenerationLayer> inputLayers)
    {
        var heightLayer = (HeightLayer)inputLayers["height"];
        var tempLayer = (TemperatureLayer)inputLayers["temperature"];
        var moistureLayer = (MoistureLayer)inputLayers["moisture"];
        
        var biomeMap = new BiomeType[heightLayer.Width, heightLayer.Height];

        for (int y = 0; y < heightLayer.Height; y++)
        {
            for (int x = 0; x < heightLayer.Width; x++)
            {
                float height = heightLayer.GetNormalizedHeight(x, y);
                float temp = tempLayer.GetTemperature(x, y);
                float moisture = moistureLayer.GetMoisture(x, y);
                
                biomeMap[x, y] = ClassifyBiome(height, temp, moisture);
            }
        }

        return new BiomeLayer(heightLayer.Width, heightLayer.Height, biomeMap);
    }

    private BiomeType ClassifyBiome(float height, float temp, float moisture)
    {
        // Water bodies
        if (height < _seaLevel)
            return BiomeType.Ocean;
        
        if (height < _beachLevel)
            return BiomeType.Beach;
        
        // Mountains
        if (height > 0.8f)
            return temp < 0.3f ? BiomeType.SnowMountain : BiomeType.Mountain;
        
        // High altitude tundra
        if (height > 0.6f && temp < 0.4f)
            return BiomeType.Tundra;
        
        // Use Whittaker-like classification
        // Hot + Wet = Rainforest
        if (temp > 0.75f && moisture > 0.75f)
            return BiomeType.Rainforest;
        
        // Hot + Dry = Desert
        if (temp > 0.75f && moisture < 0.25f)
            return BiomeType.Desert;
        
        // Hot + Medium = Savanna
        if (temp > 0.75f && moisture >= 0.25f && moisture <= 0.75f)
            return BiomeType.Savanna;
        
        // Warm + Wet = Forest
        if (temp > 0.5f && moisture > 0.6f)
            return BiomeType.Forest;
        
        // Warm + Medium = Seasonal Forest
        if (temp > 0.5f && moisture > 0.35f && moisture <= 0.6f)
            return BiomeType.SeasonalForest;
        
        // Warm + Dry = Grassland
        if (temp > 0.5f && moisture <= 0.35f)
            return BiomeType.Grassland;
        
        // Cool + Wet = Taiga
        if (temp <= 0.5f && moisture > 0.5f)
            return BiomeType.Taiga;
        
        // Cool + Dry/Medium = Tundra or Grassland
        if (temp <= 0.5f && moisture <= 0.5f)
            return temp < 0.25f ? BiomeType.Tundra : BiomeType.Grassland;
        
        // Default
        return BiomeType.Grassland;
    }
}
