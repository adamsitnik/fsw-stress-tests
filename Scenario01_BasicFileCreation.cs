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
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_basic_file_creation_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            List<(string Name, string FullPath)> createdEvents = new();
            TaskCompletionSource<bool> tcs = new();
            
            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                createdEvents.Add((e.Name!, e.FullPath));
                if (e.Name == "test.txt")
                {
                    tcs.TrySetResult(true);
                }
            };

            // Create a test file
            string expectedFilePath = Path.Combine(testDir, "test.txt");
            await File.WriteAllTextAsync(expectedFilePath, "Hello World");

            // Wait for event with timeout
            Task timeoutTask = Task.Delay(5000);
            Task completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("File creation event was not received within timeout");
            }

            // Verify exactly one event was received and it matches what we expect
            if (createdEvents.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly 1 create event, but got {createdEvents.Count}. " +
                    $"Events: {string.Join(", ", createdEvents.Select(e => e.Name))}");
            }

            (string name, string fullPath) = createdEvents[0];
            if (name != "test.txt")
            {
                throw new InvalidOperationException($"Expected file name 'test.txt', but got '{name}'");
            }

            if (fullPath != expectedFilePath)
            {
                throw new InvalidOperationException(
                    $"Expected full path '{expectedFilePath}', but got '{fullPath}'");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
