namespace LivingWorld.Core;

/// <summary>
/// Represents a 2D coordinate in the world grid.
/// </summary>
public readonly record struct GridCoord(int X, int Y)
{
    public static GridCoord Zero => new(0, 0);
    
    public GridCoord North => new(X, Y - 1);
    public GridCoord South => new(X, Y + 1);
    public GridCoord East => new(X + 1, Y);
    public GridCoord West => new(X - 1, Y);
    
    public IEnumerable<GridCoord> GetNeighbors()
    {
        yield return North;
        yield return South;
        yield return East;
        yield return West;
    }
    
    public IEnumerable<GridCoord> GetNeighborsIncludingDiagonals()
    {
        yield return North;
        yield return South;
        yield return East;
        yield return West;
        yield return new(X - 1, Y - 1);
        yield return new(X + 1, Y - 1);
        yield return new(X - 1, Y + 1);
        yield return new(X + 1, Y + 1);
    }
    
    public double DistanceTo(GridCoord other)
    {
        int dx = other.X - X;
        int dy = other.Y - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
    
    public int ManhattanDistanceTo(GridCoord other)
    {
        return Math.Abs(other.X - X) + Math.Abs(other.Y - Y);
    }
}
