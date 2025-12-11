namespace FswStressTests;

/// <summary>
/// Scenario 1: Basic file creation monitoring
/// Tests the simplest case - watching for a single file creation
/// </summary>
public class Scenario01_BasicFileCreation : IStressScenario
{
    public string Name => "Basic File Creation";

    public async Task RunAsync()
    {
        var testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var tcs = new TaskCompletionSource<bool>();
            
            using var watcher = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                if (e.Name == "test.txt")
                {
                    tcs.TrySetResult(true);
                }
            };

            // Create a test file
            await File.WriteAllTextAsync(Path.Combine(testDir, "test.txt"), "Hello World");

            // Wait for event with timeout
            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("File creation event was not received within timeout");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
