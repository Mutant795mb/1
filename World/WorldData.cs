namespace LivingWorld.World;

using LivingWorld.Core;
using LivingWorld.Generation;

/// <summary>
/// Contains all generation layers for the world.
/// This is the canonical world data that rendering and simulation read from.
/// </summary>
public sealed class WorldData
{
    public int Width { get; }
    public int Height { get; }
    public ulong Seed { get; }
    
    private readonly Dictionary<string, IGenerationLayer> _layers = new();
    
    public WorldData(int width, int height, ulong seed)
    {
        Width = width;
        Height = height;
        Seed = seed;
    }
    
    public void AddLayer(IGenerationLayer layer)
    {
        _layers[layer.LayerId] = layer;
    }
    
    public T GetLayer<T>(string layerId) where T : class, IGenerationLayer
    {
        if (_layers.TryGetValue(layerId, out var layer))
        {
            return layer as T ?? throw new InvalidCastException($"Layer {layerId} is not of type {typeof(T).Name}");
        }
        throw new KeyNotFoundException($"Layer {layerId} not found");
    }
    
    public bool HasLayer(string layerId) => _layers.ContainsKey(layerId);
    
    public IReadOnlyDictionary<string, IGenerationLayer> AllLayers => _layers;
    
    /// <summary>
    /// Get height at coordinates.
    /// </summary>
    public float GetHeight(int x, int y)
    {
        if (TryGetLayer<HeightLayer>("height", out var heightLayer))
            return heightLayer.GetHeight(x, y);
        return 0f;
    }
    
    /// <summary>
    /// Get biome at coordinates.
    /// </summary>
    public BiomeType GetBiome(int x, int y)
    {
        if (TryGetLayer<BiomeLayer>("biome", out var biomeLayer))
            return biomeLayer.GetBiome(x, y);
        return BiomeType.Ocean;
    }
    
    /// <summary>
    /// Try to get a layer, returns false if not found.
    /// </summary>
    public bool TryGetLayer<T>(string layerId, out T? layer) where T : class, IGenerationLayer
    {
        if (_layers.TryGetValue(layerId, out var baseLayer))
        {
            layer = baseLayer as T;
            return layer != null;
        }
        layer = null;
        return false;
    }
    
    /// <summary>
    /// Get temperature at coordinates.
    /// </summary>
    public float GetTemperature(int x, int y)
    {
        if (TryGetLayer<TemperatureLayer>("temperature", out var tempLayer))
            return tempLayer.GetTemperature(x, y);
        return 0f;
    }
    
    /// <summary>
    /// Get moisture at coordinates.
    /// </summary>
    public float GetMoisture(int x, int y)
    {
        if (TryGetLayer<MoistureLayer>("moisture", out var moistureLayer))
            return moistureLayer.GetMoisture(x, y);
        return 0f;
    }
    
    /// <summary>
    /// Get fertility at coordinates (returns 0 if layer not found).
    /// </summary>
    public float GetFertility(int x, int y)
    {
        if (TryGetLayer<FertilityLayer>("fertility", out var fertilityLayer))
            return fertilityLayer.GetFertility(x, y);
        return 0f;
    }
    
    /// <summary>
    /// Get erosion at coordinates (returns 0 if layer not found).
    /// </summary>
    public float GetErosion(int x, int y)
    {
        if (TryGetLayer<ErosionLayer>("erosion", out var erosionLayer))
            return erosionLayer.GetErosion(x, y);
        return 0f;
    }
    
    /// <summary>
    /// Get biomes array (for direct access).
    /// </summary>
    public BiomeType[,] Biomes
    {
        get
        {
            if (TryGetLayer<BiomeLayer>("biome", out var biomeLayer))
                return biomeLayer.BiomeMap;
            return new BiomeType[Width, Height];
        }
    }
    
    /// <summary>
    /// Get height array (for direct access).
    /// </summary>
    public float[,] HeightMap
    {
        get
        {
            if (TryGetLayer<HeightLayer>("height", out var heightLayer))
                return heightLayer.HeightMap;
            return new float[Width, Height];
        }
    }
    
    /// <summary>
    /// Get temperature array (for direct access).
    /// </summary>
    public float[,] Temperature
    {
        get
        {
            if (TryGetLayer<TemperatureLayer>("temperature", out var tempLayer))
                return tempLayer.TemperatureMap;
            return new float[Width, Height];
        }
    }
    
    /// <summary>
    /// Get moisture array (for direct access).
    /// </summary>
    public float[,] Moisture
    {
        get
        {
            if (TryGetLayer<MoistureLayer>("moisture", out var moistureLayer))
                return moistureLayer.MoistureMap;
            return new float[Width, Height];
        }
    }
    
    /// <summary>
    /// Get fertility array (for direct access).
    /// </summary>
    public float[,] Fertility
    {
        get
        {
            if (TryGetLayer<FertilityLayer>("fertility", out var fertilityLayer))
                return fertilityLayer.FertilityMap;
            return new float[Width, Height];
        }
    }
    
    /// <summary>
    /// Get erosion array (for direct access).
    /// </summary>
    public float[,] Erosion
    {
        get
        {
            if (TryGetLayer<ErosionLayer>("erosion", out var erosionLayer))
                return erosionLayer.ErosionMap;
            return new float[Width, Height];
        }
    }
}

/// <summary>
/// Orchestrates the world generation pipeline.
/// Ensures layers are generated in correct dependency order.
/// </summary>
public sealed class WorldGenerator
{
    private readonly List<IGenerationModule> _modules = new();
    private readonly Dictionary<string, IGenerationLayer> _generatedLayers = new();
    
    public void RegisterModule(IGenerationModule module)
    {
        _modules.Add(module);
    }
    
    /// <summary>
    /// Generate complete world data from seed.
    /// </summary>
    public WorldData Generate(int width, int height, ulong seed)
    {
        _generatedLayers.Clear();
        var worldData = new WorldData(width, height, seed);
        
        // Sort modules by dependencies (topological sort)
        var sortedModules = TopologicalSort(_modules);
        
        foreach (var module in sortedModules)
        {
            // Check if all dependencies are satisfied
            foreach (var dep in module.RequiredInputLayers)
            {
                if (!_generatedLayers.ContainsKey(dep))
                {
                    throw new InvalidOperationException(
                        $"Module {module.OutputLayerId} requires layer {dep} which hasn't been generated yet.");
                }
            }
            
            // Generate the layer
            var layer = module.Generate(seed, _generatedLayers);
            _generatedLayers[layer.LayerId] = layer;
            worldData.AddLayer(layer);
        }
        
        return worldData;
    }
    
    private List<IGenerationModule> TopologicalSort(List<IGenerationModule> modules)
    {
        var result = new List<IGenerationModule>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();
        
        void Visit(IGenerationModule module)
        {
            if (visited.Contains(module.OutputLayerId))
                return;
            
            if (visiting.Contains(module.OutputLayerId))
                throw new InvalidOperationException($"Circular dependency detected involving {module.OutputLayerId}");
            
            visiting.Add(module.OutputLayerId);
            
            // Visit dependencies first
            foreach (var depId in module.RequiredInputLayers)
            {
                var depModule = modules.FirstOrDefault(m => m.OutputLayerId == depId);
                if (depModule != null)
                {
                    Visit(depModule);
                }
            }
            
            visiting.Remove(module.OutputLayerId);
            visited.Add(module.OutputLayerId);
            result.Add(module);
        }
        
        foreach (var module in modules)
        {
            Visit(module);
        }
        
        return result;
    }
}
