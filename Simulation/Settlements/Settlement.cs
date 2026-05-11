namespace LivingWorld.Simulation.Settlements;

using LivingWorld.Core;

/// <summary>
/// Represents a settlement (village, town, city) in the world.
/// </summary>
public class Settlement : IEntity
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Name { get; set; }
    public Vector2 Position { get; set; }
    public int Population { get; set; } = 10;
    public float FoodStockpile { get; set; } = 100f;
    public float GrowthProgress { get; set; } = 0f;
    public bool IsActive { get; set; } = true;
    
    // For grid-based positioning
    public int X { get; set; }
    public int Y { get; set; }
    public WorldData? World { get; set; }

    public enum SettlementType
    {
        Village,
        Town,
        City
    }

    public SettlementType Type { get; private set; } = SettlementType.Village;

    public Settlement(int x, int y, int initialPopulation = 10)
    {
        X = x;
        Y = y;
        Position = new Vector2(x, y);
        Population = initialPopulation;
        Name = $"Settlement-{Id.ToString().Substring(0, 4)}";
    }

    public void Update(float deltaTime, WorldContext context)
    {
        if (!IsActive) return;
        
        World = context.World;

        // Consume food
        float foodConsumption = Population * deltaTime * 0.5f;
        FoodStockpile -= foodConsumption;

        if (FoodStockpile <= 0f)
        {
            // Starvation - population decreases
            Population -= (int)(deltaTime * 2f);
            if (Population <= 0)
            {
                IsActive = false;
                return;
            }
            FoodStockpile = 0;
        }
        else
        {
            // Gather food from surroundings based on fertility
            float fertility = context.World.GetFertility(X, Y);
            float foodProduction = fertility * Population * deltaTime * 0.3f;
            FoodStockpile += foodProduction;
            
            // Cap food stockpile
            if (FoodStockpile > Population * 50f)
                FoodStockpile = Population * 50f;

            // Growth when well-fed
            GrowthProgress += deltaTime * (Population * 0.1f) * (1f + fertility);

            // Type-based growth thresholds
            float growthThreshold = Type switch
            {
                SettlementType.Village => 100f,
                SettlementType.Town => 500f,
                SettlementType.City => 2000f,
                _ => 100f
            };

            if (GrowthProgress >= growthThreshold && Population < GetMaxPopulation())
            {
                Population += 5;
                GrowthProgress = 0;

                // Upgrade settlement type
                if (Population > 50 && Type == SettlementType.Village)
                    Type = SettlementType.Town;
                else if (Population > 200 && Type == SettlementType.Town)
                    Type = SettlementType.City;
            }
        }
    }
    
    public void Update(float deltaTime)
    {
        // IEntity interface implementation - not used for settlements
    }

    private int GetMaxPopulation() => Type switch
    {
        SettlementType.Village => 50,
        SettlementType.Town => 200,
        SettlementType.City => 1000,
        _ => 50
    };
}
