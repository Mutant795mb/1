namespace LivingWorld.Simulation.Systems;

using LivingWorld.Core;
using LivingWorld.Simulation.Entities;
using LivingWorld.Simulation.Settlements;
using Raylib_cs;
using System.Numerics;

/// <summary>
/// Manages all simulation systems and the main simulation loop.
/// </summary>
public class SimulationManager : IWorldModule
{
    private readonly List<ISimulationSystem> _systems = new();
    private readonly WorldContext _context;
    private bool _isInitialized;
    
    public string Name => "Simulation Manager";
    public bool IsInitialized => _isInitialized;
    
    // Fixed timestep configuration
    private const float FixedDeltaTime = 1f / 60f; // 60 updates per second
    private float _accumulator = 0f;
    
    // Entity pooling - avoid allocations in hot path
    private readonly List<IEntity> _activeEntities = new();
    
    public SimulationManager(WorldContext context)
    {
        _context = context;
    }
    
    public void Initialize()
    {
        if (_isInitialized) return;
        
        // Register default simulation systems in priority order
        RegisterSystem(new EntitySimulationSystem());
        RegisterSystem(new SettlementSimulationSystem());
        RegisterSystem(new EcologySimulationSystem());
        
        foreach (var system in _systems)
        {
            system.Initialize();
        }
        
        _isInitialized = true;
    }
    
    public void Shutdown()
    {
        foreach (var system in _systems)
        {
            system.Shutdown();
        }
        _systems.Clear();
        _isInitialized = false;
    }
    
    public void RegisterSystem(ISimulationSystem system)
    {
        _systems.Add(system);
        _systems.Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }
    
    public void Update(float deltaTime)
    {
        if (!_isInitialized) return;
        
        // Accumulate time and run fixed timestep updates
        _accumulator += deltaTime / 1000f; // Convert ms to seconds
        
        while (_accumulator >= FixedDeltaTime)
        {
            RunFixedUpdate(FixedDeltaTime);
            _accumulator -= FixedDeltaTime;
        }
    }
    
    private void RunFixedUpdate(float fixedDeltaTime)
    {
        _context.DeltaTime = fixedDeltaTime;
        _context.SimulationTime += fixedDeltaTime;
        
        foreach (var system in _systems)
        {
            system.Update(fixedDeltaTime, _context);
        }
    }
    
    public void DrawEntities(Camera2D camera)
    {
        if (!_isInitialized) return;
        
        foreach (var system in _systems)
        {
            if (system is SettlementSimulationSystem settlementSystem)
            {
                // Use the pre-allocated buffer from the system instead of LINQ
                foreach (var settlement in settlementSystem.Settlements)
                {
                    if (settlement.IsActive)
                    {
                        DrawSettlement(settlement, camera);
                    }
                }
            }
        }
    }
    
    private void DrawSettlement(Settlement settlement, Camera2D camera)
    {
        float cellSize = 4.0f * camera.zoom;
        if (cellSize < 3) cellSize = 3;
        
        Vector2 screenPos = Raylib.GetWorldToScreen2D(
            new Vector2(settlement.X - settlement.World!.Width / 2, settlement.Y - settlement.World.Height / 2), 
            camera
        );
        
        // Draw settlement as a colored square
        Color color = settlement.Population > 100 ? Color.Gold : Color.Orange;
        float size = MathF.Max(4, settlement.Population / 10f) * cellSize / 4f;
        
        Raylib.DrawRectangle(
            screenPos.X - size/2, 
            screenPos.Y - size/2, 
            size, 
            size, 
            color
        );
        
        // Draw population text if zoomed in enough
        if (camera.zoom > 0.5f)
        {
            Raylib.DrawText($"{settlement.Population}", 
                (int)(screenPos.X - 5), 
                (int)(screenPos.Y - 15), 
                10, 
                Color.White
            );
        }
    }
    
    public void SpawnInitialSettlements()
    {
        if (!_isInitialized) return;
        
        var settlementSystem = _systems.OfType<SettlementSimulationSystem>().FirstOrDefault();
        if (settlementSystem == null) return;
        
        int worldWidth = _context.World.Width;
        int worldHeight = _context.World.Height;
        int numSettlements = Math.Max(5, (worldWidth * worldHeight) / 10000);
        
        var rng = new Random(12345); // Deterministic spawn
        
        for (int i = 0; i < numSettlements; i++)
        {
            int x = rng.Next(worldWidth);
            int y = rng.Next(worldHeight);
            
            // Check if location is suitable (not ocean, good fertility)
            var biome = _context.World.Biomes[x, y];
            if (biome == BiomeType.Ocean || biome == BiomeType.Beach) continue;
            
            float fertility = _context.World.GetFertility(x, y);
            if (fertility < 0.3f) continue;
            
            var settlement = new Settlement(x, y, rng.Next(20, 80));
            settlementSystem.AddSettlement(settlement);
            _context.Entities.Add(settlement);
        }
    }
}

