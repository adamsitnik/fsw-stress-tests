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
            int createCount = 0;
            int deleteCount = 0;
            int parallelFiles = 5;
            int cyclesPerFile = 10;

            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Created += (s, e) => Interlocked.Increment(ref createCount);
            watcher.Deleted += (s, e) => Interlocked.Increment(ref deleteCount);

            // Perform create/delete cycles in parallel for multiple files
            Task[] cycleTasks = new Task[parallelFiles];
            for (int fileIndex = 0; fileIndex < parallelFiles; fileIndex++)
            {
                int index = fileIndex; // Capture for lambda
                cycleTasks[fileIndex] = Task.Run(async () =>
                {
                    string filePath = Path.Combine(testDir, $"cycle_{index}.txt");
                    for (int cycle = 0; cycle < cyclesPerFile; cycle++)
                    {
                        await File.WriteAllTextAsync(filePath, $"File {index}, Cycle {cycle}");
                        await Task.Delay(50);
                        File.Delete(filePath);
                        await Task.Delay(50);
                    }
                });
            }

            await Task.WhenAll(cycleTasks);

            // Wait for events to be processed
            await Task.Delay(1000);

            int expectedMinEvents = parallelFiles * cyclesPerFile / 2;

            // We should have detected at least some create and delete events
            if (createCount == 0)
            {
                throw new InvalidOperationException("No create events detected in parallel rapid cycles");
            }

            if (deleteCount == 0)
            {
                throw new InvalidOperationException("No delete events detected in parallel rapid cycles");
            }

            // Check we got a reasonable number of events
            if (createCount < expectedMinEvents || deleteCount < expectedMinEvents)
            {
                throw new InvalidOperationException(
                    $"Too few events detected. Creates: {createCount} (expected >= {expectedMinEvents}), " +
                    $"Deletes: {deleteCount} (expected >= {expectedMinEvents})");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
