namespace LivingWorld.App;

using LivingWorld.Core;
using LivingWorld.World;
using LivingWorld.Generation;
using LivingWorld.Validation;

/// <summary>
/// Headless world generation and simulation tool.
/// Can generate worlds, run simulations, and validate results without rendering.
/// </summary>
public sealed class HeadlessWorldTool
{
    private readonly int _width;
    private readonly int _height;
    private readonly ulong _seed;
    
    public HeadlessWorldTool(int width, int height, ulong seed)
    {
        _width = width;
        _height = height;
        _seed = seed;
    }
    
    /// <summary>
    /// Generate a complete world with all standard layers.
    /// </summary>
    public WorldData GenerateWorld()
    {
        Console.WriteLine($"Generating world {_width}x{_height} with seed {_seed}...");
        
        var generator = new WorldGenerator();
        
        // Register generation modules in dependency order
        generator.RegisterModule(new HeightMapGenerator(_width, _height));
        generator.RegisterModule(new TemperatureGenerator(_width, _height));
        generator.RegisterModule(new MoistureGenerator(_width, _height));
        generator.RegisterModule(new BiomeGenerator());
        
        var world = generator.Generate(_width, _height, _seed);
        
        Console.WriteLine($"World generated successfully!");
        Console.WriteLine($"  Layers: {string.Join(", ", world.AllLayers.Keys)}");
        
        return world;
    }
    
    /// <summary>
    /// Run validation on the world.
    /// </summary>
    public bool ValidateWorld(WorldData world, WorldGenerator generator)
    {
        Console.WriteLine("\nRunning validators...");
        
        var validators = new List<IWorldValidator>
        {
            new WorldIntegrityValidator(world),
            new DeterminismValidator(world, generator, _width, _height, _seed)
        };
        
        bool allValid = true;
        
        foreach (var validator in validators)
        {
            Console.WriteLine($"\nValidator: {validator.ValidatorId}");
            
            foreach (var result in validator.Validate())
            {
                if (result.IsValid)
                {
                    Console.WriteLine($"  ✓ {result.CheckName}");
                }
                else
                {
                    Console.WriteLine($"  ✗ {result.CheckName}: {result.Message}");
                    allValid = false;
                    
                    if (result.Exception != null)
                    {
                        Console.WriteLine($"    Exception: {result.Exception}");
                    }
                }
            }
        }
        
        Console.WriteLine($"\nValidation {(allValid ? "PASSED" : "FAILED")}");
        return allValid;
    }
    
    /// <summary>
    /// Print world statistics.
    /// </summary>
    public void PrintStatistics(WorldData world)
    {
        Console.WriteLine("\n=== World Statistics ===");
        
        // Biome distribution
        if (world.TryGetLayer<BiomeLayer>("biome", out var biomeLayer))
        {
            var biomeCounts = new Dictionary<BiomeType, int>();
            
            for (int y = 0; y < biomeLayer.Height; y++)
            {
                for (int x = 0; x < biomeLayer.Width; x++)
                {
                    var biome = biomeLayer.GetBiome(x, y);
                    if (!biomeCounts.ContainsKey(biome))
                        biomeCounts[biome] = 0;
                    biomeCounts[biome]++;
                }
            }
            
            Console.WriteLine("\nBiome Distribution:");
            int totalCells = _width * _height;
            
            foreach (var kvp in biomeCounts.OrderByDescending(k => k.Value))
            {
                float percent = 100f * kvp.Value / totalCells;
                Console.WriteLine($"  {kvp.Key}: {kvp.Value} ({percent:F2}%)");
            }
        }
        
        // Height statistics
        if (world.TryGetLayer<HeightLayer>("height", out var heightLayer))
        {
            Console.WriteLine($"\nHeight Range: {heightLayer.MinHeight:F4} - {heightLayer.MaxHeight:F4}");
        }
    }
    
    /// <summary>
    /// Run complete headless pipeline.
    /// </summary>
    public bool RunPipeline()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            
            var world = GenerateWorld();
            var generator = CreateGenerator();
            
            PrintStatistics(world);
            
            bool valid = ValidateWorld(world, generator);
            
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;
            
            Console.WriteLine($"\nTotal time: {duration.TotalSeconds:F2}s");
            
            return valid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nPipeline failed: {ex}");
            return false;
        }
    }
    
    private WorldGenerator CreateGenerator()
    {
        var generator = new WorldGenerator();
        generator.RegisterModule(new HeightMapGenerator(_width, _height));
        generator.RegisterModule(new TemperatureGenerator(_width, _height));
        generator.RegisterModule(new MoistureGenerator(_width, _height));
        generator.RegisterModule(new BiomeGenerator());
        return generator;
    }
}

/// <summary>
/// Main entry point for headless mode.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Living Procedural World Simulation ===");
        
        // Parse arguments or use defaults
        int width = 512;
        int height = 512;
        ulong seed = 12345;
        bool graphicalMode = false;
        int cellSize = 10;
        float fontSize = 18f;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--width":
                case "-w":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var w))
                        width = w;
                    break;
                case "--height":
                case "-h":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var h))
                        height = h;
                    break;
                case "--seed":
                case "-s":
                    if (i + 1 < args.Length && ulong.TryParse(args[++i], out var s))
                        seed = s;
                    break;
                case "--graphical":
                case "-g":
                    graphicalMode = true;
                    break;
                case "--cellsize":
                case "-c":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out var c))
                        cellSize = c;
                    break;
                case "--fontsize":
                case "-f":
                    if (i + 1 < args.Length && float.TryParse(args[++i], out var f))
                        fontSize = f;
                    break;
                case "--help":
                    PrintHelp();
                    return;
            }
        }
        
        if (graphicalMode)
        {
            Console.WriteLine("Graphical Mode\n");
            var app = new GraphicalMenuApp();
            app.Run();
        }
        else
        {
            Console.WriteLine("Headless Mode\n");
            var tool = new HeadlessWorldTool(width, height, seed);
            bool success = tool.RunPipeline();
            Environment.Exit(success ? 0 : 1);
        }
    }
    
    private static void PrintHelp()
    {
        Console.WriteLine(@"
Living Procedural World Simulation

Usage: LivingWorld.App [options]

Options:
  -w, --width <value>      World width in cells (default: 512)
  -h, --height <value>     World height in cells (default: 512)
  -s, --seed <value>       World seed (default: 12345)
  -g, --graphical          Run in graphical mode (default: headless)
  -c, --cellsize <value>   Base cell size in pixels for graphical mode (default: 10)
  -f, --fontsize <value>   Font size for GUI text (default: 18)
  --help                   Show this help message

Examples:
  LivingWorld.App --width 256 --height 256 --seed 42
  LivingWorld.App -w 1024 -h 1024 -s 99999 --graphical
  LivingWorld.App -g  # Graphical mode with default settings
  LivingWorld.App -g -w 512 -h 512 -c 12 -f 20  # Larger cells and text
");
    }
}
