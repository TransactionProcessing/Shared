using System;
using System.Threading.Tasks;
using ClientProxyBase;
using Shared.IntegrationTesting.TestContainers;

namespace Shared.IntegrationTesting.Tests;

using Logger;
using Microsoft.AspNetCore.Http;
using NLog;
using Reqnroll;

public class TestDockerHelper : DockerHelper{
    public override async Task CreateSubscriptions(){
        // Nothing here
    }
}

[Binding]
[Scope(Tag = "base")]
public class GenericSteps
{
    private readonly ScenarioContext ScenarioContext;

    private readonly TestingContext TestingContext;
    
    public GenericSteps(ScenarioContext scenarioContext, TestingContext testingContext)
    {
        this.ScenarioContext = scenarioContext;
        this.TestingContext = testingContext;
    }

    [BeforeScenario()]
    public async Task StartSystem() {
        // Initialise a logger
        String scenarioName = this.ScenarioContext.ScenarioInfo.Title.Replace(" ", "");
        NlogLogger logger = new();
        logger.Initialise(LogManager.GetLogger(scenarioName), scenarioName);
        LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);

        this.TestingContext.DockerHelper = new TestDockerHelper();
        this.TestingContext.DockerHelper.Logger = logger;

        DockerServices services = DockerServices.SqlServer | DockerServices.EventStore | DockerServices.MessagingService | DockerServices.SecurityService |
                                  DockerServices.CallbackHandler | DockerServices.FileProcessor |
                                  DockerServices.TestHost | DockerServices.TransactionProcessor |
                                  DockerServices.TransactionProcessorAcl | DockerServices.ConfigurationHost
                                  | DockerServices.EstateManagementUI;

        this.TestingContext.Logger = logger;
        this.TestingContext.Logger.LogInformation("About to Start Containers for Scenario Run");
        this.TestingContext.DockerHelper.ScenarioName = scenarioName;
        await this.TestingContext.DockerHelper.StartContainersForScenarioRun(scenarioName, services).ConfigureAwait(false);
        this.TestingContext.Logger.LogInformation("Containers for Scenario Run Started");
    }

    

    [AfterScenario()]
    public async Task StopSystem(){
        DockerServices sharedDockerServices = DockerServices.None;

        this.TestingContext.Logger.LogInformation("About to Stop Containers for Scenario Run");
        await this.TestingContext.DockerHelper.StopContainersForScenarioRun(sharedDockerServices).ConfigureAwait(false);
        this.TestingContext.Logger.LogInformation("Containers for Scenario Run Stopped");
    }
}