namespace LivingWorld.Core;

/// <summary>
/// Result of a validation check.
/// </summary>
public sealed record ValidationResult(
    bool IsValid,
    string CheckName,
    string? Message = null,
    Exception? Exception = null
)
{
    public static ValidationResult Success(string checkName) 
        => new(true, checkName);
    
    public static ValidationResult Failure(string checkName, string message) 
        => new(false, checkName, message);
    
    public static ValidationResult Error(string checkName, Exception ex) 
        => new(false, checkName, "Exception occurred", ex);
}

/// <summary>
/// Interface for world validators.
/// Validators check the integrity and consistency of world data.
/// </summary>
public interface IWorldValidator
{
    /// <summary>
    /// Unique identifier for this validator.
    /// </summary>
    string ValidatorId { get; }
    
    /// <summary>
    /// Run validation checks on the world state.
    /// </summary>
    IEnumerable<ValidationResult> Validate();
}

/// <summary>
/// Interface for simulation validators.
/// Validators check the integrity of simulation state and behavior.
/// </summary>
public interface ISimulationValidator
{
    /// <summary>
    /// Unique identifier for this validator.
    /// </summary>
    string ValidatorId { get; }
    
    /// <summary>
    /// Run validation checks on the simulation.
    /// </summary>
    IEnumerable<ValidationResult> Validate();
}
