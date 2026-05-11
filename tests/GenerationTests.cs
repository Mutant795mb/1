namespace LivingWorld.Tests;

using LivingWorld.Core;
using LivingWorld.World;
using LivingWorld.Generation;
using LivingWorld.Validation;

public class DeterministicRngTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var rng1 = new DeterministicRng(12345);
        var rng2 = new DeterministicRng(12345);
        
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(rng1.NextDouble(), rng2.NextDouble());
        }
    }
    
    [Fact]
    public void DifferentSeed_ProducesDifferentSequence()
    {
        var rng1 = new DeterministicRng(12345);
        var rng2 = new DeterministicRng(54321);
        
        bool different = false;
        for (int i = 0; i < 100; i++)
        {
            if (rng1.NextDouble() != rng2.NextDouble())
            {
                different = true;
                break;
            }
        }
        
        Assert.True(different, "Different seeds should produce different sequences");
    }
    
    [Fact]
    public void NextInt_ReturnsValueInRange()
    {
        var rng = new DeterministicRng(12345);
        
        for (int i = 0; i < 1000; i++)
        {
            int value = rng.NextInt(10, 20);
            Assert.InRange(value, 10, 20);
        }
    }
}

public class HeightMapGeneratorTests
{
    [Fact]
    public void Generate_ProducesValidHeightLayer()
    {
        var generator = new HeightMapGenerator(64, 64, octaves: 4);
        var layer = generator.Generate(12345, new Dictionary<string, IGenerationLayer>());
        
        Assert.IsType<HeightLayer>(layer);
        var heightLayer = (HeightLayer)layer;
        
        Assert.Equal(64, heightLayer.Width);
        Assert.Equal(64, heightLayer.Height);
        Assert.Equal("height", heightLayer.LayerId);
        Assert.Empty(heightLayer.Dependencies);
    }
    
    [Fact]
    public void Generate_SameSeed_ProducesSameHeights()
    {
        var generator = new HeightMapGenerator(32, 32);
        
        var layer1 = generator.Generate(42, new Dictionary<string, IGenerationLayer>());
        var layer2 = generator.Generate(42, new Dictionary<string, IGenerationLayer>());
        
        var h1 = (HeightLayer)layer1;
        var h2 = (HeightLayer)layer2;
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                Assert.Equal(h1.GetHeight(x, y), h2.GetHeight(x, y), 5);
            }
        }
    }
    
    [Fact]
    public void Generate_HeightsAreNormalized()
    {
        var generator = new HeightMapGenerator(64, 64);
        var layer = (HeightLayer)generator.Generate(12345, new Dictionary<string, IGenerationLayer>());
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float normalized = layer.GetNormalizedHeight(x, y);
                Assert.InRange(normalized, 0f, 1f);
            }
        }
    }
}

public class BiomeGeneratorTests
{
    [Fact]
    public void Generate_RequiresDependencies()
    {
        var generator = new BiomeGenerator();
        
        Assert.Contains("height", generator.RequiredInputLayers);
        Assert.Contains("temperature", generator.RequiredInputLayers);
        Assert.Contains("moisture", generator.RequiredInputLayers);
    }
    
    [Fact]
    public void Generate_ProducesValidBiomeLayer()
    {
        var width = 64;
        var height = 64;
        
        // First generate dependencies
        var heightGen = new HeightMapGenerator(width, height);
        var tempGen = new TemperatureGenerator(width, height);
        var moistureGen = new MoistureGenerator(width, height);
        var biomeGen = new BiomeGenerator();
        
        var layers = new Dictionary<string, IGenerationLayer>();
        layers["height"] = heightGen.Generate(12345, layers);
        layers["temperature"] = tempGen.Generate(12345, layers);
        layers["moisture"] = moistureGen.Generate(12345, layers);
        
        var biomeLayer = (BiomeLayer)biomeGen.Generate(12345, layers);
        
        Assert.Equal(width, biomeLayer.Width);
        Assert.Equal(height, biomeLayer.Height);
        Assert.Equal("biome", biomeLayer.LayerId);
    }
}

public class WorldGeneratorTests
{
    [Fact]
    public void Generate_CompleteWorld_HasAllLayers()
    {
        var generator = new WorldGenerator();
        generator.RegisterModule(new HeightMapGenerator(64, 64));
        generator.RegisterModule(new TemperatureGenerator(64, 64));
        generator.RegisterModule(new MoistureGenerator(64, 64));
        generator.RegisterModule(new BiomeGenerator());
        
        var world = generator.Generate(64, 64, 12345);
        
        Assert.True(world.HasLayer("height"));
        Assert.True(world.HasLayer("temperature"));
        Assert.True(world.HasLayer("moisture"));
        Assert.True(world.HasLayer("biome"));
    }
    
    [Fact]
    public void Generate_SameSeed_ProducesIdenticalWorld()
    {
        var createGenerator = () =>
        {
            var g = new WorldGenerator();
            g.RegisterModule(new HeightMapGenerator(32, 32));
            g.RegisterModule(new TemperatureGenerator(32, 32));
            g.RegisterModule(new MoistureGenerator(32, 32));
            g.RegisterModule(new BiomeGenerator());
            return g;
        };
        
        var world1 = createGenerator().Generate(32, 32, 42);
        var world2 = createGenerator().Generate(32, 32, 42);
        
        var h1 = world1.GetLayer<HeightLayer>("height");
        var h2 = world2.GetLayer<HeightLayer>("height");
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                Assert.Equal(h1.GetHeight(x, y), h2.GetHeight(x, y), 5);
            }
        }
    }
}

public class WorldIntegrityValidatorTests
{
    [Fact]
    public void Validate_ValidWorld_ReturnsSuccess()
    {
        var generator = new WorldGenerator();
        generator.RegisterModule(new HeightMapGenerator(64, 64));
        generator.RegisterModule(new TemperatureGenerator(64, 64));
        generator.RegisterModule(new MoistureGenerator(64, 64));
        generator.RegisterModule(new BiomeGenerator());
        
        var world = generator.Generate(64, 64, 12345);
        var validator = new WorldIntegrityValidator(world);
        
        var results = validator.Validate().ToList();
        
        Assert.All(results, r => Assert.True(r.IsValid, $"{r.CheckName}: {r.Message}"));
    }
}
