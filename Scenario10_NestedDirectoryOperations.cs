namespace FswStressTests;

/// <summary>
/// Scenario 10: Nested directory operations
/// Tests complex nested directory structures with multiple operations
/// </summary>
public class Scenario10_NestedDirectoryOperations : IStressScenario
{
    public string Name => "Nested Directory Operations";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_nested_directory_operations_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            int eventCount = 0;
            int minExpectedEvents = 10;

            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            watcher.Created += (s, e) => Interlocked.Increment(ref eventCount);
            watcher.Deleted += (s, e) => Interlocked.Increment(ref eventCount);

            // Create nested structure with files
            List<string> dirs = new();
            for (int i = 0; i < 3; i++)
            {
                string dir = Path.Combine(testDir, $"dir{i}");
                Directory.CreateDirectory(dir);
                dirs.Add(dir);

                for (int j = 0; j < 2; j++)
                {
                    string subDir = Path.Combine(dir, $"subdir{j}");
                    Directory.CreateDirectory(subDir);
                    dirs.Add(subDir);

                    // Create files in subdirectories
                    for (int k = 0; k < 2; k++)
                    {
                        string filePath = Path.Combine(subDir, $"file{k}.txt");
                        await File.WriteAllTextAsync(filePath, $"Content {i}-{j}-{k}");
                        await Task.Delay(20);
                    }
                }
            }

            // Wait for events
            await Task.Delay(1000);

            // Delete some files
            foreach (string dir in dirs.Where(d => d.Contains("subdir0")))
            {
                string[] files = Directory.GetFiles(dir);
                foreach (string file in files)
                {
                    File.Delete(file);
                    await Task.Delay(20);
                }
            }

            // Wait for delete events
            await Task.Delay(1000);

            if (eventCount < minExpectedEvents)
            {
                throw new InvalidOperationException(
                    $"Too few events detected: {eventCount}, expected at least {minExpectedEvents}");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
