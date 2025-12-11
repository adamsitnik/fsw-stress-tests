namespace FswStressTests;

/// <summary>
/// Scenario 6: Large file monitoring
/// Tests watching operations on larger files
/// </summary>
public class Scenario06_LargeFileMonitoring : IStressScenario
{
    public string Name => "Large File Monitoring";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_large_file_monitoring_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            List<(string Name, string FullPath)> createdEvents = new();
            TaskCompletionSource<bool> tcs = new();

            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                createdEvents.Add((e.Name!, e.FullPath));
                if (e.Name == "large.dat")
                {
                    tcs.TrySetResult(true);
                }
            };

            // Create a large file (10 MB)
            string expectedFilePath = Path.Combine(testDir, "large.dat");
            byte[] data = new byte[10 * 1024 * 1024]; // 10 MB of zeros (content doesn't matter for FSW testing)
            await File.WriteAllBytesAsync(expectedFilePath, data);

            // Wait for event
            Task timeoutTask = Task.Delay(10000);
            Task completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Large file creation event was not received within timeout");
            }

            // Verify exactly one event was received and it matches what we expect
            if (createdEvents.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly 1 create event, but got {createdEvents.Count}. " +
                    $"Events: {string.Join(", ", createdEvents.Select(e => e.Name))}");
            }

            (string name, string fullPath) = createdEvents[0];
            if (name != "large.dat")
            {
                throw new InvalidOperationException($"Expected file name 'large.dat', but got '{name}'");
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
