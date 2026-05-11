namespace LivingWorld.Core;

/// <summary>
/// Base interface for all generation layers.
/// Each layer is immutable and represents a specific aspect of the world.
/// </summary>
public interface IGenerationLayer
{
    /// <summary>
    /// Unique identifier for this layer type.
    /// </summary>
    string LayerId { get; }
    
    /// <summary>
    /// List of layer IDs this layer depends on.
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }
    
    /// <summary>
    /// World dimensions for this layer.
    /// </summary>
    int Width { get; }
    int Height { get; }
}

/// <summary>
/// Base interface for generation modules that create layers.
/// </summary>
public interface IGenerationModule
{
    /// <summary>
    /// The ID of the layer this module produces.
    /// </summary>
    string OutputLayerId { get; }
    
    /// <summary>
    /// IDs of layers this module requires as input.
    /// </summary>
    IReadOnlyList<string> RequiredInputLayers { get; }
    
    /// <summary>
    /// Generate the output layer from input layers.
    /// </summary>
    /// <param name="worldSeed">The deterministic world seed.</param>
    /// <param name="inputLayers">Dictionary of input layers by their IDs.</param>
    /// <returns>The generated output layer.</returns>
    IGenerationLayer Generate(ulong worldSeed, IReadOnlyDictionary<string, IGenerationLayer> inputLayers);
}
