namespace LivingWorld.Rendering.Chunks;

using Raylib_cs;
using LivingWorld.World;
using LivingWorld.Core;

/// <summary>
/// Represents a chunk of the world with cached rendering data.
/// Uses dirty-flag pattern to minimize re-rendering.
/// </summary>
public sealed class Chunk : IDisposable
{
    public int ChunkX { get; }
    public int ChunkY { get; }
    public int Size { get; }
    
    // Cached render texture
    private RenderTexture2D? _cachedTexture;
    private bool _isDirty = true;
    private bool _disposed;
    
    // World reference for data access
    private readonly WorldData _world;
    
    public bool IsDirty => _isDirty;
    public bool HasCache => _cachedTexture.HasValue;
    
    public Chunk(int chunkX, int chunkY, int size, WorldData world)
    {
        ChunkX = chunkX;
        ChunkY = chunkY;
        Size = size;
        _world = world;
    }
    
    /// <summary>
    /// Mark chunk as dirty (needs re-rendering).
    /// </summary>
    public void MarkDirty()
    {
        _isDirty = true;
    }
    
    /// <summary>
    /// Render chunk to cache if dirty.
    /// Returns true if cache was updated.
    /// </summary>
    public bool UpdateCache(Func<int, int, Color> getColorFunc)
    {
        if (!_isDirty) return false;
        
        // Dispose old texture if exists
        if (_cachedTexture.HasValue)
        {
            Raylib.UnloadRenderTexture(_cachedTexture.Value);
            _cachedTexture = null;
        }
        
        // Create new render texture
        _cachedTexture = Raylib.LoadRenderTexture(Size, Size);
        
        // Render to texture
        Raylib.BeginTextureMode(_cachedTexture.Value);
        Raylib.ClearBackground(Color.Transparent);
        
        int startX = ChunkX * Size;
        int startY = ChunkY * Size;
        
        for (int y = 0; y < Size && startY + y < _world.Height; y++)
        {
            for (int x = 0; x < Size && startX + x < _world.Width; x++)
            {
                int worldX = startX + x;
                int worldY = startY + y;
                
                Color color = getColorFunc(worldX, worldY);
                
                // Draw pixel at position (flip Y for texture coordinates)
                Raylib.DrawRectangle(x, Size - 1 - y, 1, 1, color);
            }
        }
        
        Raylib.EndTextureMode();
        _isDirty = false;
        
        return true;
    }
    
    /// <summary>
    /// Draw the cached texture at screen position.
    /// </summary>
    public void Draw(int screenX, int screenY, int renderSize, Color tint)
    {
        if (!_cachedTexture.HasValue) return;
        
        var texture = _cachedTexture.Value.texture;
        
        // Draw with flipped Y to match world coordinates
        Raylib.DrawTexturePro(
            texture,
            new Rectangle(0, 0, texture.width, -texture.height), // Flip Y
            new Rectangle(screenX, screenY, renderSize, renderSize),
            Vector2.Zero,
            0f,
            tint
        );
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        if (_cachedTexture.HasValue)
        {
            Raylib.UnloadRenderTexture(_cachedTexture.Value);
            _cachedTexture = null;
        }
        
        _disposed = true;
    }
}

/// <summary>
/// Manages a grid of chunks for efficient world rendering.
/// </summary>
public sealed class ChunkManager : IDisposable
{
    private readonly int _chunkSize;
    private readonly WorldData _world;
    private readonly Dictionary<(int, int), Chunk> _chunks = new();
    private bool _disposed;
    
    public int ChunkSize => _chunkSize;
    
    public ChunkManager(WorldData world, int chunkSize = 64)
    {
        _world = world;
        _chunkSize = chunkSize;
        
        // Pre-create all chunks for the world
        int chunksX = (world.Width + chunkSize - 1) / chunkSize;
        int chunksY = (world.Height + chunkSize - 1) / chunkSize;
        
        for (int cy = 0; cy < chunksY; cy++)
        {
            for (int cx = 0; cx < chunksX; cx++)
            {
                _chunks[(cx, cy)] = new Chunk(cx, cy, chunkSize, world);
            }
        }
    }
    
    /// <summary>
    /// Mark a specific cell's chunk as dirty.
    /// </summary>
    public void MarkCellDirty(int x, int y)
    {
        int chunkX = x / _chunkSize;
        int chunkY = y / _chunkSize;
        
        if (_chunks.TryGetValue((chunkX, chunkY), out var chunk))
        {
            chunk.MarkDirty();
        }
    }
    
    /// <summary>
    /// Mark all chunks as dirty (full world update).
    /// </summary>
    public void MarkAllDirty()
    {
        foreach (var chunk in _chunks.Values)
        {
            chunk.MarkDirty();
        }
    }
    
    /// <summary>
    /// Get or create chunk at coordinates.
    /// </summary>
    public Chunk? GetChunk(int chunkX, int chunkY)
    {
        _chunks.TryGetValue((chunkX, chunkY), out var chunk);
        return chunk;
    }
    
    /// <summary>
    /// Update and draw visible chunks.
    /// </summary>
    public void DrawVisibleChunks(
        int startChunkX, int endChunkX,
        int startChunkY, int endChunkY,
        Func<int, int, Color> getColorFunc,
        int baseCellSize,
        float zoom,
        Vector2 cameraOffset)
    {
        int cellRenderSize = (int)(baseCellSize * zoom);
        
        for (int cy = startChunkY; cy < endChunkY; cy++)
        {
            for (int cx = startChunkX; cx < endChunkX; cx++)
            {
                var chunk = GetChunk(cx, cy);
                if (chunk == null) continue;
                
                // Update cache if dirty
                chunk.UpdateCache(getColorFunc);
                
                // Calculate screen position
                int screenX = (int)(cameraOffset.X + cx * chunk.Size * cellRenderSize);
                int screenY = (int)(cameraOffset.Y + cy * chunk.Size * cellRenderSize);
                int renderSize = chunk.Size * cellRenderSize;
                
                // Draw cached chunk
                chunk.Draw(screenX, screenY, renderSize, Color.White);
            }
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        
        foreach (var chunk in _chunks.Values)
        {
            chunk.Dispose();
        }
        _chunks.Clear();
        
        _disposed = true;
    }
}
