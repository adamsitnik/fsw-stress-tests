using System.Diagnostics;

namespace FswStressTests;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== FileSystemWatcher Stress Tests ===");
        Console.WriteLine($"Started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine();

        List<IStressScenario> scenarios = new()
        {
            new Scenario01_BasicFileCreation(),
            new Scenario02_MultipleFileOperations(),
            new Scenario03_DirectoryCreation(),
            new Scenario04_RecursiveMonitoring(),
            new Scenario05_HighFrequencyOperations(),
            new Scenario06_LargeFileMonitoring(),
            new Scenario07_ConcurrentWatchers(),
            new Scenario08_RenameOperations(),
            new Scenario09_RapidCreateDeleteCycles(),
            new Scenario10_NestedDirectoryOperations(),
            new Scenario11_ParallelWatchersAndFiles(),
            new Scenario12_ParallelCreateDeleteCycles()
        };

        List<(string ScenarioName, Exception Error, TimeSpan Duration)> failures = new();
        Stopwatch totalStopwatch = Stopwatch.StartNew();

        for (int i = 0; i < scenarios.Count; i++)
        {
            IStressScenario scenario = scenarios[i];
            Console.WriteLine($"[{i + 1}/{scenarios.Count}] Running: {scenario.Name}");
            
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                await scenario.RunAsync();
                stopwatch.Stop();
                Console.WriteLine($"    ✓ PASSED in {stopwatch.Elapsed.TotalSeconds:F2}s");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"    ✗ FAILED in {stopwatch.Elapsed.TotalSeconds:F2}s");
                failures.Add((scenario.Name, ex, stopwatch.Elapsed));
            }
            
            Console.WriteLine();
        }

        totalStopwatch.Stop();

        // Summary
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"Total scenarios: {scenarios.Count}");
        Console.WriteLine($"Passed: {scenarios.Count - failures.Count}");
        Console.WriteLine($"Failed: {failures.Count}");
        Console.WriteLine($"Total execution time: {totalStopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine();

        // Report failures
        if (failures.Count > 0)
        {
            Console.WriteLine("=== Failures ===");
            foreach (var (scenarioName, error, duration) in failures)
            {
                Console.WriteLine($"- {scenarioName} (failed after {duration.TotalSeconds:F2}s)");
                Console.WriteLine($"  Error: {error.GetType().Name}: {error.Message}");
                if (error.StackTrace != null)
                {
                    Console.WriteLine($"  Stack Trace:");
                    foreach (string line in error.StackTrace.Split('\n'))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            Console.WriteLine($"    {line.Trim()}");
                        }
                    }
                }
                Console.WriteLine();
            }
            return 1; // Exit with error code
        }

        Console.WriteLine("All tests passed!");
        return 0;
    }
}
