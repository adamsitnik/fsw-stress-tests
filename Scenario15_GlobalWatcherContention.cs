namespace FswStressTests;

/// <summary>
/// Scenario 15: Global watcher contention test
/// Specifically designed to stress the global watcher implementation:
/// - Maximum concurrency with all CPU cores
/// - Simultaneous watch additions and removals on the same directory
/// - Continuous high-frequency file operations generating events
/// - Multiple watchers with different filters on the same directory
/// - Verifies event delivery correctness under maximum contention
/// </summary>
public class Scenario15_GlobalWatcherContention : IStressScenario
{
    public string Name => "Global Watcher Contention Test";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_global_contention_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            int maxThreads = Environment.ProcessorCount * 2; // Oversubscribe for maximum contention
            int operationDurationSeconds = 8;
            
            System.Collections.Concurrent.ConcurrentBag<Exception> exceptions = new();
            System.Collections.Concurrent.ConcurrentDictionary<int, int> eventCountsByWatcher = new();
            CancellationTokenSource cts = new();
            
            int watcherIdCounter = 0;

            // High-frequency file producer
            Task producerTask = Task.Run(async () =>
            {
                int fileCounter = 0;
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        // Rapid file operations
                        string filePath = Path.Combine(testDir, $"file_{fileCounter % 20}.txt");
                        await File.WriteAllTextAsync(filePath, $"Content {fileCounter}");
                        fileCounter++;
                        
                        // No delay - maximum stress
                        if (fileCounter % 5 == 0)
                        {
                            // Mix in some deletions
                            string deleteFile = Path.Combine(testDir, $"file_{(fileCounter - 3) % 20}.txt");
                            try
                            {
                                File.Delete(deleteFile);
                            }
                            catch (FileNotFoundException)
                            {
                                // Expected race
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Multiple threads continuously creating and destroying watchers
            List<Task> watcherThreads = new();
            for (int t = 0; t < maxThreads; t++)
            {
                int threadIndex = t;
                Task watcherThread = Task.Run(() =>
                {
                    try
                    {
                        DateTime endTime = DateTime.UtcNow.AddSeconds(operationDurationSeconds);
                        
                        while (DateTime.UtcNow < endTime)
                        {
                            int watcherId = Interlocked.Increment(ref watcherIdCounter);
                            
                            // Create watcher with varying configurations to stress the global watcher
                            NotifyFilters filter = (watcherId % 3) switch
                            {
                                0 => NotifyFilters.FileName,
                                1 => NotifyFilters.LastWrite,
                                _ => NotifyFilters.FileName | NotifyFilters.LastWrite
                            };

                            FileSystemWatcher watcher = new(testDir)
                            {
                                NotifyFilter = filter,
                                IncludeSubdirectories = false,
                                EnableRaisingEvents = true
                            };

                            // Track events per watcher
                            int localWatcherId = watcherId;
                            eventCountsByWatcher[localWatcherId] = 0;

                            watcher.Created += (s, e) => 
                            {
                                eventCountsByWatcher.AddOrUpdate(localWatcherId, 1, (k, v) => v + 1);
                            };
                            watcher.Changed += (s, e) => 
                            {
                                eventCountsByWatcher.AddOrUpdate(localWatcherId, 1, (k, v) => v + 1);
                            };
                            watcher.Deleted += (s, e) => 
                            {
                                eventCountsByWatcher.AddOrUpdate(localWatcherId, 1, (k, v) => v + 1);
                            };
                            watcher.Error += (s, e) => exceptions.Add(e.GetException());

                            // Let it run briefly to collect some events before disposal
                            // This tests that the global watcher correctly delivers events to short-lived watchers
                            Thread.Sleep(50);
                            
                            // Dispose - this removes the watch from the global watcher
                            watcher.Dispose();
                            
                            // Immediately continue to next iteration - maximum churn
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
                watcherThreads.Add(watcherThread);
            }

            // Wait for all watcher threads
            await Task.WhenAll(watcherThreads);

            // Stop producer
            cts.Cancel();
            try
            {
                await producerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Brief delay for final event processing
            await Task.Delay(500);

            // Analyze results
            if (exceptions.Count > 0)
            {
                Exception firstException = exceptions.First();
                string sampleExceptions = string.Join("; ", exceptions.Take(5).Select(e => $"{e.GetType().Name}: {e.Message}"));
                throw new InvalidOperationException(
                    $"Detected {exceptions.Count} exception(s) during global watcher contention test. " +
                    $"First: {firstException.GetType().Name}: {firstException.Message}. " +
                    $"Sample: {sampleExceptions}",
                    firstException);
            }

            // Verify we created many watchers
            int totalWatchers = eventCountsByWatcher.Count;
            if (totalWatchers < maxThreads)
            {
                throw new InvalidOperationException(
                    $"Expected to create at least {maxThreads} watchers but only created {totalWatchers}. " +
                    "This might indicate a problem with watcher creation under contention.");
            }

            // Count how many watchers received events
            int watchersWithEvents = eventCountsByWatcher.Count(kvp => kvp.Value > 0);
            
            // We expect most watchers to receive at least some events given the high-frequency producer
            // Allow for some to miss events due to timing, but not all
            if (watchersWithEvents == 0)
            {
                throw new InvalidOperationException(
                    "No watchers received any events during high-contention test. " +
                    "This indicates a critical issue with the global watcher event delivery mechanism.");
            }

            // Log statistics for debugging
            int totalEvents = eventCountsByWatcher.Values.Sum();
            Console.WriteLine($"    Created {totalWatchers} watchers, {watchersWithEvents} received events, total {totalEvents} events");
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
