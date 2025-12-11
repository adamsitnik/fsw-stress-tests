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
        string testDir = Path.Combine(Path.GetTempPath(), $"fsw_test_rename_operations_{Guid.NewGuid()}");
        Directory.CreateDirectory(testDir);

        try
        {
            List<(string OldName, string NewName, string OldFullPath, string NewFullPath)> renamedEvents = new();
            TaskCompletionSource<bool> tcs = new();

            using FileSystemWatcher watcher = new(testDir)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };

            watcher.Renamed += (sender, e) =>
            {
                RenamedEventArgs args = (RenamedEventArgs)e;
                renamedEvents.Add((args.OldName!, args.Name!, args.OldFullPath, args.FullPath));
                if (renamedEvents.Count >= 2)
                {
                    tcs.TrySetResult(true);
                }
            };

            // Create and rename a file
            string oldFilePath = Path.Combine(testDir, "old.txt");
            string newFilePath = Path.Combine(testDir, "new.txt");
            await File.WriteAllTextAsync(oldFilePath, "Content");
            await Task.Delay(100);
            File.Move(oldFilePath, newFilePath);
            await Task.Delay(100);

            // Create and rename a directory
            string oldDirPath = Path.Combine(testDir, "olddir");
            string newDirPath = Path.Combine(testDir, "newdir");
            Directory.CreateDirectory(oldDirPath);
            await Task.Delay(100);
            Directory.Move(oldDirPath, newDirPath);
            await Task.Delay(100);

            // Wait for events
            Task timeoutTask = Task.Delay(5000);
            Task completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                throw new TimeoutException(
                    $"Expected 2 rename events but got {renamedEvents.Count}. " +
                    $"Events: {string.Join(", ", renamedEvents.Select(e => $"{e.OldName} -> {e.NewName}"))}");
            }

            // Verify exactly 2 rename events
            if (renamedEvents.Count != 2)
            {
                throw new InvalidOperationException(
                    $"Expected exactly 2 rename events, but got {renamedEvents.Count}. " +
                    $"Events: {string.Join(", ", renamedEvents.Select(e => $"{e.OldName} -> {e.NewName}"))}");
            }

            // Verify file rename
            (string oldName, string newName, string oldFullPath, string newFullPath) fileRename = 
                renamedEvents.First(e => e.OldName == "old.txt");
            
            if (fileRename.newName != "new.txt")
            {
                throw new InvalidOperationException(
                    $"Expected file rename from 'old.txt' to 'new.txt', but got '{fileRename.oldName}' to '{fileRename.newName}'");
            }

            if (fileRename.oldFullPath != oldFilePath || fileRename.newFullPath != newFilePath)
            {
                throw new InvalidOperationException(
                    $"File rename paths don't match. Expected: '{oldFilePath}' -> '{newFilePath}', " +
                    $"but got: '{fileRename.oldFullPath}' -> '{fileRename.newFullPath}'");
            }

            // Verify directory rename
            (string oldName, string newName, string oldFullPath, string newFullPath) dirRename = 
                renamedEvents.First(e => e.OldName == "olddir");
            
            if (dirRename.newName != "newdir")
            {
                throw new InvalidOperationException(
                    $"Expected directory rename from 'olddir' to 'newdir', but got '{dirRename.oldName}' to '{dirRename.newName}'");
            }

            if (dirRename.oldFullPath != oldDirPath || dirRename.newFullPath != newDirPath)
            {
                throw new InvalidOperationException(
                    $"Directory rename paths don't match. Expected: '{oldDirPath}' -> '{newDirPath}', " +
                    $"but got: '{dirRename.oldFullPath}' -> '{dirRename.newFullPath}'");
            }
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }
}
