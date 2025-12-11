namespace FswStressTests;

/// <summary>
/// Interface for FileSystemWatcher stress test scenarios
/// </summary>
public interface IStressScenario
{
    /// <summary>
    /// The name of the scenario
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Execute the stress test scenario
    /// </summary>
    Task RunAsync();
}
