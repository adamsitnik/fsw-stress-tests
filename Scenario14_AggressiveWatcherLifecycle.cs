using System.Collections.Concurrent;

namespace FswStressTests;

/// <summary>
/// Scenario 14: Aggressive watcher lifecycle stress test
/// Tests edge cases in watcher lifecycle management:
/// - Multiple threads rapidly creating and destroying watchers on the same directory
/// - File operations happening during watcher creation and disposal
/// - Tests disposal during active event processing
/// - Verifies global watcher handles rapid watch additions/removals
/// </summary>
public class Scenario14_AggressiveWatcherLifecycle : IStressScenario
{
    public string Name => "Aggressive Watcher Lifecycle Management";

    public async Task RunAsync()
    {
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_aggressive_lifecycle_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            int threadCount = Environment.ProcessorCount;
            int iterationsPerThread = 50;
            ConcurrentBag<Exception> exceptions = new();
            ConcurrentBag<string> receivedEvents = new();
            
            // Create initial files to trigger events
            for (int i = 0; i < 5; i++)
            {
                string filePath = Path.Combine(testDir, $"initial_{i}.txt");
                await File.WriteAllTextAsync(filePath, "Initial content");
            }

            // Background task: continuous file modifications
            CancellationTokenSource cts = new();
            ManualResetEventSlim producerGate = new(false);
            
            Task backgroundTask = Task.Run(async () =>
            {
                // Wait for first consumer to be ready
                producerGate.Wait();
                
                int counter = 0;
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        string filePath = Path.Combine(testDir, $"background_{counter % 10}.txt");
                        await File.WriteAllTextAsync(filePath, $"Background content {counter++}");
                        await Task.Delay(20, cts.Token);
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

            // Multiple threads aggressively creating/disposing watchers
            int firstConsumer = 0;
            
            Parallel.For(0, threadCount, i =>
            {
                try
                {
                    // First consumer signals producer to start
                    if (Interlocked.CompareExchange(ref firstConsumer, 1, 0) == 0)
                    {
                        producerGate.Set();
                    }
                    
                    for (int iteration = 0; iteration < iterationsPerThread; iteration++)
                    {
                        // Create watcher
                        FileSystemWatcher watcher = new(testDir)
                        {
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                            IncludeSubdirectories = false
                        };

                        // Set up comprehensive event handlers
                        watcher.Created += (s, e) => receivedEvents.Add($"T{i}-I{iteration}: Created {e.Name}");
                        watcher.Changed += (s, e) => receivedEvents.Add($"T{i}-I{iteration}: Changed {e.Name}");
                        watcher.Deleted += (s, e) => receivedEvents.Add($"T{i}-I{iteration}: Deleted {e.Name}");
                        watcher.Renamed += (s, e) => receivedEvents.Add($"T{i}-I{iteration}: Renamed {e.OldName} to {e.Name}");
                        watcher.Error += (s, e) => exceptions.Add(e.GetException());

                        // Enable and immediately create a file to potentially trigger events
                        watcher.EnableRaisingEvents = true;
                        
                        string testFile = Path.Combine(testDir, $"test_t{i}_i{iteration}.txt");
                        File.WriteAllText(testFile, $"Thread {i} Iteration {iteration}");
                        
                        // Modify the file while watcher is active
                        File.AppendAllText(testFile, " - Modified");
                        
                        // Brief moment for events to potentially be queued
                        // This is intentional to test the edge case of disposal while events are in flight
                        Thread.Sleep(5);
                        
                        // Dispose while events might still be in flight
                        watcher.Dispose();
                        
                        // Clean up test file
                        try
                        {
                            File.Delete(testFile);
                        }
                        catch (FileNotFoundException)
                        {
                            // Race condition, acceptable
                        }

                        // No delay between iterations to stress lifecycle transitions
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Stop background task
            cts.Cancel();
            try
            {
                await backgroundTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
            
            producerGate.Dispose();

            // Wait a bit for any pending events
            await Task.Delay(500);

            // Verify no exceptions occurred
            if (exceptions.Count > 0)
            {
                Exception firstException = exceptions.First();
                string allExceptions = string.Join("; ", exceptions.Take(10).Select(e => $"{e.GetType().Name}: {e.Message}"));
                throw new InvalidOperationException(
                    $"Detected {exceptions.Count} exception(s) during aggressive watcher lifecycle test. " +
                    $"First: {firstException.GetType().Name}: {firstException.Message}. " +
                    $"Sample exceptions: {allExceptions}",
                    firstException);
            }

            // We should have received at least some events (no strict count due to disposal timing)
            if (receivedEvents.Count == 0)
            {
                throw new InvalidOperationException(
                    "No events were received during aggressive lifecycle test. " +
                    "This might indicate a problem with the global watcher event delivery.");
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
