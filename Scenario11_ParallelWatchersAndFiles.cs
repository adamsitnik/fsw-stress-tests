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
            System.Collections.Concurrent.ConcurrentBag<(string Name, string FullPath)> watcher1Events = new();
            System.Collections.Concurrent.ConcurrentBag<(string Name, string FullPath)> watcher2Events = new();
            System.Collections.Concurrent.ConcurrentBag<(string Name, string FullPath)> watcher3Events = new();
            int fileCount = 10;

            // Create watchers in parallel
            FileSystemWatcher[] watchers = new FileSystemWatcher[3];
            Parallel.For(0, 3, i =>
            {
                FileSystemWatcher w = new(testDir)
                {
                    NotifyFilter = NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                
                if (i == 0)
                    w.Created += (s, e) => watcher1Events.Add((e.Name!, e.FullPath));
                else if (i == 1)
                    w.Created += (s, e) => watcher2Events.Add((e.Name!, e.FullPath));
                else
                    w.Created += (s, e) => watcher3Events.Add((e.Name!, e.FullPath));
                
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
                if (watcher1Events.Count != fileCount || watcher2Events.Count != fileCount || watcher3Events.Count != fileCount)
                {
                    throw new InvalidOperationException(
                        $"All watchers should have detected exactly {fileCount} events. " +
                        $"W1: {watcher1Events.Count}, W2: {watcher2Events.Count}, W3: {watcher3Events.Count}");
                }

                // Verify all watchers saw the same files
                HashSet<string> files1 = new(watcher1Events.Select(e => e.Name));
                HashSet<string> files2 = new(watcher2Events.Select(e => e.Name));
                HashSet<string> files3 = new(watcher3Events.Select(e => e.Name));

                if (!files1.SetEquals(files2) || !files1.SetEquals(files3))
                {
                    throw new InvalidOperationException(
                        $"Watchers detected different files. " +
                        $"W1: {string.Join(", ", files1)}, " +
                        $"W2: {string.Join(", ", files2)}, " +
                        $"W3: {string.Join(", ", files3)}");
                }

                // Verify paths match
                foreach ((string name, string fullPath) in watcher1Events)
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
