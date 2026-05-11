namespace LivingWorld.Core;

/// <summary>
/// Immutable height map layer.
/// Contains elevation data for each grid cell.
/// </summary>
public sealed class HeightLayer : IGenerationLayer
{
    public string LayerId => "height";
    public IReadOnlyList<string> Dependencies => Array.Empty<string>();
    public int Width { get; }
    public int Height { get; }
    
    /// <summary>
    /// Height values normalized to [0, 1] range.
    /// </summary>
    public readonly float[,] HeightMap;
    
    /// <summary>
    /// Minimum height value in the map.
    /// </summary>
    public readonly float MinHeight;
    
    /// <summary>
    /// Maximum height value in the map.
    /// </summary>
    public readonly float MaxHeight;
    
    public HeightLayer(int width, int height, float[,] heightMap)
    {
        Width = width;
        Height = height;
        HeightMap = heightMap;
        
        // Calculate min/max
        float min = float.MaxValue;
        float max = float.MinValue;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float h = heightMap[x, y];
                if (h < min) min = h;
                if (h > max) max = h;
            }
        }
        
        MinHeight = min;
        MaxHeight = max;
    }
    
    public float GetHeight(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return 0f;
        return HeightMap[x, y];
    }
    
    public float GetNormalizedHeight(int x, int y)
    {
        if (MaxHeight == MinHeight) return 0.5f;
        float h = GetHeight(x, y);
        return (h - MinHeight) / (MaxHeight - MinHeight);
    }
}
