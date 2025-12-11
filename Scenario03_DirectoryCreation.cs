namespace FswStressTests;

/// <summary>
/// Scenario 3: Directory creation and monitoring
/// Tests watching for directory creation events
/// </summary>
public class Scenario03_DirectoryCreation : IStressScenario
{
    public string Name => "Directory Creation";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_directory_creation_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            List<(string Name, string FullPath)> createdEvents = new();
            TaskCompletionSource<bool> tcs = new();

            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                createdEvents.Add((e.Name!, e.FullPath));
                if (e.Name == "subdir")
                {
                    tcs.TrySetResult(true);
                }
            };

            // Create subdirectory
            string expectedSubDir = Path.Combine(testDir, "subdir");
            Directory.CreateDirectory(expectedSubDir);

            // Wait for event
            Task timeoutTask = Task.Delay(1000);
            Task completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Directory creation event was not received within timeout");
            }

            // Verify exactly one event was received and it matches what we expect
            if (createdEvents.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly 1 directory create event, but got {createdEvents.Count}. " +
                    $"Events: {string.Join(", ", createdEvents.Select(e => e.Name))}");
            }

            (string name, string fullPath) = createdEvents[0];
            if (name != "subdir" || fullPath != expectedSubDir)
            {
                throw new InvalidOperationException(
                    $"Expected directory 'subdir' at '{expectedSubDir}', but got '{name}' at '{fullPath}'");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
