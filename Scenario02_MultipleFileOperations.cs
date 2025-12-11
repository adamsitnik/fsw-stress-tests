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
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_multiple_file_operations_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            List<(string EventType, string Name, string FullPath)> events = new();

            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) => events.Add(("Created", e.Name!, e.FullPath));
            watcher.Changed += (sender, e) => events.Add(("Changed", e.Name!, e.FullPath));
            watcher.Deleted += (sender, e) => events.Add(("Deleted", e.Name!, e.FullPath));

            string filePath = Path.Combine(testDir, "test.txt");

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

            // Check that we have created, changed, and deleted events for test.txt only
            List<(string EventType, string Name, string FullPath)> createdEvents = events.Where(e => e.EventType == "Created").ToList();
            List<(string EventType, string Name, string FullPath)> changedEvents = events.Where(e => e.EventType == "Changed").ToList();
            List<(string EventType, string Name, string FullPath)> deletedEvents = events.Where(e => e.EventType == "Deleted").ToList();

            if (createdEvents.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Expected at least one Created event. Got events: {string.Join(", ", events.Select(e => $"{e.EventType}:{e.Name}"))}");
            }

            if (deletedEvents.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Expected at least one Deleted event. Got events: {string.Join(", ", events.Select(e => $"{e.EventType}:{e.Name}"))}");
            }

            // Verify all events are for test.txt only
            foreach ((string eventType, string name, string fullPath) in events)
            {
                if (name != "test.txt")
                {
                    throw new InvalidOperationException(
                        $"Unexpected event for file '{name}'. Expected only 'test.txt'. " +
                        $"All events: {string.Join(", ", events.Select(e => $"{e.EventType}:{e.Name}"))}");
                }

                if (fullPath != filePath)
                {
                    throw new InvalidOperationException(
                        $"Unexpected full path '{fullPath}'. Expected '{filePath}'");
                }
            }

            // Verify changed event exists (may be multiple due to file system behavior)
            if (changedEvents.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Expected at least one Changed event. Got events: {string.Join(", ", events.Select(e => $"{e.EventType}:{e.Name}"))}");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
