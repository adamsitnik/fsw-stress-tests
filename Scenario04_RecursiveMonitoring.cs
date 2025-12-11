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
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_recursive_monitoring_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            List<string> events = new();
            int expectedEvents = 3;
            TaskCompletionSource<bool> tcs = new();

            using FileSystemWatcher watcher = new(testDir)
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
            string level1 = Path.Combine(testDir, "level1");
            Directory.CreateDirectory(level1);
            await Task.Delay(50);

            string level2 = Path.Combine(level1, "level2");
            Directory.CreateDirectory(level2);
            await Task.Delay(50);

            string file = Path.Combine(level2, "deep.txt");
            await File.WriteAllTextAsync(file, "Deep file");
            await Task.Delay(50);

            // Wait for all events
            Task timeoutTask = Task.Delay(5000);
            Task completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException(
                    $"Expected {expectedEvents} events but got {events.Count}. " +
                    $"Events: {string.Join(", ", events)}");
            }

            // Verify we got exactly the expected events
            if (events.Count != expectedEvents)
            {
                throw new InvalidOperationException(
                    $"Expected exactly {expectedEvents} create events, but got {events.Count}. " +
                    $"Events: {string.Join(", ", events)}");
            }

            // Verify the paths match what we created
            HashSet<string> expectedPaths = new() { level1, level2, file };
            HashSet<string> actualPaths = new(events);

            if (!expectedPaths.SetEquals(actualPaths))
            {
                string missing = string.Join(", ", expectedPaths.Except(actualPaths));
                string unexpected = string.Join(", ", actualPaths.Except(expectedPaths));
                throw new InvalidOperationException(
                    $"Event paths don't match. " +
                    $"Expected: {string.Join(", ", expectedPaths)}. " +
                    $"Actual: {string.Join(", ", actualPaths)}. " +
                    (string.IsNullOrEmpty(missing) ? "" : $"Missing: {missing}. ") +
                    (string.IsNullOrEmpty(unexpected) ? "" : $"Unexpected: {unexpected}."));
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
