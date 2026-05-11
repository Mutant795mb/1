namespace LivingWorld.Generation;

using LivingWorld.Core;

/// <summary>
/// Immutable resource layer.
/// Contains natural resource distribution for each grid cell.
/// </summary>
public sealed class ResourceLayer : IGenerationLayer
{
    public string LayerId => "resources";
    public IReadOnlyList<string> Dependencies => new[] { "height", "erosion" };
    public int Width { get; }
    public int Height { get; }
    
    /// <summary>
    /// Resource data for each cell.
    /// </summary>
    public readonly CellResources[,] ResourceMap;
    
    public ResourceLayer(int width, int height, CellResources[,] resourceMap)
    {
        Width = width;
        Height = height;
        ResourceMap = resourceMap;
    }
    
    public CellResources GetResources(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return CellResources.Empty;
        return ResourceMap[x, y];
    }
}

/// <summary>
/// Resource types available in the world.
/// </summary>
[Flags]
public enum ResourceType
{
    None = 0,
    Wood = 1 << 0,
    Stone = 1 << 1,
    Iron = 1 << 2,
    Copper = 1 << 3,
    Gold = 1 << 4,
    Coal = 1 << 5,
    Oil = 1 << 6,
    Gems = 1 << 7,
    Fish = 1 << 8,
    Salt = 1 << 9,
    Clay = 1 << 10
}

/// <summary>
/// Resource quantities and types in a single cell.
/// </summary>
public readonly struct CellResources
{
    public readonly ResourceType Types;
    public readonly byte WoodAmount;
    public readonly byte StoneAmount;
    public readonly byte MetalAmount;
    public readonly byte PreciousAmount;
    public readonly byte FoodAmount;
    
    public bool HasResource(ResourceType type) => (Types & type) != ResourceType.None;
    
    public static CellResources Empty => new(0, 0, 0, 0, 0);
    
    private CellResources(ResourceType types, byte wood, byte stone, byte metal, byte precious, byte food = 0)
    {
        Types = types;
        WoodAmount = wood;
        StoneAmount = stone;
        MetalAmount = metal;
        PreciousAmount = precious;
        FoodAmount = food;
    }
    
    public static CellResources Create(
        ResourceType types,
        byte wood = 0,
        byte stone = 0,
        byte metal = 0,
        byte precious = 0,
        byte food = 0)
    {
        return new CellResources(types, wood, stone, metal, precious, food);
    }
}

/// <summary>
/// Generates natural resource distribution based on geology and erosion.
/// Resources are placed deterministically based on terrain characteristics.
/// </summary>
public sealed class ResourceGenerator : IGenerationModule
{
    private readonly int _width;
    private readonly int _height;
    
    public string OutputLayerId => "resources";
    public IReadOnlyList<string> RequiredInputLayers => new[] { "height", "erosion" };
    
    public ResourceGenerator(int width, int height)
    {
        _width = width;
        _height = height;
    }
    
