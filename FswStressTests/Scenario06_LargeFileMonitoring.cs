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
        var testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var tcs = new TaskCompletionSource<bool>();

            using var watcher = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                if (e.Name == "large.dat")
                {
                    tcs.TrySetResult(true);
                }
            };

            // Create a large file (10 MB)
            var filePath = Path.Combine(testDir, "large.dat");
            var data = new byte[10 * 1024 * 1024]; // 10 MB
            new Random().NextBytes(data);
            await File.WriteAllBytesAsync(filePath, data);

            // Wait for event
            var timeoutTask = Task.Delay(10000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException("Large file creation event was not received within timeout");
            }

            // Verify file size
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length != data.Length)
            {
                throw new InvalidOperationException(
                    $"File size mismatch: expected {data.Length}, got {fileInfo.Length}");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
