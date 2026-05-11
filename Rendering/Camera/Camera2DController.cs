namespace LivingWorld.Rendering.Camera;

using Raylib_cs;
using System.Numerics;

/// <summary>
/// 2D camera controller with smooth zoom and pan.
/// </summary>
public sealed class Camera2DController
{
    private Vector2 _offset = Vector2.Zero;
    private float _zoom = 1f;
    private float _targetZoom = 1f;
    
    private bool _isDragging = false;
    private bool _isLeftClickDrag = false;
    private Vector2 _lastMousePos = Vector2.Zero;
    private Vector2 _dragStartPos = Vector2.Zero;
    
    // Configuration
    public float MinZoom { get; set; } = 0.3f;
    public float MaxZoom { get; set; } = 20f;
    public float ZoomSpeed { get; set; } = 0.15f;
    public float BaseCellSize { get; set; } = 8f;
    
    public Vector2 Offset => _offset;
    public float Zoom => _zoom;
    public int CellRenderSize => (int)(BaseCellSize * _zoom);
    
    /// <summary>
    /// Initialize camera centered on the world.
    /// </summary>
    public void CenterOnWorld(int worldWidth, int worldHeight)
    {
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        _offset = new Vector2(
            (screenWidth - worldWidth * BaseCellSize) / 2f,
            (screenHeight - worldHeight * BaseCellSize) / 2f
        );
        _targetZoom = _zoom;
    }
    
    /// <summary>
    /// Handle input for zoom and pan.
    /// Returns true if a cell was clicked (not dragged).
    /// </summary>
    public bool HandleInput(out Vector2? clickedCell)
    {
        clickedCell = null;
        
        // Smooth zoom with mouse wheel
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            float zoomFactor = wheel > 0 ? 1.15f : 0.87f;
            _targetZoom = MathF.Max(MinZoom, MathF.Min(MaxZoom, _targetZoom * zoomFactor));
        }
        
        // Smooth zoom interpolation
        _zoom += (_targetZoom - _zoom) * ZoomSpeed;
        
        // Pan with middle mouse button
        if (Raylib.IsMouseButtonPressed(MouseButton.MouseButtonMiddle))
        {
            _isDragging = true;
            _lastMousePos = Raylib.GetMousePosition();
        }
        else if (Raylib.IsMouseButtonReleased(MouseButton.MouseButtonMiddle))
        {
            _isDragging = false;
        }
        
        // Pan with left mouse button drag
        if (Raylib.IsMouseButtonPressed(MouseButton.MouseButtonLeft))
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            _isLeftClickDrag = true;
            _dragStartPos = mousePos;
        }
        else if (Raylib.IsMouseButtonReleased(MouseButton.MouseButtonLeft))
        {
            if (_isLeftClickDrag)
            {
                // Was a drag, not a click
                _isLeftClickDrag = false;
            }
            else
            {
                // Was a click - return screen position for further processing
                clickedCell = Raylib.GetMousePosition();
            }
        }
        
        // Handle dragging
        if (_isDragging)
        {
            Vector2 currentMousePos = Raylib.GetMousePosition();
            _offset.X += currentMousePos.X - _lastMousePos.X;
            _offset.Y += currentMousePos.Y - _lastMousePos.Y;
            _lastMousePos = currentMousePos;
        }
        else if (_isLeftClickDrag)
        {
            Vector2 currentMousePos = Raylib.GetMousePosition();
            _offset.X += currentMousePos.X - _dragStartPos.X;
            _offset.Y += currentMousePos.Y - _dragStartPos.Y;
            _dragStartPos = currentMousePos;
        }
        
        return _isLeftClickDrag == false && clickedCell.HasValue;
    }
    
    /// <summary>
    /// Convert screen coordinates to world cell coordinates.
    /// </summary>
    public (int X, int Y) ScreenToWorld(Vector2 screenPos, int worldWidth, int worldHeight)
    {
        int cellSize = CellRenderSize;
        int x = (int)((screenPos.X - _offset.X) / cellSize);
        int y = (int)((screenPos.Y - _offset.Y) / cellSize);
        return (x, y);
    }
    
    /// <summary>
    /// Check if point is inside a rectangle.
    /// </summary>
    public static bool IsPointInRect(Vector2 point, Rectangle rect)
    {
        return point.X >= rect.X && point.X <= rect.X + rect.Width &&
               point.Y >= rect.Y && point.Y <= rect.Y + rect.Height;
    }
    
    /// <summary>
    /// Reset camera to default state.
    /// </summary>
    public void Reset()
    {
        _offset = Vector2.Zero;
        _zoom = 1f;
        _targetZoom = 1f;
        _isDragging = false;
        _isLeftClickDrag = false;
    }
}
