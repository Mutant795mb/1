namespace LivingWorld.Rendering;

using LivingWorld.Simulation;
using LivingWorld.Simulation.Settlements;
using System.Collections.Generic;

using LivingWorld.World;
using LivingWorld.Generation;
using Raylib_cs;

/// <summary>
/// Enhanced interactive world renderer with smooth zoom, pan, and detailed GUI.
/// </summary>
public sealed class UnifiedWorldRenderer
{
    private readonly WorldData _world;
    private readonly SimulationManager? _simulation;
    private readonly int _baseCellSize;
    private readonly float _fontSize;
    
    private readonly Dictionary<BiomeType, Color> _biomeColors = new()
    {
        { BiomeType.Ocean, new Color(0, 100, 200, 255) },
        { BiomeType.Beach, new Color(238, 203, 173, 255) },
        { BiomeType.Grassland, new Color(124, 252, 0, 255) },
        { BiomeType.Forest, new Color(34, 139, 34, 255) },
        { BiomeType.SeasonalForest, new Color(50, 205, 50, 255) },
        { BiomeType.Rainforest, new Color(0, 100, 0, 255) },
        { BiomeType.Savanna, new Color(245, 222, 179, 255) },
        { BiomeType.Tundra, new Color(192, 192, 192, 255) },
        { BiomeType.Mountain, new Color(128, 128, 128, 255) },
        { BiomeType.SnowMountain, new Color(255, 255, 255, 255) },
        { BiomeType.Taiga, new Color(47, 79, 79, 255) },
        { BiomeType.Desert, new Color(245, 222, 113, 255) },
        { BiomeType.Swamp, new Color(85, 107, 47, 255) },
        { BiomeType.Lake, new Color(70, 130, 180, 255) }
    };
    
    // Camera for zoom/pan
    private Vector2 _cameraOffset = Vector2.Zero;
    private float _zoom = 1f;
    private float _targetZoom = 1f; // For smooth zoom
    private bool _isDragging = false;
    private bool _isLeftClickDrag = false;
    private Vector2 _lastMousePos = Vector2.Zero;
    private Vector2 _dragStartPos = Vector2.Zero;
    
    // GUI state
    private bool _showGui = false;
    private GridCoord? _selectedCell = null;
    private Rectangle _guiRect;
    private bool _showSettings = true;
    
    // Settings
    private int _settingsWidth;
    private int _settingsHeight;
    private ulong _settingsSeed;
    private float _settingsFontSize;
    
    // Layers to display
    private DisplayLayer _currentDisplayLayer = DisplayLayer.Biome;
    
    public enum DisplayLayer
    {
        Biome,
        Height,
        Temperature,
        Moisture,
        Fertility,
        Erosion,
        Resources
    }
    
    public UnifiedWorldRenderer(WorldData world, SimulationManager? simulation = null, int baseCellSize = 8, float fontSize = 16f)
    {
        _world = world;
        _baseCellSize = baseCellSize;
        _fontSize = fontSize;
        
        _simulation = simulation;
        // Initialize GUI rect based on font size
        float guiScale = fontSize / 16f;
        _guiRect = new Rectangle(10, 10, 400 * guiScale, 500 * guiScale);
        
        // Default settings
        _settingsWidth = world.Width;
        _settingsHeight = world.Height;
        _settingsSeed = world.Seed;
        _settingsFontSize = fontSize;
    }
    
    /// <summary>
    /// Initialize the rendering window.
    /// </summary>
    public void InitWindow()
    {
        int screenWidth = Math.Max(1024, _world.Width * _baseCellSize);
        int screenHeight = Math.Max(768, _world.Height * _baseCellSize);
        Raylib.InitWindow(screenWidth, screenHeight, "Living World Simulation - Interactive");
        Raylib.SetTargetFPS(60);
        
        // Center camera
        _cameraOffset = new Vector2(
            (screenWidth - _world.Width * _baseCellSize) / 2f,
            (screenHeight - _world.Height * _baseCellSize) / 2f
        );
        _targetZoom = _zoom;
    }
    
    /// <summary>
    /// Render the world with interaction.
    /// </summary>
    public void Render()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(new Color(20, 20, 20, 255));
        
        // Handle input
        HandleInput();
        
        // Draw world
        DrawWorld();
        
        // Draw UI overlay
        DrawUiOverlay();
        
        // Draw GUI panel if selected
        if (_showGui && _selectedCell.HasValue)
        {
            DrawGuiPanel(_selectedCell.Value);
        }
        
