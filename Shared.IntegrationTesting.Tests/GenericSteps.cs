﻿namespace Shared.IntegrationTesting.Tests;

using Ductus.FluentDocker;
using Ductus.FluentDocker.Common;
using Logger;
using NLog;
using TechTalk.SpecFlow;

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
        NlogLogger logger = new NlogLogger();
        logger.Initialise(LogManager.GetLogger(scenarioName), scenarioName);
        LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);

        this.TestingContext.DockerHelper = new DockerHelper();
        this.TestingContext.DockerHelper.Logger = logger;
        this.TestingContext.DockerHelper.SqlServerContainer = Setup.DatabaseServerContainer;
        this.TestingContext.DockerHelper.SqlServerNetwork = Setup.DatabaseServerNetwork;
        this.TestingContext.DockerHelper.DockerCredentials = Setup.DockerCredentials;
        this.TestingContext.DockerHelper.SqlCredentials = Setup.SqlCredentials;
        this.TestingContext.DockerHelper.SqlServerContainerName = "sharedsqlserver";

        DockerServices services = DockerServices.EventStore;
        //DockerServices services = DockerServices.EventStore | DockerServices.MessagingService | DockerServices.SecurityService |
        //                          DockerServices.CallbackHandler | DockerServices.EstateManagement | DockerServices.FileProcessor |
        //                          DockerServices.TestHost | DockerServices.TransactionProcessor |
        //                          DockerServices.TransactionProcessorAcl;

        this.TestingContext.Logger = logger;
        this.TestingContext.Logger.LogInformation("About to Start Containers for Scenario Run");
        this.TestingContext.DockerHelper.ScenarioName = scenarioName;
        await this.TestingContext.DockerHelper.StartContainersForScenarioRun(scenarioName, services).ConfigureAwait(false);
        this.TestingContext.Logger.LogInformation("Containers for Scenario Run Started");
    }

    [AfterScenario()]
    public async Task StopSystem()
    {
        this.TestingContext.Logger.LogInformation("About to Stop Containers for Scenario Run");
        await this.TestingContext.DockerHelper.StopContainersForScenarioRun().ConfigureAwait(false);
        this.TestingContext.Logger.LogInformation("Containers for Scenario Run Stopped");
    }
}