    public IGenerationLayer Generate(ulong worldSeed, IReadOnlyDictionary<string, IGenerationLayer> inputLayers)
    {
        var heightLayer = (HeightLayer)inputLayers["height"];
        var erosionLayer = (ErosionLayer)inputLayers["erosion"];
        
        var resourceMap = new CellResources[_width, _height];
        var rng = new DeterministicRng(worldSeed + 10000);
        
        // Generate mineral deposits using noise-based clustering
        var ironNoise = GenerateResourceNoise(rng, 0.03f, 3);
        var copperNoise = GenerateResourceNoise(rng, 0.04f, 3);
        var goldNoise = GenerateResourceNoise(rng, 0.015f, 4);
        var coalNoise = GenerateResourceNoise(rng, 0.035f, 3);
        var oilNoise = GenerateResourceNoise(rng, 0.02f, 4);
        var gemNoise = GenerateResourceNoise(rng, 0.01f, 5);
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                float height = heightLayer.GetNormalizedHeight(x, y);
                float erosion = erosionLayer.GetErosion(x, y);
                
                ResourceType types = ResourceType.None;
                byte wood = 0, stone = 0, metal = 0, precious = 0, food = 0;
                
                // Forest resources (based on biome implicitly via height/moisture proxy)
                if (height > 0.35f && height < 0.7f)
                {
                    var forestRng = new DeterministicRng(worldSeed + (ulong)(x * 73856093 ^ y * 19349663));
                    if (forestRng.NextDouble() < 0.4)
                    {
                        types |= ResourceType.Wood;
                        wood = (byte)(20 + forestRng.NextInt(30, 80));
                    }
                }
                
                // Stone is common in mountains and hills
                if (height > 0.5f)
                {
                    types |= ResourceType.Stone;
                    stone = (byte)(30 + (height - 0.5f) * 100);
                }
                
                // Metals in mountainous areas with some erosion (exposed veins)
                if (height > 0.55f && erosion > 0.3f)
                {
                    if (ironNoise[x, y] > 0.5f)
                    {
                        types |= ResourceType.Iron;
                        metal = (byte)(20 + ironNoise[x, y] * 60);
                    }
                    
                    if (copperNoise[x, y] > 0.5f)
                    {
                        types |= ResourceType.Copper;
                        metal = (byte)MathF.Max(metal, (byte)(15 + copperNoise[x, y] * 50));
                    }
                }
                
                // Precious metals in high mountains with high erosion
                if (height > 0.7f && erosion > 0.5f)
                {
                    if (goldNoise[x, y] > 0.6f)
                    {
                        types |= ResourceType.Gold;
                        precious = (byte)(5 + goldNoise[x, y] * 20);
                    }
                    
                    if (gemNoise[x, y] > 0.65f)
                    {
                        types |= ResourceType.Gems;
                        precious = (byte)MathF.Max(precious, (byte)(3 + gemNoise[x, y] * 15));
                    }
                }
                
                // Coal in mid-altitude areas
                if (height > 0.4f && height < 0.75f && coalNoise[x, y] > 0.55f)
                {
                    types |= ResourceType.Coal;
                    metal = (byte)MathF.Max(metal, (byte)(15 + coalNoise[x, y] * 45));
                }
                
                // Oil in low-lying sedimentary basins
                if (height > 0.3f && height < 0.45f && oilNoise[x, y] > 0.6f)
                {
                    types |= ResourceType.Oil;
                    metal = (byte)MathF.Max(metal, (byte)(10 + oilNoise[x, y] * 35));
                }
                
                // Fish in water bodies
                if (height < 0.3f)
                {
                    var fishRng = new DeterministicRng(worldSeed + (ulong)(x * 73856093 ^ y * 19349663) + 2000);
                    if (fishRng.NextDouble() < 0.5)
                    {
                        types |= ResourceType.Fish;
                        food = (byte)(10 + fishRng.NextInt(20, 50));
                    }
                }
                
                // Salt in coastal areas
                if (height >= 0.3f && height < 0.38f)
                {
                    var saltRng = new DeterministicRng(worldSeed + (ulong)(x * 73856093 ^ y * 19349663) + 3000);
                    if (saltRng.NextDouble() < 0.3)
                    {
                        types |= ResourceType.Salt;
                        stone = (byte)MathF.Max(stone, (byte)(5 + saltRng.NextInt(10, 30)));
                    }
                }
                
                // Clay in river valleys (moderate erosion areas)
                if (height > 0.32f && height < 0.5f && erosion > 0.4f && erosion < 0.7f)
                {
                    var clayRng = new DeterministicRng(worldSeed + (ulong)(x * 73856093 ^ y * 19349663) + 4000);
                    if (clayRng.NextDouble() < 0.35)
                    {
                        types |= ResourceType.Clay;
                        stone = (byte)MathF.Max(stone, (byte)(8 + clayRng.NextInt(15, 40)));
                    }
                }
                
                resourceMap[x, y] = CellResources.Create(types, wood, stone, metal, precious, food);
            }
        }
        
        return new ResourceLayer(_width, _height, resourceMap);
    }
    
    /// <summary>
    /// Generate clustered noise for resource distribution.
    /// </summary>
    private float[,] GenerateResourceNoise(DeterministicRng rng, float frequency, int octaves)
    {
        var permutation = new int[512];
        var p = Enumerable.Range(0, 256).ToArray();
        
        for (int i = 255; i > 0; i--)
        {
            int j = rng.NextInt(0, i);
            (p[i], p[j]) = (p[j], p[i]);
        }
        
        Array.Copy(p, 0, permutation, 0, 256);
        Array.Copy(p, 0, permutation, 256, 256);
        
        var noise = new float[_width, _height];
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                float value = 0f;
                float amplitude = 1f;
                float freq = frequency;
                float maxValue = 0f;
                
                for (int o = 0; o < octaves; o++)
                {
                    value += amplitude * SimpleNoise(x * freq, y * freq, permutation);
                    maxValue += amplitude;
                    amplitude *= 0.5f;
                    freq *= 2f;
                }
                
                noise[x, y] = value / maxValue;
            }
        }
        
        return noise;
    }
    
    private static float SimpleNoise(float x, float y, int[] permutation)
    {
        int xi = (int)Math.Floor(x) & 255;
        int yi = (int)Math.Floor(y) & 255;
        
        int hash = permutation[permutation[xi] + yi];
        return (hash % 100) / 100f;
    }
}
