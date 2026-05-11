namespace LivingWorld.Simulation.Entities;

using LivingWorld.Core;

/// <summary>
/// Base class for all entities in the world.
/// </summary>
public abstract class BaseEntity : IEntity
{
    public Guid Id { get; } = Guid.NewGuid();
    public Vector2 Position { get; set; }
    public bool IsActive { get; set; } = true;
    
    public abstract void Update(float deltaTime);
}

/// <summary>
/// Simple animal entity with basic needs.
/// </summary>
public class AnimalEntity : BaseEntity
{
    public enum AnimalType
    {
        Herbivore,
        Carnivore
    }
    
    public AnimalType Type { get; set; }
    public float Health { get; set; } = 100f;
    public float Hunger { get; set; } = 0f;
    public float Age { get; set; } = 0f;
    public int MovementSpeed { get; set; } = 1;
    
    public override void Update(float deltaTime)
    {
        if (!IsActive) return;
        
        Age += deltaTime;
        Hunger += deltaTime * 5f; // Hunger increases over time
        
        if (Hunger >= 100f || Health <= 0f)
        {
            IsActive = false; // Entity dies
        }
    }
}

/// <summary>
/// Human settler entity that can form settlements.
/// </summary>
public class HumanEntity : BaseEntity
{
    public string? SettlementId { get; set; }
    public float Food { get; set; } = 10f;
    public float Happiness { get; set; } = 50f;
    
    public override void Update(float deltaTime)
    {
        if (!IsActive) return;
        
        Food -= deltaTime * 2f; // Consumes food over time
        
        if (Food <= 0f)
        {
            Happiness -= deltaTime * 10f;
            if (Happiness <= 0f)
            {
                IsActive = false; // Entity dies or leaves
            }
        }
    }
}
