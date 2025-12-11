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
        var testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var watcher1Events = 0;
            var watcher2Events = 0;
            var watcher3Events = 0;

            using var watcher1 = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            using var watcher2 = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            using var watcher3 = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher1.Created += (s, e) => Interlocked.Increment(ref watcher1Events);
            watcher2.Created += (s, e) => Interlocked.Increment(ref watcher2Events);
            watcher3.Created += (s, e) => Interlocked.Increment(ref watcher3Events);

            // Create test files
            for (int i = 0; i < 5; i++)
            {
                var filePath = Path.Combine(testDir, $"test_{i}.txt");
                await File.WriteAllTextAsync(filePath, $"Content {i}");
                await Task.Delay(100);
            }

            // Wait for events to be processed
            await Task.Delay(1000);

            // All watchers should have detected events
            if (watcher1Events == 0 || watcher2Events == 0 || watcher3Events == 0)
            {
                throw new InvalidOperationException(
                    $"Not all watchers detected events. W1: {watcher1Events}, W2: {watcher2Events}, W3: {watcher3Events}");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
