namespace FswStressTests;

/// <summary>
/// Scenario 11: Parallel watchers and file creation
/// Tests multiple FileSystemWatcher instances created in parallel, monitoring files created in parallel
/// </summary>
public class Scenario11_ParallelWatchersAndFiles : IStressScenario
{
    public string Name => "Parallel Watchers and File Creation";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_parallel_watchers_and_files_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            int watcher1Events = 0;
            int watcher2Events = 0;
            int watcher3Events = 0;
            int fileCount = 10;

            // Create watchers in parallel
            Task<FileSystemWatcher> createWatcher1 = Task.Run(() =>
            {
                FileSystemWatcher w = new(testDir)
                {
                    NotifyFilter = NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                w.Created += (s, e) => Interlocked.Increment(ref watcher1Events);
                return w;
            });

            Task<FileSystemWatcher> createWatcher2 = Task.Run(() =>
            {
                FileSystemWatcher w = new(testDir)
                {
                    NotifyFilter = NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                w.Created += (s, e) => Interlocked.Increment(ref watcher2Events);
                return w;
            });

            Task<FileSystemWatcher> createWatcher3 = Task.Run(() =>
            {
                FileSystemWatcher w = new(testDir)
                {
                    NotifyFilter = NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };
                w.Created += (s, e) => Interlocked.Increment(ref watcher3Events);
                return w;
            });

            FileSystemWatcher[] watchers = await Task.WhenAll(createWatcher1, createWatcher2, createWatcher3);

            using (watchers[0])
            using (watchers[1])
            using (watchers[2])
            {
                // Create files in parallel
                Task[] fileTasks = new Task[fileCount];
                for (int i = 0; i < fileCount; i++)
                {
                    int index = i; // Capture for lambda
                    fileTasks[i] = Task.Run(async () =>
                    {
                        string filePath = Path.Combine(testDir, $"parallel_{index}.txt");
                        await File.WriteAllTextAsync(filePath, $"Content {index}");
                    });
                }

                await Task.WhenAll(fileTasks);

                // Wait for events to be processed
                await Task.Delay(2000);

                // All watchers should have detected the same number of events
                if (watcher1Events != fileCount || watcher2Events != fileCount || watcher3Events != fileCount)
                {
                    throw new InvalidOperationException(
                        $"All watchers should have detected exactly {fileCount} events. " +
                        $"W1: {watcher1Events}, W2: {watcher2Events}, W3: {watcher3Events}");
                }
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
