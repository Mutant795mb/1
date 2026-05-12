namespace LivingWorld.Rendering.Input;

using Raylib_cs;
using System.Numerics;

/// <summary>
/// Handles all input for the world renderer.
/// Separated from UnifiedWorldRenderer to reduce coupling.
/// </summary>
public sealed class WorldInputHandler
{
    private bool _isDragging = false;
    private bool _isLeftClickDrag = false;
    private Vector2 _lastMousePos = Vector2.Zero;
    private Vector2 _dragStartPos = Vector2.Zero;
    
    // Configuration
    public float ZoomSpeed { get; set; } = 0.15f;
    public float MinZoom { get; set; } = 0.3f;
    public float MaxZoom { get; set; } = 20f;
    
    // State
    public bool IsDragging => _isDragging || _isLeftClickDrag;
    public Vector2 LastMousePos => _lastMousePos;
    
    /// <summary>
    /// Process zoom input. Returns new target zoom.
    /// </summary>
    public float ProcessZoom(float currentZoom, float targetZoom)
    {
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            float zoomFactor = wheel > 0 ? 1.15f : 0.87f;
            targetZoom = MathF.Max(MinZoom, MathF.Min(MaxZoom, targetZoom * zoomFactor));
        }
        
        // Smooth zoom interpolation
        return currentZoom + (targetZoom - currentZoom) * ZoomSpeed;
    }
    
    /// <summary>
    /// Process pan input. Returns camera offset delta.
    /// </summary>
    public Vector2 ProcessPan(Vector2 currentOffset)
    {
        Vector2 delta = Vector2.Zero;
        
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
            _isLeftClickDrag = false;
        }
        
        // Handle dragging
        if (_isDragging)
        {
            Vector2 currentMousePos = Raylib.GetMousePosition();
            delta = currentMousePos - _lastMousePos;
            _lastMousePos = currentMousePos;
        }
        else if (_isLeftClickDrag)
        {
            Vector2 currentMousePos = Raylib.GetMousePosition();
            delta = currentMousePos - _dragStartPos;
            _dragStartPos = currentMousePos;
        }
        
        return currentOffset + delta;
    }
    
    /// <summary>
    /// Check if a cell click occurred (not a drag).
    /// Returns screen position if clicked, null otherwise.
    /// </summary>
    public Vector2? CheckCellClick(Rectangle guiRect)
    {
        if (Raylib.IsMouseButtonPressed(MouseButton.MouseButtonLeft))
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            
            // Don't select if clicking on GUI
            if (!IsPointInRect(mousePos, guiRect))
            {
                // Store for drag detection
                _dragStartPos = mousePos;
            }
        }
        
        if (Raylib.IsMouseButtonReleased(MouseButton.MouseButtonLeft))
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            
            // If it wasn't a drag (mouse didn't move much), it's a click
            if (Vector2.Distance(_dragStartPos, mousePos) < 5f)
            {
                if (!IsPointInRect(mousePos, guiRect))
                {
                    return mousePos;
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Check if right click was pressed (to close GUI).
    /// </summary>
    public bool IsRightClickPressed()
    {
        return Raylib.IsMouseButtonPressed(MouseButton.MouseButtonRight);
    }
    
    /// <summary>
    /// Check if ESC was pressed (to exit).
    /// </summary>
    public bool IsEscapePressed()
    {
        return Raylib.IsKeyPressed(KeyboardKey.Escape);
    }
    
    /// <summary>
    /// Get currently pressed layer key (1-7).
    /// Returns null if no layer key pressed.
    /// </summary>
    public int? GetPressedLayerKey()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.KeyOne)) return 0;
        if (Raylib.IsKeyPressed(KeyboardKey.KeyTwo)) return 1;
        if (Raylib.IsKeyPressed(KeyboardKey.KeyThree)) return 2;
        if (Raylib.IsKeyPressed(KeyboardKey.KeyFour)) return 3;
        if (Raylib.IsKeyPressed(KeyboardKey.KeyFive)) return 4;
        if (Raylib.IsKeyPressed(KeyboardKey.KeySix)) return 5;
        if (Raylib.IsKeyPressed(KeyboardKey.KeySeven)) return 6;
        return null;
    }
    
    /// <summary>
    /// Check if settings toggle key was pressed.
    /// </summary>
    public bool IsSettingsTogglePressed()
    {
        return Raylib.IsKeyPressed(KeyboardKey.KeyS);
    }
    
    /// <summary>
    /// Get font size adjustment (-1, 0, or +1).
    /// </summary>
    public int GetFontSizeAdjustment()
    {
        int adjustment = 0;
        if (Raylib.IsKeyPressed(KeyboardKey.KeyEqual) || Raylib.IsKeyPressed(KeyboardKey.KeyKpAdd))
        {
            adjustment = 1;
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KeyMinus) || Raylib.IsKeyPressed(KeyboardKey.KeyKpSubtract))
        {
            adjustment = -1;
        }
        return adjustment;
    }
    
    private static bool IsPointInRect(Vector2 point, Rectangle rect)
    {
        return point.X >= rect.X && point.X <= rect.X + rect.Width &&
               point.Y >= rect.Y && point.Y <= rect.Y + rect.Height;
    }
    
    /// <summary>
    /// Reset input state.
    /// </summary>
    public void Reset()
    {
        _isDragging = false;
        _isLeftClickDrag = false;
        _lastMousePos = Vector2.Zero;
        _dragStartPos = Vector2.Zero;
    }
}
