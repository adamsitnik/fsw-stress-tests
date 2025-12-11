namespace FswStressTests;

/// <summary>
/// Scenario 8: Rename operations
/// Tests watching for file and directory rename events
/// </summary>
public class Scenario08_RenameOperations : IStressScenario
{
    public string Name => "Rename Operations";

    public async Task RunAsync()
    {
        var testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var renamedEvents = new List<string>();
            var tcs = new TaskCompletionSource<bool>();

            using var watcher = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };

            watcher.Renamed += (sender, e) =>
            {
                renamedEvents.Add($"{e.OldName} -> {e.Name}");
                if (renamedEvents.Count >= 2)
                {
                    tcs.TrySetResult(true);
                }
            };

            // Create and rename a file
            var oldFilePath = Path.Combine(testDir, "old.txt");
            var newFilePath = Path.Combine(testDir, "new.txt");
            await File.WriteAllTextAsync(oldFilePath, "Content");
            await Task.Delay(100);
            File.Move(oldFilePath, newFilePath);
            await Task.Delay(100);

            // Create and rename a directory
            var oldDirPath = Path.Combine(testDir, "olddir");
            var newDirPath = Path.Combine(testDir, "newdir");
            Directory.CreateDirectory(oldDirPath);
            await Task.Delay(100);
            Directory.Move(oldDirPath, newDirPath);
            await Task.Delay(100);

            // Wait for events
            var timeoutTask = Task.Delay(5000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException(
                    $"Expected 2 rename events but got {renamedEvents.Count}. " +
                    $"Events: {string.Join(", ", renamedEvents)}");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
