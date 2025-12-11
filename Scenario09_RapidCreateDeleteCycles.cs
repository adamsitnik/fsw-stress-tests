namespace FswStressTests;

/// <summary>
/// Scenario 9: Rapid create/delete cycles
/// Tests handling rapid creation and deletion of the same file
/// </summary>
public class Scenario09_RapidCreateDeleteCycles : IStressScenario
{
    public string Name => "Rapid Create/Delete Cycles";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_rapid_create_delete_cycles_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            int createCount = 0;
            int deleteCount = 0;
            int cycles = 20;

            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Created += (s, e) => Interlocked.Increment(ref createCount);
            watcher.Deleted += (s, e) => Interlocked.Increment(ref deleteCount);

            string filePath = Path.Combine(testDir, "cycle.txt");

            // Rapid create/delete cycles
            for (int i = 0; i < cycles; i++)
            {
                await File.WriteAllTextAsync(filePath, $"Cycle {i}");
                await Task.Delay(50);
                File.Delete(filePath);
                await Task.Delay(50);
            }

            // Wait for events to be processed
            await Task.Delay(1000);

            // We should have detected at least some create and delete events
            if (createCount == 0)
            {
                throw new InvalidOperationException("No create events detected in rapid cycles");
            }

            if (deleteCount == 0)
            {
                throw new InvalidOperationException("No delete events detected in rapid cycles");
            }

            // Check we got a reasonable number of events
            if (createCount < cycles / 2 || deleteCount < cycles / 2)
            {
                throw new InvalidOperationException(
                    $"Too few events detected. Creates: {createCount}/{cycles}, Deletes: {deleteCount}/{cycles}");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
