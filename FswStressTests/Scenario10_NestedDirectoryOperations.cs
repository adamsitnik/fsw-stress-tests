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
        var testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            var eventCount = 0;
            var minExpectedEvents = 10;

            using var watcher = new FileSystemWatcher(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            watcher.Created += (s, e) => Interlocked.Increment(ref eventCount);
            watcher.Deleted += (s, e) => Interlocked.Increment(ref eventCount);

            // Create nested structure with files
            var dirs = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var dir = Path.Combine(testDir, $"dir{i}");
                Directory.CreateDirectory(dir);
                dirs.Add(dir);

                for (int j = 0; j < 2; j++)
                {
                    var subDir = Path.Combine(dir, $"subdir{j}");
                    Directory.CreateDirectory(subDir);
                    dirs.Add(subDir);

                    // Create files in subdirectories
                    for (int k = 0; k < 2; k++)
                    {
                        var filePath = Path.Combine(subDir, $"file{k}.txt");
                        await File.WriteAllTextAsync(filePath, $"Content {i}-{j}-{k}");
                        await Task.Delay(20);
                    }
                }
            }

            // Wait for events
            await Task.Delay(1000);

            // Delete some files
            foreach (var dir in dirs.Where(d => d.Contains("subdir0")))
            {
                var files = Directory.GetFiles(dir);
                foreach (var file in files)
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
