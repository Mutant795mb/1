namespace LivingWorld.Validation;

using LivingWorld.Core;
using LivingWorld.World;
using LivingWorld.Generation;

/// <summary>
/// Validates world generation integrity.
/// </summary>
public sealed class WorldIntegrityValidator : IWorldValidator
{
    private readonly WorldData _world;
    
    public string ValidatorId => "world_integrity";
    
    public WorldIntegrityValidator(WorldData world)
    {
        _world = world;
    }
    
    public IEnumerable<ValidationResult> Validate()
    {
        yield return ValidateDimensions();
        yield return ValidateHeightRange();
        yield return ValidateBiomeConsistency();
        yield return ValidateLayerDependencies();
    }
    
    private ValidationResult ValidateDimensions()
    {
        try
        {
            if (_world.Width <= 0 || _world.Height <= 0)
                return ValidationResult.Failure(ValidatorId, "Invalid world dimensions");
            
            if (_world.Width > 10000 || _world.Height > 10000)
                return ValidationResult.Failure(ValidatorId, "World dimensions exceed maximum");
            
            return ValidationResult.Success(ValidatorId);
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ValidatorId, ex);
        }
    }
    
    private ValidationResult ValidateHeightRange()
    {
        try
        {
            if (!_world.TryGetLayer<HeightLayer>("height", out var heightLayer))
                return ValidationResult.Failure(ValidatorId, "Height layer missing");
            
            for (int y = 0; y < Math.Min(100, heightLayer.Height); y++)
            {
                for (int x = 0; x < Math.Min(100, heightLayer.Width); x++)
                {
                    float h = heightLayer.GetNormalizedHeight(x, y);
                    if (h < 0f || h > 1f)
                        return ValidationResult.Failure(ValidatorId, $"Height out of range at ({x},{y}): {h}");
                }
            }
            
            return ValidationResult.Success(ValidatorId);
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ValidatorId, ex);
        }
    }
    
    private ValidationResult ValidateBiomeConsistency()
    {
        try
        {
            if (!_world.TryGetLayer<BiomeLayer>("biome", out var biomeLayer))
                return ValidationResult.Failure(ValidatorId, "Biome layer missing");
            
            // Sample check: ocean should be at low elevations
            if (_world.TryGetLayer<HeightLayer>("height", out var heightLayer))
            {
                int oceanAtHighElevation = 0;
                int sampleCount = 0;
                
                for (int y = 0; y < biomeLayer.Height; y += 10)
                {
                    for (int x = 0; x < biomeLayer.Width; x += 10)
                    {
                        sampleCount++;
                        var biome = biomeLayer.GetBiome(x, y);
                        float height = heightLayer.GetNormalizedHeight(x, y);
                        
                        if (biome == BiomeType.Ocean && height > 0.4f)
                            oceanAtHighElevation++;
                    }
                }
                
                // Allow some tolerance for edge cases
                if (sampleCount > 0 && (float)oceanAtHighElevation / sampleCount > 0.1f)
                    return ValidationResult.Failure(ValidatorId, "Too many ocean tiles at high elevation");
            }
            
            return ValidationResult.Success(ValidatorId);
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ValidatorId, ex);
        }
    }
    
    private ValidationResult ValidateLayerDependencies()
    {
        try
        {
            // Check that all declared dependencies exist
            foreach (var layer in _world.AllLayers.Values)
            {
                foreach (var dep in layer.Dependencies)
                {
                    if (!_world.HasLayer(dep))
                        return ValidationResult.Failure(ValidatorId, $"Layer {layer.LayerId} depends on missing layer {dep}");
                }
            }
            
            return ValidationResult.Success(ValidatorId);
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ValidatorId, ex);
        }
    }
}

/// <summary>
/// Validates determinism by regenerating and comparing.
/// </summary>
public sealed class DeterminismValidator : IWorldValidator
{
    private readonly WorldData _originalWorld;
    private readonly WorldGenerator _generator;
    private readonly int _width;
    private readonly int _height;
    private readonly ulong _seed;
    
    public string ValidatorId => "determinism";
    
    public DeterminismValidator(WorldData world, WorldGenerator generator, int width, int height, ulong seed)
    {
        _originalWorld = world;
        _generator = generator;
        _width = width;
        _height = height;
        _seed = seed;
    }
    
    public IEnumerable<ValidationResult> Validate()
    {
        yield return ValidateHeightDeterminism();
    }
    
    private ValidationResult ValidateHeightDeterminism()
    {
        try
        {
            // Regenerate world with same seed
            var regeneratedWorld = _generator.Generate(_width, _height, _seed);
            
            if (!regeneratedWorld.TryGetLayer<HeightLayer>("height", out var regeneratedHeight))
                return ValidationResult.Failure(ValidatorId, "Failed to regenerate height layer");
            
            if (!_originalWorld.TryGetLayer<HeightLayer>("height", out var originalHeight))
                return ValidationResult.Failure(ValidatorId, "Original height layer missing");
            
            // Compare samples
            int mismatches = 0;
            int samples = 0;
            
            for (int y = 0; y < _height; y += 7)
            {
                for (int x = 0; x < _width; x += 7)
                {
                    samples++;
                    float orig = originalHeight.GetHeight(x, y);
                    float regen = regeneratedHeight.GetHeight(x, y);
                    
                    if (MathF.Abs(orig - regen) > 0.0001f)
                        mismatches++;
                }
            }
            
            if (mismatches > 0)
                return ValidationResult.Failure(ValidatorId, $"Non-deterministic generation detected: {mismatches}/{samples} mismatches");
            
            return ValidationResult.Success(ValidatorId);
        }
        catch (Exception ex)
        {
            return ValidationResult.Error(ValidatorId, ex);
        }
    }
}
