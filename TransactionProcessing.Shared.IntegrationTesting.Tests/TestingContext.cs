using TransactionProcessing.Shared.IntegrationTesting.TestContainers;

namespace TransactionProcessing.Shared.IntegrationTesting.Tests;

using Logger;

public class TestingContext
{
    public ILogger Logger { get; set; }
    public DockerHelper DockerHelper { get; set; }
    public TestingContext()
    {
    }
}