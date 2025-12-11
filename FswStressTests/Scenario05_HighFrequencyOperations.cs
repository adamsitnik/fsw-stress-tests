namespace FswStressTests;

/// <summary>
/// Scenario 5: High-frequency file operations
/// Tests handling many file operations in rapid succession
/// </summary>
public class Scenario05_HighFrequencyOperations : IStressScenario
{
    public string Name => "High-Frequency File Operations";

    public async Task RunAsync()
    {
        var testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var eventCount = 0;
            var fileCount = 50;

            using var watcher = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                Interlocked.Increment(ref eventCount);
            };

            // Create many files rapidly
            for (int i = 0; i < fileCount; i++)
            {
                var filePath = Path.Combine(testDir, $"file_{i}.txt");
                await File.WriteAllTextAsync(filePath, $"Content {i}");
            }

            // Wait a bit for events to be processed
            await Task.Delay(2000);

            // We should have received at least some events
            // (not necessarily all due to event coalescing in some systems)
            if (eventCount == 0)
            {
                throw new InvalidOperationException("No file creation events were detected");
            }

            if (eventCount < fileCount / 2)
            {
                throw new InvalidOperationException(
                    $"Too few events detected: {eventCount} out of {fileCount} files created");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
