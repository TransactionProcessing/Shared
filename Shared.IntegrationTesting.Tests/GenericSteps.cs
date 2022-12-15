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

    public GenericSteps(ScenarioContext scenarioContext,
                        TestingContext testingContext)
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
        this.TestingContext.Logger = logger;
        this.TestingContext.Logger.LogInformation("About to Start Containers for Scenario Run");
        await this.TestingContext.DockerHelper.StartContainersForScenarioRun(scenarioName).ConfigureAwait(false);
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