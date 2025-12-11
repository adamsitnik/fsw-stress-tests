namespace FswStressTests;

/// <summary>
/// Scenario 11: Parallel watchers and file creation
/// Tests multiple FileSystemWatcher instances created in parallel, monitoring files created in parallel
/// </summary>
public class Scenario11_ParallelWatchersAndFiles : IStressScenario
{
    public string Name => "Parallel Watchers and File Creation";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_parallel_watchers_and_files_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            int watcherCount = Environment.ProcessorCount;
            System.Collections.Concurrent.ConcurrentBag<(string Name, string FullPath)>[] watcherEvents = new System.Collections.Concurrent.ConcurrentBag<(string Name, string FullPath)>[watcherCount];
            for (int i = 0; i < watcherCount; i++)
            {
                watcherEvents[i] = new();
            }
            int fileCount = 10;

            // Create watchers in parallel
            FileSystemWatcher[] watchers = new FileSystemWatcher[watcherCount];
            Parallel.For(0, Environment.ProcessorCount, i =>
            {
                FileSystemWatcher w = new(testDir)
                {
                    NotifyFilter = NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                
                int index = i; // Capture for lambda
                w.Created += (s, e) => watcherEvents[index].Add((e.Name!, e.FullPath));
                
                watchers[i] = w;
            });

            using (watchers[0])
            using (watchers[1])
            using (watchers[2])
            {
                // Create files in parallel
                Parallel.For(0, fileCount, i =>
                {
                    string filePath = Path.Combine(testDir, $"parallel_{i}.txt");
                    File.WriteAllText(filePath, $"Content {i}");
                });

                // Wait for events to be processed
                await Task.Delay(2000);

                // All watchers should have detected the same number of events
                for (int i = 0; i < watcherCount; i++)
                {
                    if (watcherEvents[i].Count != fileCount)
                    {
                        string eventCounts = string.Join(", ", watcherEvents.Select((e, idx) => $"W{idx}: {e.Count}"));
                        throw new InvalidOperationException(
                            $"All watchers should have detected exactly {fileCount} events. {eventCounts}");
                    }
                }

                // Verify all watchers saw the same files
                HashSet<string> files0 = new(watcherEvents[0].Select(e => e.Name));
                for (int i = 1; i < watcherCount; i++)
                {
                    HashSet<string> filesI = new(watcherEvents[i].Select(e => e.Name));
                    if (!files0.SetEquals(filesI))
                    {
                        throw new InvalidOperationException(
                            $"Watchers detected different files. " +
                            $"W0: {string.Join(", ", files0)}, " +
                            $"W{i}: {string.Join(", ", filesI)}");
                    }
                }

                // Verify paths match
                foreach ((string name, string fullPath) in watcherEvents[0])
                {
                    string expectedPath = Path.Combine(testDir, name);
                    if (fullPath != expectedPath)
                    {
                        throw new InvalidOperationException(
                            $"Path mismatch for '{name}': expected '{expectedPath}', got '{fullPath}'");
                    }
                }
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
