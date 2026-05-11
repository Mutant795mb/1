namespace LivingWorld.App;

using Raylib_cs;
using LivingWorld.Core;
using LivingWorld.World;
using LivingWorld.Generation;
using LivingWorld.Rendering;
using LivingWorld.Simulation;
using LivingWorld.Validation;
using System.Numerics;
using System.Linq;

/// <summary>
/// Graphical world simulation with interactive menu, generation settings, and detailed GUI.
/// </summary>
public sealed class GraphicalMenuApp
{
    private enum AppState { Menu, Generating, Running }
    
    private AppState _currentState = AppState.Menu;
    
    // Menu State
    private int _selectedMode = 0; // 0: Graphical, 1: Headless
    private string _seedInput = "12345";
    private bool _seedFocused = false;
    private int _mapSizeIndex = 2; // 0: 128, 1: 256, 2: 512, 3: 1024, 4: 2048
    private readonly int[] _mapSizes = { 128, 256, 512, 1024, 2048 };
    private float _uiScale = 1.0f;
    private readonly Color _bgColor = new(20, 20, 25, 255);
    private readonly Color _panelColor = new(40, 40, 50, 240);
    private readonly Color _accentColor = new(100, 200, 255, 255);
    private readonly Color _textColor = new(240, 240, 240, 255);
    
    // Generation State
    private string _generationLog = "";
    private float _generationProgress = 0.0f;
    
    // Running State
    private WorldData? _world;
    private UnifiedWorldRenderer? _renderer;
    private SimulationManager? _simulation;
    private Camera2D _camera;
    private bool _isDragging;
    private Rectangle? _selectionRect;
    private bool _showSettings;
    private int _displayLayer = 0; // 0: Biome, 1: Height, etc.
    private bool _simulationInitialized = false;
    
    // Fonts
    private Font _font;
    
    public void Run()
    {
        Raylib.InitWindow(1280, 720, "Living Procedural World Simulation");
        Raylib.SetTargetFPS(60);
        
        _font = Raylib.GetFontDefault();
        
        _camera = new Camera2D
        {
            target = Vector2.Zero,
            offset = new Vector2(640, 360),
            rotation = 0.0f,
            zoom = 1.0f
        };

        while (!Raylib.WindowShouldClose())
        {
            Update();
            Draw();
        }

        Cleanup();
        Raylib.CloseWindow();
    }

    private void Update()
    {
        switch (_currentState)
        {
            case AppState.Menu:
                UpdateMenu();
                break;
            case AppState.Generating:
                UpdateGenerating();
                break;
            case AppState.Running:
                UpdateRunning();
                break;
        }
    }

    private void UpdateMenu()
    {
        var mousePos = Raylib.GetMousePosition();
        var seedRect = new Rectangle(640 - 200 * _uiScale, 250 * _uiScale, 400 * _uiScale, 40 * _uiScale);
        
        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            _seedFocused = CheckCollisionPointRec(mousePos, seedRect);
        }
        