/// <summary>
/// Simulation system for managing entities (animals, humans).
/// </summary>
public class EntitySimulationSystem : ISimulationSystem
{
    public string Name => "Entity System";
    public bool IsInitialized { get; private set; }
    public int Priority => 10;
    
    // Pre-allocated list to avoid allocations in hot path
    private readonly List<IEntity> _activeEntitiesBuffer = new();
    
    public void Initialize() => IsInitialized = true;
    public void Shutdown() => IsInitialized = false;
    
    public void Update(float deltaTime, WorldContext context)
    {
        if (!IsInitialized) return;
        
        // Clear buffer without allocation
        _activeEntitiesBuffer.Clear();
        
        // Collect active entities without LINQ allocation
        foreach (var entity in context.Entities)
        {
            if (entity.IsActive)
            {
                _activeEntitiesBuffer.Add(entity);
            }
        }
        
        // Update all entities
        foreach (var entity in _activeEntitiesBuffer)
        {
            entity.Update(deltaTime);
        }
        
        // Remove dead entities
        context.Entities.RemoveAll(e => !e.IsActive);
        
        // Spawn new entities randomly based on biome fertility
        SpawnEntities(deltaTime, context);
    }
    
    public void Render(WorldContext context)
    {
        // Rendering handled by InteractiveWorldRenderer
    }
    
    private void SpawnEntities(float deltaTime, WorldContext context)
    {
        // Simple spawning logic - can be enhanced
        if (context.World == null) return;
        
        // Chance to spawn animals in fertile areas
        // This is a placeholder for more sophisticated spawning logic
    }
}

/// <summary>
/// Simulation system for managing settlements.
/// </summary>
public class SettlementSimulationSystem : ISimulationSystem
{
    public string Name => "Settlement System";
    public bool IsInitialized { get; private set; }
    public int Priority => 20;
    
    private readonly List<Settlement> _settlements = new();
    
    // Pre-allocated buffer to avoid allocations in hot path
    private readonly List<Settlement> _activeSettlementsBuffer = new();
    
    public IReadOnlyList<Settlement> Settlements => _settlements.AsReadOnly();
    
    public void Initialize() => IsInitialized = true;
    public void Shutdown()
    {
        _settlements.Clear();
        IsInitialized = false;
    }
    
    public void Update(float deltaTime, WorldContext context)
    {
        if (!IsInitialized || context.World == null) return;
        
        // Clear buffer without allocation
        _activeSettlementsBuffer.Clear();
        
        // Collect active settlements without LINQ allocation
        foreach (var settlement in _settlements)
        {
            if (settlement.IsActive)
            {
                _activeSettlementsBuffer.Add(settlement);
            }
        }
        
        // Update all settlements
        foreach (var settlement in _activeSettlementsBuffer)
        {
            settlement.Update(deltaTime, context);
        }
        
        // Remove abandoned settlements
        _settlements.RemoveAll(s => !s.IsActive);
        
        // Try to found new settlements
        FoundNewSettlements(context);
    }
    
    public void Render(WorldContext context)
    {
        // Rendering handled by InteractiveWorldRenderer
    }
    
    public void AddSettlement(Settlement settlement)
    {
        _settlements.Add(settlement);
    }
    
    private void FoundNewSettlements(WorldContext context)
    {
        // Placeholder for settlement founding logic
        // Should check for suitable locations based on fertility, water, etc.
    }
}

/// <summary>
/// Simulation system for ecology (predator-prey, vegetation).
/// </summary>
public class EcologySimulationSystem : ISimulationSystem
{
    public string Name => "Ecology System";
    public bool IsInitialized { get; private set; }
    public int Priority => 30;
    
    public void Initialize() => IsInitialized = true;
    public void Shutdown() => IsInitialized = false;
    
    public void Update(float deltaTime, WorldContext context)
    {
        if (!IsInitialized) return;
        
        // Placeholder for ecology simulation
        // Predator-prey dynamics, vegetation growth, etc.
    }
    
    public void Render(WorldContext context)
    {
        // Rendering handled by InteractiveWorldRenderer
    }
}
