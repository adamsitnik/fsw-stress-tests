# FileSystemWatcher Stress Tests

Stress tests for .NET 10's `FileSystemWatcher` class.

## Running

```bash
dotnet run -c Release
```

## Exit Codes

- `0` - All tests passed
- `1` - One or more tests failed

## Adding New Tests

Create a class implementing `IStressScenario` and add it to the scenarios list in `Program.cs`:

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