        if (_seedFocused)
        {
            int key = Raylib.GetKeyPressed();
            while (key != -1)
            {
                if (key == (int)KeyboardKey.KEY_BACKSPACE)
                {
                    if (_seedInput.Length > 0) _seedInput = _seedInput.Substring(0, _seedInput.Length - 1);
                }
                else if (key >= 32 && key <= 126)
                {
                    _seedInput += (char)key;
                }
                key = Raylib.GetKeyPressed();
            }
        }
        
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_EQUAL)) _uiScale = MathF.Min(_uiScale + 0.1f, 2.0f);
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_MINUS)) _uiScale = MathF.Max(_uiScale - 0.1f, 0.5f);
    }

    private void UpdateGenerating()
    {
        // Synchronous generation handles its own rendering
    }

    private void UpdateRunning()
    {
        if (_renderer == null || _world == null) return;

        // Initialize simulation spawn if not already done
        if (!_simulationInitialized && _simulation != null)
        {
            _simulation.SpawnInitialSettlements();
            _simulationInitialized = true;
        }

        // Update simulation
        _simulation?.Update(16); // 16ms per frame (~60 FPS)

        if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_MIDDLE) || 
           (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) && _isDragging))
        {
            Vector2 delta = Raylib.GetMouseDelta();
            _camera.target -= delta / _camera.zoom;
            _isDragging = true;
        }
        else
        {
            _isDragging = false;
        }

        float wheel = Raylib.GetMouseWheelMove();
        if (wheel != 0)
        {
            Vector2 mouseWorldPos = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _camera);
            _camera.zoom *= (wheel > 0) ? 1.1f : 0.9f;
            _camera.zoom = MathF.Max(0.1f, MathF.Min(10.0f, _camera.zoom));
            
            Vector2 newMouseWorldPos = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), _camera);
            _camera.target += (mouseWorldPos - newMouseWorldPos);
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT) && !_isDragging)
        {
            Vector2 screenPos = Raylib.GetMousePosition();
            Vector2 worldPos = Raylib.GetScreenToWorld2D(screenPos, _camera);
            
            int x = (int)(worldPos.X + _world.Width / 2);
            int y = (int)(worldPos.Y + _world.Height / 2);
            
            if (x >= 0 && x < _world.Width && y >= 0 && y < _world.Height)
            {
                _selectionRect = new Rectangle(x, y, 1, 1);
            }
        }
        
        if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_RIGHT))
        {
            _selectionRect = null;
            _showSettings = false;
        }
        
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_S))
        {
            _showSettings = !_showSettings;
        }
        
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ONE)) _displayLayer = 0;
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_TWO)) _displayLayer = 1;
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_THREE)) _displayLayer = 2;
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_FOUR)) _displayLayer = 3;
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_FIVE)) _displayLayer = 4;
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_SIX)) _displayLayer = 5;
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_SEVEN)) _displayLayer = 6;
        
        if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
        {
            _currentState = AppState.Menu;
            CleanupRunningState();
        }
    }

    private void Draw()
    {
        Raylib.ClearBackground(_bgColor);
        
        switch (_currentState)
        {
            case AppState.Menu:
                DrawMenu();
                break;
            case AppState.Generating:
                DrawGenerating();
                break;
            case AppState.Running:
                DrawRunning();
                break;
        }
        
        Raylib.DrawFPS(10, 10);
    }

    private void DrawMenu()
    {
        float centerX = 640;
        float centerY = 360;
        float scale = _uiScale;
        
        DrawTextCentered("LIVING PROCEDURAL WORLD", centerX, centerY - 200 * scale, 40 * scale, _accentColor);
        DrawTextCentered("SIMULATION", centerX, centerY - 160 * scale, 40 * scale, _accentColor);
        
        Rectangle panelRect = new(centerX - 300 * scale, centerY - 100 * scale, 600 * scale, 400 * scale);
        Raylib.DrawRectangleRec(panelRect, _panelColor);
        Raylib.DrawRectangleLinesEx(panelRect, 2, _accentColor);
        
        float yPos = centerY - 60 * scale;
        float labelHeight = 30 * scale;
        float inputHeight = 40 * scale;
        float gap = 20 * scale;
        
        DrawTextScaled("Simulation Mode:", centerX - 280 * scale, yPos, 20 * scale, _textColor);
        yPos += labelHeight + gap;
        
        Rectangle graphicalBtn = new(centerX - 280 * scale, yPos, 280 * scale, inputHeight);
        Rectangle headlessBtn = new(centerX + 10 * scale, yPos, 280 * scale, inputHeight);
        
        Raylib.DrawRectangleRec(graphicalBtn, _selectedMode == 0 ? _accentColor : Color.Gray);
        Raylib.DrawRectangleRec(headlessBtn, _selectedMode == 1 ? _accentColor : Color.Gray);
        
        DrawTextCentered("GRAPHICAL", graphicalBtn.X + graphicalBtn.Width/2, graphicalBtn.Y + inputHeight/2 - 10*scale, 20*scale, Color.Black);
        DrawTextCentered("HEADLESS (LOGS)", headlessBtn.X + headlessBtn.Width/2, headlessBtn.Y + inputHeight/2 - 10*scale, 20*scale, Color.Black);
        
        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), graphicalBtn) && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            _selectedMode = 0;
        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), headlessBtn) && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            _selectedMode = 1;
            
        yPos += inputHeight + gap * 2;
        
        DrawTextScaled("World Seed:", centerX - 280 * scale, yPos, 20 * scale, _textColor);
        yPos += labelHeight + gap;
        
        Rectangle seedRect = new(centerX - 280 * scale, yPos, 560 * scale, inputHeight);
        Raylib.DrawRectangleRec(seedRect, _seedFocused ? Color.White : Color.DarkGray);
        Raylib.DrawRectangleLinesEx(seedRect, 2, _seedFocused ? _accentColor : Color.Gray);
        
        string displaySeed = _seedFocused && (Raylib.GetTime() % 1.0 < 0.5) ? _seedInput + "_" : _seedInput;
        DrawTextCentered(displaySeed, seedRect.X + seedRect.Width/2, seedRect.Y + inputHeight/2 - 10*scale, 20*scale, Color.Black);
        
        yPos += inputHeight + gap * 2;
        
        DrawTextScaled("Map Size:", centerX - 280 * scale, yPos, 20 * scale, _textColor);
        yPos += labelHeight + gap;
        
        string sizeLabel = $"{_mapSizes[_mapSizeIndex]} x {_mapSizes[_mapSizeIndex]}";
        Rectangle prevBtn = new(centerX - 280 * scale, yPos, 100 * scale, inputHeight);
        Rectangle nextBtn = new(centerX + 180 * scale, yPos, 100 * scale, inputHeight);
        Rectangle labelRect = new(centerX - 170 * scale, yPos, 340 * scale, inputHeight);
        
        Raylib.DrawRectangleRec(prevBtn, Color.Gray);
        Raylib.DrawRectangleRec(nextBtn, Color.Gray);
        Raylib.DrawRectangleRec(labelRect, Color.DarkGray);
        
        DrawTextCentered("<", prevBtn.X + prevBtn.Width/2, prevBtn.Y + inputHeight/2 - 10*scale, 20*scale, Color.White);
        DrawTextCentered(sizeLabel, labelRect.X + labelRect.Width/2, labelRect.Y + inputHeight/2 - 10*scale, 20*scale, Color.White);
        DrawTextCentered(">", nextBtn.X + nextBtn.Width/2, nextBtn.Y + inputHeight/2 - 10*scale, 20*scale, Color.White);
        
        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), prevBtn) && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            _mapSizeIndex = Math.Max(0, _mapSizeIndex - 1);
        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), nextBtn) && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            _mapSizeIndex = Math.Min(_mapSizes.Length - 1, _mapSizeIndex + 1);
            
        yPos += inputHeight + gap * 3;
        
        Rectangle startBtn = new(centerX - 200 * scale, yPos, 400 * scale, 60 * scale);
        Color startColor = Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), startBtn) ? Color.Green : Color.Lime;
        Raylib.DrawRectangleRec(startBtn, startColor);
        DrawTextCentered("GENERATE WORLD", startBtn.X + startBtn.Width/2, startBtn.Y + 60*scale/2 - 20*scale, 30*scale, Color.Black);
        
        if (Raylib.CheckCollisionPointRec(Raylib.GetMousePosition(), startBtn) && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
        {
            StartGeneration();
        }
        
        DrawTextCentered($"UI Scale: +/- Keys | Current: {_uiScale:F1}x", centerX, 680, 16*scale, Color.Gray);
    }

    private void DrawGenerating()
    {
        float centerX = 640;
        float centerY = 360;
        float scale = _uiScale;
        
        DrawTextCentered("GENERATING WORLD...", centerX, centerY - 50 * scale, 40 * scale, _accentColor);
        
        Rectangle barBg = new(centerX - 300 * scale, centerY, 600 * scale, 40 * scale);
        Rectangle barFill = new(barBg.X, barBg.Y, barBg.Width * _generationProgress, barBg.Height);
        
        Raylib.DrawRectangleRec(barBg, Color.Gray);
        Raylib.DrawRectangleRec(barFill, _accentColor);
        Raylib.DrawRectangleLinesEx(barBg, 2, Color.White);
        
        DrawTextCentered($"{(int)(_generationProgress * 100)}%", centerX, centerY + 60 * scale, 30 * scale, Color.White);
        DrawTextCentered(_generationLog, centerX, centerY + 100 * scale, 20 * scale, Color.LightGray);
    }

    private void DrawRunning()
    {
        if (_world == null || _renderer == null) return;
        
        Raylib.BeginMode2D(_camera);
        
        // 1. Рисуем мир
        _renderer.Render();
        
        // 2. Рисуем сущности (поселения, юниты) через SimulationManager
        _simulation?.DrawEntities(_camera);
        
        // 3. Рисуем выделение
        if (_selectionRect.HasValue)
        {
            var rect = _selectionRect.Value;
            Vector2 worldPos = new Vector2(rect.X - _world.Width / 2, rect.Y - _world.Height / 2);
            float cellSize = GetCellSize();
            if (cellSize < 10) cellSize = 10;
            
            Raylib.DrawCircleLines(worldPos.X * cellSize + cellSize/2, worldPos.Y * cellSize + cellSize/2, cellSize/2 + 2, Color.Red);
        }
        
        Raylib.EndMode2D();
        DrawGuiOverlay();
    }

    private void DrawGuiOverlay()
    {
        float scale = _uiScale;
        
        Raylib.DrawRectangle(0, 0, 1280, 40 * scale, new Color(0, 0, 0, 180));
        DrawTextScaled($"World: {_world?.Width}x{_world?.Height} | Seed: {_seedInput} | Layer: {GetLayerName(_displayLayer)}", 20, 10, 20*scale, Color.White);
        DrawTextScaled("ESC: Menu | S: Settings | 1-7: Layers | LMB Drag: Pan | LMB Click: Select", 600, 10, 20*scale, Color.LightGray);
        
        if (_selectionRect.HasValue && _world != null)
        {
            var rect = _selectionRect.Value;
            int x = (int)rect.X;
            int y = (int)rect.Y;
            
            string biomeName = _world.Biomes[x, y].ToString().Replace("_", " ");
            float height = _world.HeightMap[x, y];
            float temp = _world.Temperature[x, y];
            float moisture = _world.Moisture[x, y];
            float fertility = _world.GetFertility(x, y);
            float erosion = _world.GetErosion(x, y);
            
            // Calculate dynamic panel height based on data
            float panelW = 320 * scale;
            float basePanelH = 280 * scale;
            float panelX = 1280 - panelW - 20;
            float panelY = 60 * scale;
            
            Raylib.DrawRectangleRec(new Rectangle(panelX, panelY, panelW, basePanelH), _panelColor);
            Raylib.DrawRectangleLinesEx(new Rectangle(panelX, panelY, panelW, basePanelH), 2, _accentColor);
            
            float yPos = panelY + 15 * scale;
            DrawTextScaled($"Cell: [{x}, {y}]", panelX + 15 * scale, yPos, 22 * scale, _accentColor);
            yPos += 30 * scale;
            
            DrawTextScaled($"Biome: {biomeName}", panelX + 15 * scale, yPos, 18 * scale, _textColor);
            yPos += 25 * scale;
            
            DrawTextScaled($"Height: {height:F3}", panelX + 15 * scale, yPos, 18 * scale, _textColor);
            yPos += 25 * scale;
            
            DrawTextScaled($"Temperature: {temp:F2}", panelX + 15 * scale, yPos, 18 * scale, _textColor);
            yPos += 25 * scale;
            
            DrawTextScaled($"Moisture: {moisture:F2}", panelX + 15 * scale, yPos, 18 * scale, _textColor);
            yPos += 25 * scale;
            
            DrawTextScaled($"Fertility: {fertility:F2}", panelX + 15 * scale, yPos, 18 * scale, _textColor);
            yPos += 25 * scale;
            
            DrawTextScaled($"Erosion: {erosion:F2}", panelX + 15 * scale, yPos, 18 * scale, _textColor);
            yPos += 25 * scale;
            
            // Show resources if available
            if (_world.TryGetLayer<ResourceLayer>("resources", out var resLayer))
            {
                var resources = resLayer.GetResources(x, y);
                if (resources.Count > 0)
                {
                    DrawTextScaled($"Resources:", panelX + 15 * scale, yPos, 18 * scale, Color.Gold);
                    yPos += 25 * scale;
                    
                    foreach (var res in resources.Take(3)) // Show max 3 resources
                    {
                        DrawTextScaled($"  • {res.Key}: {res.Value:F1}", panelX + 15 * scale, yPos, 16 * scale, Color.LightGray);
                        yPos += 20 * scale;
                    }
                    if (resources.Count > 3)
                    {
                        DrawTextScaled($"  ... and {resources.Count - 3} more", panelX + 15 * scale, yPos, 14 * scale, Color.Gray);
                    }
                }
            }
        }
        
        if (_showSettings)
        {
            float panelW = 250 * scale;
            float panelH = 200 * scale;
            float panelX = 20;
            float panelY = 60 * scale;
            
            Raylib.DrawRectangleRec(new Rectangle(panelX, panelY, panelW, panelH), _panelColor);
            Raylib.DrawRectangleLinesEx(new Rectangle(panelX, panelY, panelW, panelH), 2, _accentColor);
            
            DrawTextScaled("SETTINGS", panelX + 15 * scale, panelY + 15 * scale, 22 * scale, _accentColor);
            DrawTextScaled($"UI Scale: {_uiScale:F1}", panelX + 15 * scale, panelY + 50 * scale, 18 * scale, _textColor);
            DrawTextScaled("Use +/- to adjust", panelX + 15 * scale, panelY + 75 * scale, 14 * scale, Color.Gray);
            DrawTextScaled($"Zoom: {_camera?.zoom:F2}x", panelX + 15 * scale, panelY + 100 * scale, 16 * scale, Color.LightGray);
            DrawTextScaled("Mouse Wheel to zoom", panelX + 15 * scale, panelY + 120 * scale, 14 * scale, Color.Gray);
        }
    }

    private void StartGeneration()
    {
        _currentState = AppState.Generating;
        
        int size = _mapSizes[_mapSizeIndex];
        ulong seed = ulong.TryParse(_seedInput, out var s) ? s : 12345;
        
        try
        {
            _generationLog = "Initializing...";
            _generationProgress = 0.1f;
            
            var generator = new WorldGenerator();
            generator.RegisterModule(new HeightMapGenerator(size, size));
            generator.RegisterModule(new TemperatureGenerator(size, size));
            generator.RegisterModule(new MoistureGenerator(size, size));
            generator.RegisterModule(new ErosionGenerator(size, size));
            generator.RegisterModule(new FertilityGenerator(size, size));
            generator.RegisterModule(new ResourceGenerator(size, size));
            generator.RegisterModule(new BiomeGenerator());
            
            _generationLog = "Generating Height...";
            _generationProgress = 0.3f;
            RenderGenerationStep();
            
            _generationLog = "Simulating Climate...";
            _generationProgress = 0.5f;
            RenderGenerationStep();
            
            _generationLog = "Calculating Biomes...";
            _generationProgress = 0.7f;
            RenderGenerationStep();
            
            _generationLog = "Finalizing...";
            _generationProgress = 0.9f;
            
            _world = generator.Generate(size, size, seed);
            
            _generationProgress = 1.0f;
            RenderGenerationStep();
            Raylib.WaitForCompletion(200);
            
            // Create simulation context and manager
            var context = new WorldContext(_world!);
            _simulation = new SimulationManager(context);
            _simulation.Initialize();
            _renderer = new UnifiedWorldRenderer(_world!, _simulation);
            _camera.target = Vector2.Zero;
            _camera.zoom = 1.0f;
            
            _currentState = AppState.Running;
        }
        catch (Exception ex)
        {
            _generationLog = $"ERROR: {ex.Message}";
            Raylib.WaitForCompletion(2000);
            _currentState = AppState.Menu;
        }
    }

    private void RenderGenerationStep()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(_bgColor);
        DrawGenerating();
        Raylib.EndDrawing();
        Raylib.SwapBuffers();
    }

    private void CleanupRunningState()
    {
        _renderer?.Dispose();
        _renderer = null;
        _world = null;
        _selectionRect = null;
    }

    private void Cleanup()
    {
        CleanupRunningState();
    }

    private bool CheckCollisionPointRec(Vector2 point, Rectangle rec)
    {
        return point.X >= rec.X && point.X <= rec.X + rec.Width &&
               point.Y >= rec.Y && point.Y <= rec.Y + rec.Height;
    }

    private void DrawTextCentered(string text, float x, float y, float fontSize, Color color)
    {
        int measure = (int)Raylib.MeasureTextEx(_font, text, fontSize, 0).X;
        Raylib.DrawTextEx(_font, text, new Vector2(x - measure / 2, y), fontSize, 0, color);
    }

    private void DrawTextScaled(string text, float x, float y, float fontSize, Color color)
    {
        Raylib.DrawTextEx(_font, text, new Vector2(x, y), fontSize, 0, color);
    }
    
    private float GetCellSize()
    {
        if (_world == null) return 10;
        
        // Base cell size is 4 pixels, scaled by camera zoom
        return 4.0f * _camera.zoom;
    }

    private string GetLayerName(int index)
    {
        return index switch 
        {
            0 => "Biomes",
            1 => "Height",
            2 => "Temperature",
            3 => "Moisture",
            4 => "Erosion",
            5 => "Fertility",
            6 => "Resources",
            _ => "Unknown"
        };
    }
}
