namespace LivingWorld.Core;

/// <summary>
/// Base class for simulation systems.
/// Each system has its own state, tick rate, and dependencies.
/// </summary>
public abstract class SimulationSystem
{
    /// <summary>
    /// Unique identifier for this system.
    /// </summary>
    public abstract string SystemId { get; }
    
    /// <summary>
    /// How often this system updates (in simulation ticks).
    /// 1 = every tick, 60 = once per 60 ticks, etc.
    /// </summary>
    public abstract int TickRate { get; }
    
    /// <summary>
    /// IDs of other systems this system depends on.
    /// </summary>
    public abstract IReadOnlyList<string> Dependencies { get; }
    
    /// <summary>
    /// Whether this system is enabled.
    /// </summary>
    public bool IsEnabled { get; protected set; } = true;
    
    /// <summary>
    /// Initialize the system with world state.
    /// </summary>
    public virtual void Initialize() { }
    
    /// <summary>
    /// Update the system. Called according to TickRate.
    /// </summary>
    /// <param name="tick">Current simulation tick.</param>
    /// <param name="deltaTime">Time since last update.</param>
    public abstract void Update(long tick, double deltaTime);
    
    /// <summary>
    /// Get debug metrics for this system.
    /// </summary>
    public virtual IDictionary<string, object> GetMetrics() => new Dictionary<string, object>();
    
    /// <summary>
    /// Enable or disable the system.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }
}
