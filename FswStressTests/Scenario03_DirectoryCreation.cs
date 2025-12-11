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
        var testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var tcs = new TaskCompletionSource<bool>();

            using var watcher = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                if (e.Name == "subdir")
                {
                    tcs.TrySetResult(true);
                }
            };

            // Create subdirectory
            var subDir = Path.Combine(testDir, "subdir");
            Directory.CreateDirectory(subDir);

            // Wait for event
            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Directory creation event was not received within timeout");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
