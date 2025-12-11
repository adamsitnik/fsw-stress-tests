namespace FswStressTests;

/// <summary>
/// Scenario 12: Parallel create/delete cycles
/// Tests handling rapid creation and deletion of multiple files in parallel
/// </summary>
public class Scenario12_ParallelCreateDeleteCycles : IStressScenario
{
    public string Name => "Parallel Create/Delete Cycles";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_parallel_create_delete_cycles_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            System.Collections.Concurrent.ConcurrentBag<(string EventType, string Name, string FullPath)> events = new();
            int parallelFiles = 5;
            int cyclesPerFile = 10;

            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Created += (s, e) => events.Add(("Created", e.Name!, e.FullPath));
            watcher.Deleted += (s, e) => events.Add(("Deleted", e.Name!, e.FullPath));

            // Perform create/delete cycles in parallel for multiple files
            Parallel.For(0, parallelFiles, fileIndex =>
            {
                string filePath = Path.Combine(testDir, $"cycle_{fileIndex}.txt");
                for (int cycle = 0; cycle < cyclesPerFile; cycle++)
                {
                    File.WriteAllText(filePath, $"File {fileIndex}, Cycle {cycle}");
                    Thread.Sleep(50);
                    File.Delete(filePath);
                    Thread.Sleep(50);
                }
            });

            // Wait for events to be processed
            await Task.Delay(1000);

            // Separate events by type
            List<(string EventType, string Name, string FullPath)> createEvents = 
                events.Where(e => e.EventType == "Created").ToList();
            List<(string EventType, string Name, string FullPath)> deleteEvents = 
                events.Where(e => e.EventType == "Deleted").ToList();

            // We expect at least half the events due to potential event coalescing in parallel scenarios
            int expectedMinEvents = parallelFiles * cyclesPerFile / 2;

            // We should have detected at least some create and delete events
            if (createEvents.Count == 0)
            {
                throw new InvalidOperationException("No create events detected in parallel rapid cycles");
            }

            if (deleteEvents.Count == 0)
            {
                throw new InvalidOperationException("No delete events detected in parallel rapid cycles");
            }

            // Check we got a reasonable number of events
            if (createEvents.Count < expectedMinEvents || deleteEvents.Count < expectedMinEvents)
            {
                throw new InvalidOperationException(
                    $"Too few events detected. Creates: {createEvents.Count} (expected >= {expectedMinEvents}), " +
                    $"Deletes: {deleteEvents.Count} (expected >= {expectedMinEvents})");
            }

            // Verify all events are for expected files
            HashSet<string> expectedFileNames = new();
            for (int i = 0; i < parallelFiles; i++)
            {
                expectedFileNames.Add($"cycle_{i}.txt");
            }

            foreach ((string eventType, string name, string fullPath) in events)
            {
                if (!expectedFileNames.Contains(name))
                {
                    throw new InvalidOperationException(
                        $"Unexpected file in {eventType} event: '{name}'. Expected one of: {string.Join(", ", expectedFileNames)}");
                }

                string expectedPath = Path.Combine(testDir, name);
                if (fullPath != expectedPath)
                {
                    throw new InvalidOperationException(
                        $"Path mismatch for '{name}': expected '{expectedPath}', got '{fullPath}'");
                }
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
