namespace FswStressTests;

/// <summary>
/// Scenario 7: Concurrent watchers on same directory
/// Tests multiple FileSystemWatcher instances monitoring the same directory
/// </summary>
public class Scenario07_ConcurrentWatchers : IStressScenario
{
    public string Name => "Concurrent Watchers";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_concurrent_watchers_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            int watcher1Events = 0;
            int watcher2Events = 0;
            int watcher3Events = 0;

            using FileSystemWatcher watcher1 = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            using FileSystemWatcher watcher2 = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            using FileSystemWatcher watcher3 = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher1.Created += (s, e) => Interlocked.Increment(ref watcher1Events);
            watcher2.Created += (s, e) => Interlocked.Increment(ref watcher2Events);
            watcher3.Created += (s, e) => Interlocked.Increment(ref watcher3Events);

            int fileCount = 5;
            // Create test files
            for (int i = 0; i < fileCount; i++)
            {
                string filePath = Path.Combine(testDir, $"test_{i}.txt");
                await File.WriteAllTextAsync(filePath, $"Content {i}");
            }

            // Wait for events to be processed
            await Task.Delay(1000);

            // All watchers should have detected the same number of events
            if (watcher1Events != fileCount || watcher2Events != fileCount || watcher3Events != fileCount)
            {
                throw new InvalidOperationException(
                    $"All watchers should have detected exactly {fileCount} events. " +
                    $"W1: {watcher1Events}, W2: {watcher2Events}, W3: {watcher3Events}");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
