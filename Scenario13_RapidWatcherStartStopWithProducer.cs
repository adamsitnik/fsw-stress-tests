namespace FswStressTests;

/// <summary>
/// Scenario 13: Rapid watcher start/stop cycles with concurrent file operations
/// Tests the global watcher mechanism under extreme stress:
/// - (CPU-1) threads each starting and stopping 10 different watchers for the same directory
/// - No delays between operations to maximize stress on the global watcher
/// - One producer thread continuously creating file changes
/// - Verifies no exceptions are thrown by FileSystemWatcher
/// </summary>
public class Scenario13_RapidWatcherStartStopWithProducer : IStressScenario
{
    public string Name => "Rapid Watcher Start/Stop with Producer Thread";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_rapid_start_stop_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            int threadCount = Math.Max(1, Environment.ProcessorCount - 1);
            int watchersPerThread = 10;
            int operationDurationSeconds = 10;

            System.Collections.Concurrent.ConcurrentBag<Exception> exceptions = new();
            CancellationTokenSource cts = new();

            // Producer thread: continuously creates files
            Task producerTask = Task.Run(async () =>
            {
                int fileCounter = 0;
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        string filePath = Path.Combine(testDir, $"producer_file_{fileCounter++}.txt");
                        await File.WriteAllTextAsync(filePath, $"Content {fileCounter}");
                        
                        // Very brief delay to prevent overwhelming the filesystem
                        await Task.Delay(10, cts.Token);
                        
                        // Delete every 10th file to vary operations
                        if (fileCounter % 10 == 0)
                        {
                            try
                            {
                                File.Delete(filePath);
                            }
                            catch (FileNotFoundException)
                            {
                                // Expected race condition, ignore
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelled
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Consumer threads: rapidly start and stop watchers
            List<Task> consumerTasks = new();
            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                Task consumerTask = Task.Run(() =>
                {
                    try
                    {
                        DateTime endTime = DateTime.UtcNow.AddSeconds(operationDurationSeconds);
                        
                        while (DateTime.UtcNow < endTime)
                        {
                            // Create and dispose 10 watchers in rapid succession
                            for (int j = 0; j < watchersPerThread; j++)
                            {
                                FileSystemWatcher watcher = new(testDir)
                                {
                                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                                    IncludeSubdirectories = false,
                                    EnableRaisingEvents = true
                                };

                                // Set up event handlers to catch any exceptions
                                watcher.Created += (s, e) => { /* Event received */ };
                                watcher.Changed += (s, e) => { /* Event received */ };
                                watcher.Deleted += (s, e) => { /* Event received */ };
                                watcher.Error += (s, e) => exceptions.Add(e.GetException());

                                // Immediately dispose without delay
                                watcher.Dispose();
                            }
                            
                            // No delay between iterations to maximize stress
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
                consumerTasks.Add(consumerTask);
            }

            // Wait for all consumer threads to complete
            await Task.WhenAll(consumerTasks);

            // Signal producer to stop
            cts.Cancel();
            try
            {
                await producerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Check for exceptions
            if (exceptions.Count > 0)
            {
                Exception firstException = exceptions.First();
                string allExceptions = string.Join("; ", exceptions.Select(e => $"{e.GetType().Name}: {e.Message}"));
                throw new InvalidOperationException(
                    $"Detected {exceptions.Count} exception(s) during rapid watcher start/stop cycles. " +
                    $"First: {firstException.GetType().Name}: {firstException.Message}. " +
                    $"All: {allExceptions}",
                    firstException);
            }
        }
        finally
        {
            try
            {
                Directory.Delete(testDir, true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
