namespace FswStressTests;

/// <summary>
/// Scenario 2: Multiple file operations (create, modify, delete)
/// Tests watching for different types of file operations
/// </summary>
public class Scenario02_MultipleFileOperations : IStressScenario
{
    public string Name => "Multiple File Operations";

    public async Task RunAsync()
    {
        var testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var events = new List<string>();
            var tcs = new TaskCompletionSource<bool>();

            using var watcher = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) => events.Add($"Created:{e.Name}");
            watcher.Changed += (sender, e) => events.Add($"Changed:{e.Name}");
            watcher.Deleted += (sender, e) => events.Add($"Deleted:{e.Name}");

            var filePath = Path.Combine(testDir, "test.txt");

            // Create
            await File.WriteAllTextAsync(filePath, "Initial content");
            await Task.Delay(100);

            // Modify
            await File.WriteAllTextAsync(filePath, "Modified content");
            await Task.Delay(100);

            // Delete
            File.Delete(filePath);
            await Task.Delay(100);

            // Verify we got at least some events
            if (events.Count == 0)
            {
                throw new InvalidOperationException("No file system events were detected");
            }

            // Check that we have created and deleted events
            var hasCreated = events.Any(e => e.StartsWith("Created:"));
            var hasDeleted = events.Any(e => e.StartsWith("Deleted:"));

            if (!hasCreated || !hasDeleted)
            {
                throw new InvalidOperationException(
                    $"Expected Created and Deleted events. Got: {string.Join(", ", events)}");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
