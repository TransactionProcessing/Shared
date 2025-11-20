using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Shared.IntegrationTesting.TestContainers;

namespace Shared.IntegrationTesting.Tests;

using NLog;
using Reqnroll;
using Shared.Logger;
using Shouldly;

[Binding]
public class Setup
{
    public static IContainer DatabaseServerContainer;
    public static INetwork DatabaseServerNetwork;
    public static (String usename, String password) SqlCredentials = ("sa", "thisisalongpassword123!");
    public static (String url, String username, String password) DockerCredentials = ("https://www.docker.com", "stuartferguson", "Sc0tland");

    [BeforeTestRun]
    protected static async Task GlobalSetup(){
        ShouldlyConfiguration.DefaultTaskTimeout = TimeSpan.FromMinutes(1);

        DockerHelper dockerHelper = new TestDockerHelper();
        dockerHelper.RequiredDockerServices = DockerServices.SqlServer;

        NlogLogger logger = new();
        logger.Initialise(LogManager.GetLogger("Reqnroll"), "Reqnroll");
        LogManager.AddHiddenAssembly(typeof(NlogLogger).Assembly);
        dockerHelper.Logger = logger;
        dockerHelper.SqlCredentials = Setup.SqlCredentials;
        dockerHelper.DockerCredentials = Setup.DockerCredentials;
        dockerHelper.SqlServerContainerName = "sharedsqlserver";

        String? isCi = Environment.GetEnvironmentVariable("IsCI");
        dockerHelper.Logger.LogInformation($"IsCI [{isCi}]");
        if (String.Compare(isCi, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0){
            // override teh SQL Server image
            dockerHelper.Logger.LogInformation("Sql Image overridden");
            dockerHelper.SetImageDetails(ContainerType.SqlServer, ("mssqlserver:2022-ltsc2022", false));
        }

        // Only one thread can execute this block at a time
        await SetupLock.WaitAsync();
        try
        {
            Setup.DatabaseServerNetwork = await dockerHelper.SetupTestNetwork("sharednetwork");
            if (dockerHelper.DockerPlatform == DockerEnginePlatform.Windows) {
                await DatabaseServerNetwork.CreateAsync();
            }
            
            dockerHelper.Logger.LogInformation("in start SetupSqlServerContainer");
            Setup.DatabaseServerContainer = await dockerHelper.SetupSqlServerContainer(Setup.DatabaseServerNetwork);
        }
        finally
        {
            SetupLock.Release();
        }
    }

    private static readonly SemaphoreSlim SetupLock = new SemaphoreSlim(1, 1);
}