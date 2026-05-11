namespace LivingWorld.Core;

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
/// Interface for generation modules that produce world layers.
/// </summary>
public interface IGenerationModule
{
    string OutputLayerId { get; }
    IEnumerable<string> RequiredInputLayers { get; }
    IWorldLayer Generate(ulong seed, IReadOnlyDictionary<string, IWorldLayer> inputLayers, int width, int height);
}

/// <summary>
/// Interface for simulation systems that update world state over time.
/// </summary>
public interface ISimulationSystem
{
    string SystemId { get; }
    void Initialize(WorldContext context);
    void Update(float deltaTime, WorldContext context);
    void Render(WorldContext context);
}

/// <summary>
/// Global world context passed to all systems.
/// </summary>
public sealed class WorldContext
{
    public WorldData World { get; set; } = null!;
    public float SimulationTime { get; set; }
    public float DeltaTime { get; set; }
}
