namespace LivingWorld.Core;

/// <summary>
/// Base interface for all world modules.
/// </summary>
public interface IWorldModule
{
    string Name { get; }
    bool IsInitialized { get; }
    void Initialize();
    void Shutdown();
}

/// <summary>
/// Base interface for all world data layers.
/// </summary>
public interface IWorldLayer
{
    string LayerId { get; }
    int Width { get; }
    int Height { get; }
}

/// <summary>
/// Base interface for all generation layers.
/// </summary>
public interface IGenerationLayer : IWorldLayer
{
    IReadOnlyList<string> Dependencies { get; }
}

/// <summary>
/// Interface for generation modules that produce world layers.
/// </summary>
public interface IGenerationModule : IWorldModule
{
    string OutputLayerId { get; }
    IReadOnlyList<string> RequiredInputLayers { get; }
    IGenerationLayer Generate(ulong seed, IReadOnlyDictionary<string, IWorldLayer> inputLayers, int width, int height);
}

/// <summary>
/// Interface for simulation systems that update world state over time.
/// </summary>
public interface ISimulationSystem : IWorldModule
{
    int Priority { get; }
    void Update(float deltaTime, WorldContext context);
    void Render(WorldContext context);
}

/// <summary>
/// Interface for entities in the world.
/// </summary>
public interface IEntity
{
    Guid Id { get; }
    Vector2 Position { get; set; }
    bool IsActive { get; set; }
    void Update(float deltaTime);
}

/// <summary>
/// Global world context passed to all systems.
/// </summary>
public sealed class WorldContext
{
    public WorldData World { get; set; } = null!;
    public float SimulationTime { get; set; }
    public float DeltaTime { get; set; }
    public List<IEntity> Entities { get; set; } = new();
    
    public WorldContext(WorldData world)
    {
        World = world;
    }
}
