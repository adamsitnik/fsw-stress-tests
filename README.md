# FileSystemWatcher Stress Tests

A comprehensive command-line application that performs stress tests on .NET 10's `FileSystemWatcher` class.

## Features

- **10 Progressive Stress Test Scenarios**: Tests range from simple file creation monitoring to complex nested directory operations
- **Execution Time Tracking**: Each scenario reports its execution time
- **Resilient Testing**: Continues running all scenarios even when one fails
- **Aggregate Failure Reporting**: All failures are reported at the end with detailed error information
- **Separate Scenario Classes**: Each test is isolated in its own class for easy maintenance and extension

## Scenarios

The application includes 12 stress test scenarios, ordered by increasing complexity:

1. **Basic File Creation** - Simple file creation monitoring
2. **Multiple File Operations** - Create, modify, and delete operations
3. **Directory Creation** - Directory creation monitoring
4. **Recursive Directory Monitoring** - Watching subdirectories recursively
5. **High-Frequency File Operations** - Rapid creation of many files
6. **Large File Monitoring** - Monitoring large file operations (10MB)
7. **Concurrent Watchers** - Multiple watchers on the same directory
8. **Rename Operations** - File and directory rename events
9. **Rapid Create/Delete Cycles** - Repeated creation and deletion
10. **Nested Directory Operations** - Complex nested structures with multiple operations
11. **Parallel Watchers and File Creation** - Multiple watchers created in parallel, monitoring files created in parallel
12. **Parallel Create/Delete Cycles** - Rapid creation and deletion of multiple files in parallel

## Requirements

- .NET 10.0 SDK or later

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

Or with release configuration for better performance:

```bash
dotnet run -c Release
```

## Output

The application provides:
- Real-time progress for each scenario
- Pass/fail status with execution time
- Summary statistics at the end
- Detailed failure information if any tests fail

Example output:
```
=== FileSystemWatcher Stress Tests ===
Started at: 2025-12-11 10:34:02

[1/12] Running: Basic File Creation
    ✓ PASSED in 0.01s

[2/12] Running: Multiple File Operations
    ✓ PASSED in 0.30s

...

=== Summary ===
Total scenarios: 12
Passed: 12
Failed: 0
Total execution time: 12.50s

All tests passed!
```

## Exit Codes

- `0` - All tests passed
- `1` - One or more tests failed

## Extending

To add a new stress test scenario:

1. Create a new class implementing `IStressScenario`
2. Implement the `Name` property and `RunAsync()` method
3. Add an instance to the scenarios list in `Program.cs`

Example:
```csharp
public class ScenarioXX_YourTest : IStressScenario
{
    public string Name => "Your Test Description";

    public async Task RunAsync()
    {
        // Your test implementation
    }
}
```
