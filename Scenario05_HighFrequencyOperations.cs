namespace FswStressTests;

/// <summary>
/// Scenario 5: High-frequency file operations
/// Tests handling many file operations in rapid succession
/// </summary>
public class Scenario05_HighFrequencyOperations : IStressScenario
{
    public string Name => "High-Frequency File Operations";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_high_frequency_operations_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            List<(string Name, string FullPath)> createdEvents = new();
            int fileCount = 50;

            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            watcher.Created += (sender, e) =>
            {
                lock (createdEvents)
                {
                    createdEvents.Add((e.Name!, e.FullPath));
                }
            };

            // Create many files rapidly and track expected paths
            HashSet<string> expectedFiles = new();
            for (int i = 0; i < fileCount; i++)
            {
                string fileName = $"file_{i}.txt";
                string filePath = Path.Combine(testDir, fileName);
                expectedFiles.Add(filePath);
                await File.WriteAllTextAsync(filePath, $"Content {i}");
            }

            // Wait a bit for events to be processed
            await Task.Delay(2000);

            // We should have received at least some events
            // (not necessarily all due to event coalescing in some systems)
            if (createdEvents.Count == 0)
            {
                throw new InvalidOperationException("No file creation events were detected");
            }

            if (createdEvents.Count < fileCount / 2)
            {
                throw new InvalidOperationException(
                    $"Too few events detected: {createdEvents.Count} out of {fileCount} files created");
            }

            // Verify all reported events are for files we created
            foreach ((string name, string fullPath) in createdEvents)
            {
                if (!expectedFiles.Contains(fullPath))
                {
                    throw new InvalidOperationException(
                        $"Unexpected file event: {name} at {fullPath}. " +
                        $"This file was not created by the test.");
                }

                if (!name.StartsWith("file_") || !name.EndsWith(".txt"))
                {
                    throw new InvalidOperationException(
                        $"Unexpected file name pattern: {name}. Expected file_N.txt format.");
                }
            }

            // Verify paths are correct
            HashSet<string> reportedPaths = new(createdEvents.Select(e => e.FullPath));
            foreach (string path in reportedPaths)
            {
                if (!path.StartsWith(testDir))
                {
                    throw new InvalidOperationException(
                        $"File path '{path}' is not in expected directory '{testDir}'");
                }
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