        Raylib.EndDrawing();
    }
    
    private void HandleInput()
    {
        // Smooth zoom with mouse wheel
        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            float zoomFactor = wheel > 0 ? 1.15f : 0.87f;
            _targetZoom = MathF.Max(0.3f, MathF.Min(20f, _targetZoom * zoomFactor));
        }
        
        // Smooth zoom interpolation
        _zoom += (_targetZoom - _zoom) * 0.15f;
        
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
        
        // Pan with left mouse button drag (when not clicking on GUI)
        if (Raylib.IsMouseButtonPressed(MouseButton.MouseButtonLeft))
        {
            Vector2 mousePos = Raylib.GetMousePosition();
            if (!IsPointInRect(mousePos, _guiRect))
            {
                _isLeftClickDrag = true;
                _dragStartPos = mousePos;
            }
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
                // Was a click, select cell
                if (!IsPointInRect(mousePos, _guiRect))
                {
                    var cell = ScreenToWorldCoords(mousePos);
                    if (cell.X >= 0 && cell.X < _world.Width && 
                        cell.Y >= 0 && cell.Y < _world.Height)
                    {
                        _selectedCell = cell;
                        _showGui = true;
                    }
                }
            }
        }
        
        // Handle dragging
        if (_isDragging)
        {
            Vector2 currentMousePos = Raylib.GetMousePosition();
            _cameraOffset.X += currentMousePos.X - _lastMousePos.X;
            _cameraOffset.Y += currentMousePos.Y - _lastMousePos.Y;
            _lastMousePos = currentMousePos;
        }
        else if (_isLeftClickDrag)
        {
            Vector2 currentMousePos = Raylib.GetMousePosition();
            _cameraOffset.X += currentMousePos.X - _dragStartPos.X;
            _cameraOffset.Y += currentMousePos.Y - _dragStartPos.Y;
            _dragStartPos = currentMousePos;
        }
        
        // Right click to close GUI
        if (Raylib.IsMouseButtonPressed(MouseButton.MouseButtonRight))
        {
            _showGui = false;
            _selectedCell = null;
        }
        
        // ESC to exit
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            Raylib.CloseWindow();
        }
        
        // Layer switching with number keys
        if (Raylib.IsKeyPressed(KeyboardKey.KeyOne)) _currentDisplayLayer = DisplayLayer.Biome;
        if (Raylib.IsKeyPressed(KeyboardKey.KeyTwo)) _currentDisplayLayer = DisplayLayer.Height;
        if (Raylib.IsKeyPressed(KeyboardKey.KeyThree)) _currentDisplayLayer = DisplayLayer.Temperature;
        if (Raylib.IsKeyPressed(KeyboardKey.KeyFour)) _currentDisplayLayer = DisplayLayer.Moisture;
        if (Raylib.IsKeyPressed(KeyboardKey.KeyFive)) _currentDisplayLayer = DisplayLayer.Fertility;
        if (Raylib.IsKeyPressed(KeyboardKey.KeySix)) _currentDisplayLayer = DisplayLayer.Erosion;
        if (Raylib.IsKeyPressed(KeyboardKey.KeySeven)) _currentDisplayLayer = DisplayLayer.Resources;
        
        // Toggle settings panel with 'S' key
        if (Raylib.IsKeyPressed(KeyboardKey.KeyS))
        {
            _showSettings = !_showSettings;
        }
        
        // Adjust font size with +/- keys
        if (Raylib.IsKeyPressed(KeyboardKey.KeyEqual) || Raylib.IsKeyPressed(KeyboardKey.KeyKpAdd))
        {
            _fontSize = Math.Min(32f, _fontSize + 2f);
        }
        if (Raylib.IsKeyPressed(KeyboardKey.KeyMinus) || Raylib.IsKeyPressed(KeyboardKey.KeyKpSubtract))
        {
            _fontSize = Math.Max(10f, _fontSize - 2f);
        }
    }
    
    private void DrawWorld()
    {
        int cellRenderSize = (int)(_baseCellSize * _zoom);
        
        switch (_currentDisplayLayer)
        {
            case DisplayLayer.Biome:
                DrawBiomeLayer(cellRenderSize);
                break;
            case DisplayLayer.Height:
                DrawHeightLayer(cellRenderSize);
                break;
            case DisplayLayer.Temperature:
                DrawTemperatureLayer(cellRenderSize);
                break;
            case DisplayLayer.Moisture:
                DrawMoistureLayer(cellRenderSize);
                break;
            case DisplayLayer.Fertility:
                DrawFertilityLayer(cellRenderSize);
                break;
            case DisplayLayer.Erosion:
                DrawErosionLayer(cellRenderSize);
                break;
            case DisplayLayer.Resources:
                DrawResourceLayer(cellRenderSize);
                break;
        }
        
        // Draw selection highlight
        if (_selectedCell.HasValue)
        {
            var cell = _selectedCell.Value;
            int x = (int)(_cameraOffset.X + cell.X * cellRenderSize);
            int y = (int)(_cameraOffset.Y + cell.Y * cellRenderSize);
            
            Raylib.DrawRectangleLines(x, y, cellRenderSize + 1, cellRenderSize + 1, new Color(255, 255, 0, 255));
            Raylib.DrawRectangleLines(x - 1, y - 1, cellRenderSize + 3, cellRenderSize + 3, new Color(255, 255, 0, 255));
        }
    }
    
    private void DrawBiomeLayer(int cellSize)
    {
        if (!_world.TryGetLayer<BiomeLayer>("biome", out var biomeLayer)) return;
        
        // Calculate visible area with padding
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        int startX = Math.Max(0, (int)((- _cameraOffset.X) / cellSize) - 2);
        int startY = Math.Max(0, (int)((- _cameraOffset.Y) / cellSize) - 2);
        int endX = Math.Min(_world.Width, (int)((screenWidth - _cameraOffset.X) / cellSize) + 3);
        int endY = Math.Min(_world.Height, (int)((screenHeight - _cameraOffset.Y) / cellSize) + 3);
        
        // LOD: Skip pixels when zoomed out
        int step = CalculateLodStep(cellSize);
        
        for (int y = startY; y < endY; y += step)
        {
            for (int x = startX; x < endX; x += step)
            {
                var biome = biomeLayer.GetBiome(x, y);
                var color = _biomeColors.GetValueOrDefault(biome, new Color(128, 0, 128, 255));
                
                int screenX = (int)(_cameraOffset.X + x * cellSize);
                int screenY = (int)(_cameraOffset.Y + y * cellSize);
                int renderSize = cellSize * step;
                
                Raylib.DrawRectangle(screenX, screenY, renderSize, renderSize, color);
            }
        }
    }
    
    private void DrawHeightLayer(int cellSize)
    {
        if (!_world.TryGetLayer<HeightLayer>("height", out var heightLayer)) return;
        
        // Calculate visible area with padding
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        int startX = Math.Max(0, (int)((- _cameraOffset.X) / cellSize) - 2);
        int startY = Math.Max(0, (int)((- _cameraOffset.Y) / cellSize) - 2);
        int endX = Math.Min(_world.Width, (int)((screenWidth - _cameraOffset.X) / cellSize) + 3);
        int endY = Math.Min(_world.Height, (int)((screenHeight - _cameraOffset.Y) / cellSize) + 3);
        
        // LOD: Skip pixels when zoomed out
        int step = CalculateLodStep(cellSize);
        
        for (int y = startY; y < endY; y += step)
        {
            for (int x = startX; x < endX; x += step)
            {
                float h = heightLayer.GetNormalizedHeight(x, y);
                Color color = InterpolateColor(h, 
                    new Color(0, 0, 128, 255),    // Deep blue (low)
                    new Color(0, 100, 200, 255),  // Ocean
                    new Color(238, 203, 173, 255), // Beach
                    new Color(34, 139, 34, 255),   // Green (mid)
                    new Color(128, 128, 128, 255), // Mountain
                    new Color(255, 255, 255, 255)  // Snow (high)
                );
                
                int screenX = (int)(_cameraOffset.X + x * cellSize);
                int screenY = (int)(_cameraOffset.Y + y * cellSize);
                int renderSize = cellSize * step;
                
                Raylib.DrawRectangle(screenX, screenY, renderSize, renderSize, color);
            }
        }
    }
    
    private void DrawTemperatureLayer(int cellSize)
    {
        if (!_world.TryGetLayer<TemperatureLayer>("temperature", out var tempLayer)) return;
        
        // Calculate visible area with padding
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        int startX = Math.Max(0, (int)((- _cameraOffset.X) / cellSize) - 2);
        int startY = Math.Max(0, (int)((- _cameraOffset.Y) / cellSize) - 2);
        int endX = Math.Min(_world.Width, (int)((screenWidth - _cameraOffset.X) / cellSize) + 3);
        int endY = Math.Min(_world.Height, (int)((screenHeight - _cameraOffset.Y) / cellSize) + 3);
        
        // LOD: Skip pixels when zoomed out
        int step = CalculateLodStep(cellSize);
        
        for (int y = startY; y < endY; y += step)
        {
            for (int x = startX; x < endX; x += step)
            {
                float t = tempLayer.GetTemperature(x, y);
                Color color = InterpolateColor(t,
                    new Color(0, 0, 255, 255),    // Cold (blue)
                    new Color(0, 255, 255, 255),  // Cool (cyan)
                    new Color(0, 255, 0, 255),    // Mild (green)
                    new Color(255, 255, 0, 255),  // Warm (yellow)
                    new Color(255, 0, 0, 255)     // Hot (red)
                );
                
                int screenX = (int)(_cameraOffset.X + x * cellSize);
                int screenY = (int)(_cameraOffset.Y + y * cellSize);
                int renderSize = cellSize * step;
                
                Raylib.DrawRectangle(screenX, screenY, renderSize, renderSize, color);
            }
        }
    }
    
    private void DrawMoistureLayer(int cellSize)
    {
        if (!_world.TryGetLayer<MoistureLayer>("moisture", out var moistureLayer)) return;
        
        // Calculate visible area with padding
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        int startX = Math.Max(0, (int)((- _cameraOffset.X) / cellSize) - 2);
        int startY = Math.Max(0, (int)((- _cameraOffset.Y) / cellSize) - 2);
        int endX = Math.Min(_world.Width, (int)((screenWidth - _cameraOffset.X) / cellSize) + 3);
        int endY = Math.Min(_world.Height, (int)((screenHeight - _cameraOffset.Y) / cellSize) + 3);
        
        // LOD: Skip pixels when zoomed out
        int step = CalculateLodStep(cellSize);
        
        for (int y = startY; y < endY; y += step)
        {
            for (int x = startX; x < endX; x += step)
            {
                float m = moistureLayer.GetMoisture(x, y);
                Color color = InterpolateColor(m,
                    new Color(255, 200, 0, 255),   // Dry (orange)
                    new Color(255, 255, 100, 255), // Semi-dry (light yellow)
                    new Color(100, 200, 100, 255), // Moderate (light green)
                    new Color(0, 150, 200, 255),   // Wet (blue-green)
                    new Color(0, 50, 150, 255)     // Very wet (dark blue)
                );
                
                int screenX = (int)(_cameraOffset.X + x * cellSize);
                int screenY = (int)(_cameraOffset.Y + y * cellSize);
                int renderSize = cellSize * step;
                
                Raylib.DrawRectangle(screenX, screenY, renderSize, renderSize, color);
            }
        }
    }
    
    private void DrawFertilityLayer(int cellSize)
    {
        if (!_world.TryGetLayer<FertilityLayer>("fertility", out var fertilityLayer)) return;
        
        // Calculate visible area with padding
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        int startX = Math.Max(0, (int)((- _cameraOffset.X) / cellSize) - 2);
        int startY = Math.Max(0, (int)((- _cameraOffset.Y) / cellSize) - 2);
        int endX = Math.Min(_world.Width, (int)((screenWidth - _cameraOffset.X) / cellSize) + 3);
        int endY = Math.Min(_world.Height, (int)((screenHeight - _cameraOffset.Y) / cellSize) + 3);
        
        // LOD: Skip pixels when zoomed out
        int step = CalculateLodStep(cellSize);
        
        for (int y = startY; y < endY; y += step)
        {
            for (int x = startX; x < endX; x += step)
            {
                float f = fertilityLayer.GetFertility(x, y);
                Color color = InterpolateColor(f,
                    new Color(139, 69, 19, 255),   // Barren (brown)
                    new Color(210, 180, 140, 255), // Poor (tan)
                    new Color(154, 205, 50, 255),  // Moderate (yellow-green)
                    new Color(34, 139, 34, 255),   // Good (green)
                    new Color(0, 100, 0, 255)      // Excellent (dark green)
                );
                
                int screenX = (int)(_cameraOffset.X + x * cellSize);
                int screenY = (int)(_cameraOffset.Y + y * cellSize);
                int renderSize = cellSize * step;
                
                Raylib.DrawRectangle(screenX, screenY, renderSize, renderSize, color);
            }
        }
    }
    
    private void DrawErosionLayer(int cellSize)
    {
        if (!_world.TryGetLayer<ErosionLayer>("erosion", out var erosionLayer)) return;
        
        // Calculate visible area with padding
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        int startX = Math.Max(0, (int)((- _cameraOffset.X) / cellSize) - 2);
        int startY = Math.Max(0, (int)((- _cameraOffset.Y) / cellSize) - 2);
        int endX = Math.Min(_world.Width, (int)((screenWidth - _cameraOffset.X) / cellSize) + 3);
        int endY = Math.Min(_world.Height, (int)((screenHeight - _cameraOffset.Y) / cellSize) + 3);
        
        // LOD: Skip pixels when zoomed out
        int step = CalculateLodStep(cellSize);
        
        for (int y = startY; y < endY; y += step)
        {
            for (int x = startX; x < endX; x += step)
            {
                float e = erosionLayer.GetErosion(x, y);
                Color color = InterpolateColor(e,
                    new Color(139, 90, 43, 255),   // Low erosion (brown)
                    new Color(205, 133, 63, 255),  // Moderate (peru)
                    new Color(244, 164, 96, 255),  // High (sandy brown)
                    new Color(255, 228, 181, 255), // Very high (moccasin)
                    new Color(255, 255, 255, 255)  // Extreme (white)
                );
                
                int screenX = (int)(_cameraOffset.X + x * cellSize);
                int screenY = (int)(_cameraOffset.Y + y * cellSize);
                int renderSize = cellSize * step;
                
                Raylib.DrawRectangle(screenX, screenY, renderSize, renderSize, color);
            }
        }
    }
    
    private void DrawResourceLayer(int cellSize)
    {
        if (!_world.TryGetLayer<ResourceLayer>("resources", out var resourceLayer)) return;
        
        // Calculate visible area with padding
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        int startX = Math.Max(0, (int)((- _cameraOffset.X) / cellSize) - 2);
        int startY = Math.Max(0, (int)((- _cameraOffset.Y) / cellSize) - 2);
        int endX = Math.Min(_world.Width, (int)((screenWidth - _cameraOffset.X) / cellSize) + 3);
        int endY = Math.Min(_world.Height, (int)((screenHeight - _cameraOffset.Y) / cellSize) + 3);
        
        // LOD: Skip pixels when zoomed out
        int step = CalculateLodStep(cellSize);
        
        for (int y = startY; y < endY; y += step)
        {
            for (int x = startX; x < endX; x += step)
            {
                var resources = resourceLayer.GetResources(x, y);
                Color color = GetResourceColor(resources);
                
                int screenX = (int)(_cameraOffset.X + x * cellSize);
                int screenY = (int)(_cameraOffset.Y + y * cellSize);
                int renderSize = cellSize * step;
                
                Raylib.DrawRectangle(screenX, screenY, renderSize, renderSize, color);
            }
        }
    }
    
    private Color GetResourceColor(CellResources resources)
    {
        if (resources.Types == ResourceType.None)
            return new Color(80, 80, 80, 255); // Gray for no resources
        
        // Show dominant resource type
        if (resources.HasResource(ResourceType.Wood) && resources.WoodAmount > 30)
            return new Color(34, 139, 34, 255); // Forest green
        if (resources.HasResource(ResourceType.Fish) && resources.FoodAmount > 20)
            return new Color(70, 130, 180, 255); // Steel blue
        if (resources.HasResource(ResourceType.Gold) || resources.HasResource(ResourceType.Gems))
            return new Color(255, 215, 0, 255); // Gold
        if (resources.HasResource(ResourceType.Iron) || resources.HasResource(ResourceType.Coal))
            return new Color(128, 128, 128, 255); // Iron gray
        if (resources.HasResource(ResourceType.Copper))
            return new Color(184, 115, 51, 255); // Copper
        if (resources.HasResource(ResourceType.Oil))
            return new Color(30, 30, 30, 255); // Black
        if (resources.HasResource(ResourceType.Stone) || resources.HasResource(ResourceType.Clay) || resources.HasResource(ResourceType.Salt))
            return new Color(169, 169, 169, 255); // Dark gray
        
        return new Color(100, 100, 100, 255);
    }
    
    private static Color InterpolateColor(float t, params Color[] colors)
    {
        if (colors.Length == 0) return Color.White;
        if (colors.Length == 1) return colors[0];
        
        t = MathF.Max(0f, MathF.Min(1f, t));
        float segment = 1f / (colors.Length - 1);
        int index = (int)(t / segment);
        index = Math.Min(index, colors.Length - 2);
        
        float localT = (t - index * segment) / segment;
        
        Color c1 = colors[index];
        Color c2 = colors[index + 1];
        
        return new Color(
            (int)(c1.R + (c2.R - c1.R) * localT),
            (int)(c1.G + (c2.G - c1.G) * localT),
            (int)(c1.B + (c2.B - c1.B) * localT),
            255
        );
    }
    
    private void DrawUiOverlay()
    {
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        // Top bar with info - larger and more readable
        int topBarHeight = (int)(35 * (_fontSize / 16f));
        Raylib.DrawRectangle(0, 0, screenWidth, topBarHeight, new Color(0, 0, 0, 200));
        Raylib.DrawText($"World: {_world.Width}x{_world.Height} | Seed: {_world.Seed}", 15, 10, (int)_fontSize, Color.White);
        Raylib.DrawText($"Layer: {_currentDisplayLayer} | Zoom: {_zoom:F2}x", 300, 10, (int)_fontSize, Color.White);
        Raylib.DrawText("LMB: Select/Move | RMB: Close | MMB: Pan | Wheel: Zoom | 1-7: Layers | S: Settings | ESC: Exit", screenWidth - 650, 10, (int)(_fontSize * 0.9f), Color.Yellow);
        
        // Layer legend - larger and more visible
        DrawLayerLegend();
        
        // Settings panel
        if (_showSettings)
        {
            DrawSettingsPanel();
        }
    }
    
    private void DrawSettingsPanel()
    {
        float scale = _fontSize / 16f;
        int panelWidth = 280;
        int panelHeight = 180;
        int panelX = Raylib.GetScreenWidth() - panelWidth - 10;
        int panelY = 40;
        
        Raylib.DrawRectangle(panelX, panelY, panelWidth, panelHeight, new Color(30, 30, 30, 220));
        Raylib.DrawRectangleLines(panelX, panelY, panelWidth, panelHeight, Color.Gray);
        
        Raylib.DrawText("Settings (S to toggle)", panelX + 10, panelY + 8, (int)(14 * scale), Color.Yellow);
        
        Raylib.DrawText($"Font Size: {_fontSize:F0}", panelX + 10, panelY + 35, (int)(12 * scale), Color.LightGray);
        Raylib.DrawText($"Base Cell: {_baseCellSize}px", panelX + 10, panelY + 55, (int)(12 * scale), Color.LightGray);
        Raylib.DrawText($"Zoom: {_zoom:F2}x", panelX + 10, panelY + 75, (int)(12 * scale), Color.LightGray);
        Raylib.DrawText($"Camera: ({_cameraOffset.X:F0}, {_cameraOffset.Y:F0})", panelX + 10, panelY + 95, (int)(12 * scale), Color.LightGray);
        
        Raylib.DrawText($"Selected: {(_selectedCell?.ToString() ?? "None")}", panelX + 10, panelY + 120, (int)(12 * scale), Color.White);
        
        Raylib.DrawText("+/-: Font Size", panelX + 10, panelY + 145, (int)(10 * scale), Color.Gray);
    }
    
    private void DrawLayerLegend()
    {
        float scale = _fontSize / 16f;
        int legendX = 10;
        int legendY = Raylib.GetScreenHeight() - (int)(140 * scale);
        
        Raylib.DrawRectangle(legendX, legendY, (int)(220 * scale), (int)(130 * scale), new Color(0, 0, 0, 200));
        Raylib.DrawRectangleLines(legendX, legendY, (int)(220 * scale), (int)(130 * scale), Color.Gray);
        
        string title = $"Legend: {_currentDisplayLayer}";
        Raylib.DrawText(title, legendX + (int)(10 * scale), legendY + (int)(10 * scale), (int)(14 * scale), Color.Yellow);
        
        string[] legendText = _currentDisplayLayer switch
        {
            DisplayLayer.Biome => new[] { "Ocean: Blue", "Beach: Tan", "Forest: Green", "Desert: Yellow", "Mountain: Gray", "Snow: White" },
            DisplayLayer.Height => new[] { "Low: Dark Blue", "Mid: Green", "High: Gray", "Peak: White" },
            DisplayLayer.Temperature => new[] { "Cold: Blue", "Cool: Cyan", "Mild: Green", "Warm: Yellow", "Hot: Red" },
            DisplayLayer.Moisture => new[] { "Dry: Orange", "Moderate: Light Green", "Wet: Blue-Green", "Very Wet: Dark Blue" },
            DisplayLayer.Fertility => new[] { "Barren: Brown", "Poor: Tan", "Moderate: Yellow-Green", "Good: Green", "Excellent: Dark Green" },
            DisplayLayer.Erosion => new[] { "Low: Brown", "Moderate: Peru", "High: Sandy", "Very High: Light", "Extreme: White" },
            DisplayLayer.Resources => new[] { "None: Gray", "Wood: Green", "Fish: Blue", "Gold/Gems: Gold", "Iron/Coal: Gray", "Copper: Brown", "Oil: Black" },
            _ => Array.Empty<string>()
        };
        
        for (int i = 0; i < legendText.Length; i++)
        {
            Raylib.DrawText(legendText[i], legendX + (int)(10 * scale), legendY + (int)(35 * scale) + (int)(i * 18 * scale), (int)(12 * scale), Color.White);
        }
    }
    
    private void DrawGuiPanel(GridCoord cell)
    {
        float scale = _fontSize / 16f;
        
        // Ensure GUI is within screen bounds
        int screenWidth = Raylib.GetScreenWidth();
        int screenHeight = Raylib.GetScreenHeight();
        
        int guiX = (int)Math.Min(_guiRect.X, screenWidth - _guiRect.Width - 10);
        int guiY = (int)Math.Min(_guiRect.Y, screenHeight - _guiRect.Height - 10);
        
        var guiRect = new Rectangle(guiX, guiY, _guiRect.Width, _guiRect.Height);
        
        // Background
        Raylib.DrawRectangleRec(guiRect, new Color(30, 30, 30, 240));
        Raylib.DrawRectangleLinesEx(guiRect, 2, Color.Gray);
        
        // Title - larger and more readable
        string title = $"Cell [{cell.X}, {cell.Y}]";
        Raylib.DrawText(title, (int)guiRect.X + (int)(12 * scale), (int)guiRect.Y + (int)(12 * scale), (int)(18 * scale), Color.Yellow);
        
        int yPos = (int)guiRect.Y + (int)(45 * scale);
        int lineHeight = (int)(20 * scale);
        
        // Get all layer data
        var biome = _world.GetBiome(cell.X, cell.Y);
        float height = _world.GetHeight(cell.X, cell.Y);
        
        // Biome info
        Raylib.DrawText($"Biome:", (int)guiRect.X + (int)(12 * scale), yPos, (int)(14 * scale), Color.LightGray);
        yPos += lineHeight;
        Raylib.DrawText($"  {biome}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.White);
        yPos += (int)(lineHeight * 1.3f);
        
        // Height info
        Raylib.DrawText($"Terrain:", (int)guiRect.X + (int)(12 * scale), yPos, (int)(14 * scale), Color.LightGray);
        yPos += lineHeight;
        Raylib.DrawText($"  Height: {height:F3}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.White);
        yPos += lineHeight;
        Raylib.DrawText($"  Normalized: {_world.TryGetLayer<HeightLayer>("height", out var hl) ? hl.GetNormalizedHeight(cell.X, cell.Y):0:F2}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.White);
        yPos += (int)(lineHeight * 1.3f);
        
        // Climate info
        Raylib.DrawText($"Climate:", (int)guiRect.X + (int)(12 * scale), yPos, (int)(14 * scale), Color.LightGray);
        yPos += lineHeight;
        if (_world.TryGetLayer<TemperatureLayer>("temperature", out var tempLayer))
        {
            float temp = tempLayer.GetTemperature(cell.X, cell.Y);
            string tempDesc = temp < 0.3f ? "Cold" : temp < 0.5f ? "Cool" : temp < 0.7f ? "Warm" : "Hot";
            Raylib.DrawText($"  Temperature: {temp:F2} ({tempDesc})", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.White);
        }
        yPos += lineHeight;
        if (_world.TryGetLayer<MoistureLayer>("moisture", out var moistLayer))
        {
            float moist = moistLayer.GetMoisture(cell.X, cell.Y);
            string moistDesc = moist < 0.25f ? "Arid" : moist < 0.5f ? "Moderate" : moist < 0.75f ? "Humid" : "Very Humid";
            Raylib.DrawText($"  Moisture: {moist:F2} ({moistDesc})", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.White);
        }
        yPos += (int)(lineHeight * 1.3f);
        
        // Soil info
        Raylib.DrawText($"Soil & Agriculture:", (int)guiRect.X + (int)(12 * scale), yPos, (int)(14 * scale), Color.LightGray);
        yPos += lineHeight;
        if (_world.TryGetLayer<FertilityLayer>("fertility", out var fertLayer))
        {
            float fert = fertLayer.GetFertility(cell.X, cell.Y);
            string fertDesc = fert < 0.2f ? "Barren" : fert < 0.4f ? "Poor" : fert < 0.6f ? "Moderate" : fert < 0.8f ? "Good" : "Excellent";
            Raylib.DrawText($"  Fertility: {fert:F2} ({fertDesc})", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.White);
        }
        yPos += lineHeight;
        if (_world.TryGetLayer<ErosionLayer>("erosion", out var erosLayer))
        {
            float eros = erosLayer.GetErosion(cell.X, cell.Y);
            Raylib.DrawText($"  Erosion: {eros:F2}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.White);
        }
        yPos += (int)(lineHeight * 1.3f);
        
        // Resources info
        Raylib.DrawText($"Resources:", (int)guiRect.X + (int)(12 * scale), yPos, (int)(14 * scale), Color.LightGray);
        yPos += lineHeight;
        if (_world.TryGetLayer<ResourceLayer>("resources", out var resLayer))
        {
            var res = resLayer.GetResources(cell.X, cell.Y);
            if (res.Types == ResourceType.None)
            {
                Raylib.DrawText($"  No significant resources", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.Gray);
            }
            else
            {
                if (res.HasResource(ResourceType.Wood))
                    Raylib.DrawText($"  Wood: {res.WoodAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), new Color(34, 139, 34, 255));
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Stone))
                    Raylib.DrawText($"  Stone: {res.StoneAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.Gray);
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Iron))
                    Raylib.DrawText($"  Iron: {res.MetalAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), new Color(180, 180, 180, 255));
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Copper))
                    Raylib.DrawText($"  Copper: {res.MetalAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), new Color(184, 115, 51, 255));
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Coal))
                    Raylib.DrawText($"  Coal: {res.MetalAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.Black);
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Gold))
                    Raylib.DrawText($"  Gold: {res.PreciousAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), new Color(255, 215, 0, 255));
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Gems))
                    Raylib.DrawText($"  Gems: {res.PreciousAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), new Color(200, 50, 200, 255));
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Oil))
                    Raylib.DrawText($"  Oil: {res.MetalAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), new Color(30, 30, 30, 255));
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Fish))
                    Raylib.DrawText($"  Fish: {res.FoodAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), new Color(70, 130, 180, 255));
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Salt))
                    Raylib.DrawText($"  Salt: {res.StoneAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), Color.White);
                yPos += lineHeight;
                if (res.HasResource(ResourceType.Clay))
                    Raylib.DrawText($"  Clay: {res.StoneAmount}", (int)guiRect.X + (int)(22 * scale), yPos, (int)(13 * scale), new Color(169, 100, 50, 255));
            }
        }
        
        // Instructions
        yPos = (int)guiRect.Y + (int)guiRect.Height - (int)(28 * scale);
        Raylib.DrawText("RMB to close", (int)guiRect.X + (int)(12 * scale), yPos, (int)(12 * scale), Color.Gray);
    }
    
    private GridCoord ScreenToWorldCoords(Vector2 screenPos)
    {
        int cellRenderSize = (int)(_baseCellSize * _zoom);
        
        int x = (int)((screenPos.X - _cameraOffset.X) / cellRenderSize);
        int y = (int)((screenPos.Y - _cameraOffset.Y) / cellRenderSize);
        
        return new GridCoord(x, y);
    }
    
    private bool IsPointInRect(Vector2 point, Rectangle rect)
    {
        return point.X >= rect.X && point.X <= rect.X + rect.Width &&
               point.Y >= rect.Y && point.Y <= rect.Y + rect.Height;
    }
    
    /// <summary>
    /// Calculate LOD step based on cell render size.
    /// When zoomed out, skip cells to improve performance.
    /// </summary>
    private int CalculateLodStep(int cellRenderSize)
    {
        // If cells are very small on screen, skip some for performance
        if (cellRenderSize < 4) return 8;      // Very far zoom - show 1/64 cells
        if (cellRenderSize < 8) return 4;      // Far zoom - show 1/16 cells
        if (cellRenderSize < 12) return 2;     // Medium zoom - show 1/4 cells
        return 1;                               // Close zoom - show all cells
    }
    
    /// <summary>
    /// Check if window should close.
    /// </summary>
    public bool ShouldClose() => Raylib.WindowShouldClose();
    
    /// <summary>
    /// Close the window.
    /// </summary>
    public void CloseWindow() => Raylib.CloseWindow();

    /// <summary>
    /// Draw simulation entities (settlements, units) on top of the world.
    /// </summary>
    private void DrawSimulationEntities(int cellSize)
    {
        if (_simulation == null) return;
        
        var entities = _simulation.Entities;
        int step = CalculateLodStep(cellSize);
        
        foreach (var entity in entities)
        {
            if (entity.Position.X < 0 || entity.Position.X >= _world.Width ||
                entity.Position.Y < 0 || entity.Position.Y >= _world.Height)
                continue;
            
            // Skip based on LOD
            if ((entity.Position.X % step != 0) || (entity.Position.Y % step != 0))
                continue;
            
            int screenX = (int)(_cameraOffset.X + entity.Position.X * cellSize);
            int screenY = (int)(_cameraOffset.Y + entity.Position.Y * cellSize);
            int renderSize = Math.Max(4, cellSize / 2);
            
            Color entityColor = entity switch
            {
                Settlement => new Color(255, 165, 0, 255), // Orange for settlements
                _ => new Color(200, 200, 200, 255) // Gray for other entities
            };
            
            // Draw entity as circle
            Raylib.DrawCircle(screenX + renderSize/2, screenY + renderSize/2, renderSize/2, entityColor);
        }
    }
}
