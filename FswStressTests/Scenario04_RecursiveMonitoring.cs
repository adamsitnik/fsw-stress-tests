namespace FswStressTests;

/// <summary>
/// Scenario 4: Recursive directory monitoring
/// Tests watching subdirectories recursively
/// </summary>
public class Scenario04_RecursiveMonitoring : IStressScenario
{
    public string Name => "Recursive Directory Monitoring";

    public async Task RunAsync()
    {
        var testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var events = new List<string>();
            var expectedEvents = 3;
            var tcs = new TaskCompletionSource<bool>();

            using var watcher = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                events.Add(e.FullPath);
                if (events.Count >= expectedEvents)
                {
                    tcs.TrySetResult(true);
                }
            };

            // Create nested structure
            var level1 = Path.Combine(testDir, "level1");
            Directory.CreateDirectory(level1);
            await Task.Delay(50);

            var level2 = Path.Combine(level1, "level2");
            Directory.CreateDirectory(level2);
            await Task.Delay(50);

            var file = Path.Combine(level2, "deep.txt");
            await File.WriteAllTextAsync(file, "Deep file");
            await Task.Delay(50);

            // Wait for all events
            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException(
                    $"Expected {expectedEvents} events but got {events.Count}. " +
                    $"Events: {string.Join(", ", events)}");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
