namespace LivingWorld.Generation;

using LivingWorld.Core;

/// <summary>
/// Immutable erosion layer.
/// Contains erosion data for each grid cell (normalized 0-1, low-high erosion).
/// </summary>
public sealed class ErosionLayer : IGenerationLayer
{
    public string LayerId => "erosion";
    public IReadOnlyList<string> Dependencies => new[] { "height", "moisture" };
    public int Width { get; }
    public int Height { get; }
    
    /// <summary>
    /// Erosion values normalized to [0, 1] range.
    /// </summary>
    public readonly float[,] ErosionMap;
    
    public ErosionLayer(int width, int height, float[,] erosionMap)
    {
        Width = width;
        Height = height;
        ErosionMap = erosionMap;
    }
    
    public float GetErosion(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0.5f;
        return ErosionMap[x, y];
    }
}

/// <summary>
/// Simulates hydraulic and thermal erosion.
/// Water flows downhill, carrying sediment and eroding terrain.
/// </summary>
public sealed class ErosionGenerator : IGenerationModule
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _erosionIterations;
    
    public string OutputLayerId => "erosion";
    public IReadOnlyList<string> RequiredInputLayers => new[] { "height", "moisture" };
    
    public ErosionGenerator(int width, int height, int erosionIterations = 3)
    {
        _width = width;
        _height = height;
        _erosionIterations = erosionIterations;
    }
    
    public IGenerationLayer Generate(ulong worldSeed, IReadOnlyDictionary<string, IGenerationLayer> inputLayers)
    {
        var heightLayer = (HeightLayer)inputLayers["height"];
        var moistureLayer = (MoistureLayer)inputLayers["moisture"];
        var erosionMap = new float[_width, _height];
        
        // Initialize erosion based on moisture
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                float moisture = moistureLayer.GetMoisture(x, y);
                erosionMap[x, y] = moisture * 0.3f; // Base erosion from rainfall
            }
        }
        
        // Simulate water flow and erosion
        var rng = new DeterministicRng(worldSeed + 5000);
        
        for (int iteration = 0; iteration < _erosionIterations; iteration++)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    float height = heightLayer.GetNormalizedHeight(x, y);
                    if (height < 0.3f) continue; // Skip lowlands
                    
                    float moisture = moistureLayer.GetMoisture(x, y);
                    if (moisture < 0.2f) continue; // Skip dry areas
                    
                    // Find lowest neighbor
                    float lowestHeight = height;
                    int lowestX = x, lowestY = y;
                    
                    foreach (var neighbor in new GridCoord(x, y).GetNeighborsIncludingDiagonals())
                    {
                        if (neighbor.X >= 0 && neighbor.X < _width && 
                            neighbor.Y >= 0 && neighbor.Y < _height)
                        {
                            float neighborHeight = heightLayer.GetNormalizedHeight(neighbor.X, neighbor.Y);
                            if (neighborHeight < lowestHeight)
                            {
                                lowestHeight = neighborHeight;
                                lowestX = neighbor.X;
                                lowestY = neighbor.Y;
                            }
                        }
                    }
                    
                    // If there's a lower neighbor, erode this cell and deposit downstream
                    if (lowestX != x || lowestY != y)
                    {
                        float erosionAmount = moisture * 0.02f * (height - lowestHeight);
                        erosionMap[x, y] += erosionAmount;
                        erosionMap[lowestX, lowestY] -= erosionAmount * 0.3f; // Some sediment deposits
                    }
                }
            }
        }
        
        // Normalize erosion map
        float minErosion = float.MaxValue;
        float maxErosion = float.MinValue;
        
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (erosionMap[x, y] < minErosion) minErosion = erosionMap[x, y];
                if (erosionMap[x, y] > maxErosion) maxErosion = erosionMap[x, y];
            }
        }
        
        // Clamp and normalize
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (maxErosion != minErosion)
                    erosionMap[x, y] = (erosionMap[x, y] - minErosion) / (maxErosion - minErosion);
                else
                    erosionMap[x, y] = 0.5f;
                
                erosionMap[x, y] = MathF.Max(0f, MathF.Min(1f, erosionMap[x, y]));
            }
        }
        
        return new ErosionLayer(_width, _height, erosionMap);
    }
}
