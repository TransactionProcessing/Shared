using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.IntegrationTesting
{
    using System.Data;
    using System.IO;
    using System.Net;
    using System.Security.Policy;
    using System.Threading;
    using System.Threading.Tasks;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;
    using Logger;
    using Microsoft.Data.SqlClient;

    public abstract class DockerHelper
    {
        public abstract Task StartContainersForScenarioRun(String scenarioName);
        public abstract Task StopContainersForScenarioRun();

        public const Int32 EstateManagementDockerPort = 5000;
        public const Int32 SecurityServiceDockerPort = 5001;
        public const Int32 TransactionProcessorDockerPort = 5002;
        public const Int32 TransactionProcessorACLDockerPort = 5003;

        public const Int32 EventStoreTcpDockerPort = 1113;
        public const Int32 EventStoreHttpDockerPort = 2113;

        public static INetworkService SetupTestNetwork(String networkName=null, Boolean reuseIfExists=false)
        {
            networkName = String.IsNullOrEmpty(networkName) ? $"testnetwork{Guid.NewGuid()}" : networkName;
            
            // Build a network
            NetworkBuilder networkService = new Ductus.FluentDocker.Builders.Builder().UseNetwork(networkName);

            if (reuseIfExists)
            {
                networkService.ReuseIfExist();
            }

            return networkService.Build();
        }

        public static IContainerService SetupSecurityServiceContainer(String containerName, ILogger logger, String imageName, 
                                                                      INetworkService networkService,String hostFolder,
                                                                      (String URL, String UserName, String Password)? dockerCredentials)
        {
            logger.LogInformation("About to Start Security Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"ServiceOptions:PublicOrigin=http://{containerName}:{SecurityServiceDockerPort}");
            environmentVariables.Add($"ServiceOptions:IssuerUrl=http://{containerName}:{SecurityServiceDockerPort}");
            environmentVariables.Add("ASPNETCORE_ENVIRONMENT=IntegrationTest");
            environmentVariables.Add("urls=http://*:5001");

            ContainerBuilder securityServiceContainer = new Ductus.FluentDocker.Builders.Builder().UseContainer().WithName(containerName)
                                                                                                  .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName)
                                                                                                  .ExposePort(DockerHelper.SecurityServiceDockerPort)
                                                                                                  .UseNetwork(new List<INetworkService>
                                                                                                              {
                                                                                                                  networkService
                                                                                                              }.ToArray()).Mount(hostFolder,
                                                                                                                                 "/home/txnproc/trace",
                                                                                                                                 MountType.ReadWrite);

            if (dockerCredentials.HasValue)
            {
                securityServiceContainer.WithCredential(dockerCredentials.Value.URL, dockerCredentials.Value.UserName, dockerCredentials.Value.Password);
            }

            // Now build and return the container                
            IContainerService builtContainer = securityServiceContainer.Build().Start().WaitForPort("5001/tcp", 30000);
            Thread.Sleep(20000); // This hack is in till health checks implemented :|

            logger.LogInformation("Security Service Container Started");

            return builtContainer;
        }

        public static IContainerService SetupEventStoreContainer(String containerName, ILogger logger, String imageName,
                                                                  INetworkService networkService, String hostFolder)
        {
            logger.LogInformation("About to Start Event Store Container");
            
            IContainerService eventStoreContainer = new Ductus.FluentDocker.Builders.Builder()
                                                    .UseContainer()
                                                    .UseImage(imageName)
                                                    .ExposePort(EventStoreHttpDockerPort)
                                                    .ExposePort(EventStoreTcpDockerPort)
                                                    .WithName(containerName)
                                                    .WithEnvironment("EVENTSTORE_RUN_PROJECTIONS=all", "EVENTSTORE_START_STANDARD_PROJECTIONS=true")
                                                    .UseNetwork(networkService)
                                                    .Mount(hostFolder, "/var/log/eventstore", MountType.ReadWrite)
                                                    .Build()
                                                    .Start().WaitForPort("2113/tcp", 30000);

            logger.LogInformation("Event Store Container Started");

            return eventStoreContainer;
        }

        public static IContainerService SetupEstateManagementContainer(String containerName, ILogger logger, String imageName,
                                                                       List<INetworkService> networkServices, String hostFolder,
                                                                       (String URL, String UserName, String Password)? dockerCredentials,
                                                                       String securityServiceContainerName,
                                                                       String eventStoreContainerName)
        {
            logger.LogInformation("About to Start Estate Management Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"EventStoreSettings:ConnectionString=ConnectTo=tcp://admin:changeit@{eventStoreContainerName}:{DockerHelper.EventStoreTcpDockerPort};VerboseLogging=true;");
            environmentVariables.Add($"AppSettings:SecurityService=http://{securityServiceContainerName}:{DockerHelper.SecurityServiceDockerPort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=http://{securityServiceContainerName}:{DockerHelper.SecurityServiceDockerPort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.EstateManagementDockerPort}");

            ContainerBuilder estateManagementContainer = new Builder()
                                                         .UseContainer()
                                                         .WithName(containerName)
                                                         .WithEnvironment(environmentVariables.ToArray())
                                                         .UseImage(imageName)
                                                         .ExposePort(EstateManagementDockerPort)
                                                         .UseNetwork(networkServices.ToArray())
                                                         .Mount(hostFolder, "/home", MountType.ReadWrite);


            if (dockerCredentials.HasValue)
            {
                estateManagementContainer.WithCredential(dockerCredentials.Value.URL, dockerCredentials.Value.UserName, dockerCredentials.Value.Password);
            }

            // Now build and return the container                
            IContainerService builtContainer = estateManagementContainer.Build().Start().WaitForPort($"{DockerHelper.EstateManagementDockerPort}/tcp", 30000);
            
            logger.LogInformation("Estate Management Container Started");

            return builtContainer;
        }

        public static IContainerService SetupTransactionProcessorContainer(String containerName, ILogger logger, String imageName,
                                                                            List<INetworkService> networkServices, String hostFolder,
                                                                            (String URL, String UserName, String Password)? dockerCredentials,
                                                                            String securityServiceContainerName,
                                                                            String eventStoreContainerName)
        {
            logger.LogInformation("About to Start Transaction Processor Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"EventStoreSettings:ConnectionString=ConnectTo=tcp://admin:changeit@{eventStoreContainerName}:{DockerHelper.EventStoreTcpDockerPort};VerboseLogging=true;");
            environmentVariables.Add($"AppSettings:SecurityService=http://{securityServiceContainerName}:{DockerHelper.SecurityServiceDockerPort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=http://{securityServiceContainerName}:{DockerHelper.SecurityServiceDockerPort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.TransactionProcessorDockerPort}");

            ContainerBuilder transactionProcessorContainer = new Builder()
                                                             .UseContainer()
                                                             .WithName(containerName)
                                                             .WithEnvironment(environmentVariables.ToArray())
                                                             .UseImage(imageName)
                                                             .ExposePort(TransactionProcessorDockerPort)
                                                             .UseNetwork(networkServices.ToArray())
                                                             .Mount(hostFolder, "/home", MountType.ReadWrite);

            if (dockerCredentials.HasValue)
            {
                transactionProcessorContainer.WithCredential(dockerCredentials.Value.URL, dockerCredentials.Value.UserName, dockerCredentials.Value.Password);
            }

            // Now build and return the container                
            IContainerService builtContainer = transactionProcessorContainer.Build().Start().WaitForPort($"{DockerHelper.TransactionProcessorDockerPort}/tcp", 30000);

            logger.LogInformation("Transaction Processor Container Started");

            return builtContainer;
        }

        public static IContainerService SetupTransactionProcessorACLContainer(String containerName, ILogger logger, String imageName,
                                                                              INetworkService networkService, String hostFolder,
                                                                              (String URL, String UserName, String Password)? dockerCredentials,
                                                                              String securityServiceContainerName)
        {
            logger.LogInformation("About to Start Transaction Processor ACL Container");

            ContainerBuilder transactionProcessorACLContainer = new Builder()
                                                                .UseContainer()
                                                                .WithName(containerName)
                                                                .UseImage(imageName)
                                                                .ExposePort(DockerHelper.TransactionProcessorACLDockerPort)
                                                                .UseNetwork(new List<INetworkService> { networkService }.ToArray())
                                                                .Mount(hostFolder, "/home", MountType.ReadWrite);


            if (dockerCredentials.HasValue)
            {
                transactionProcessorACLContainer.WithCredential(dockerCredentials.Value.URL, dockerCredentials.Value.UserName, dockerCredentials.Value.Password);
            }

            // Now build and return the container                
            IContainerService builtContainer = transactionProcessorACLContainer.Build().Start().WaitForPort($"{DockerHelper.TransactionProcessorDockerPort}/tcp", 30000);

            logger.LogInformation("Transaction Processor Container ACL Started");

            return builtContainer;
        }

        public static String StartSqlContainerWithOpenConnection(String containerName, ILogger logger, String imageName,
                                                                    INetworkService networkService, String hostFolder,
                                                                    (String URL, String UserName, String Password)? dockerCredentials)
        {
            IContainerService databaseServerContainer = new Ductus.FluentDocker.Builders.Builder()
                                                        .UseContainer()
                                                        .WithName(containerName)
                                                        .UseImage(imageName)
                                                        .WithEnvironment("ACCEPT_EULA=Y", $"SA_PASSWORD=thisisalongpassword123!")
                                                        .ExposePort(1433)
                                                        .UseNetwork(networkService)
                                                        .KeepContainer()
                                                        .KeepRunning()
                                                        .ReuseIfExists()
                                                        .Build()
                                                        .Start()
                                                        .WaitForPort("1433/tcp", 30000);

            IPEndPoint sqlServerEndpoint = databaseServerContainer.ToHostExposedEndpoint("1433/tcp");

            // Try opening a connection
            Int32 maxRetries = 10;
            Int32 counter = 1;

            String server = "127.0.0.1";
            String database = "SubscriptionServiceConfiguration";
            String user = "sa";
            String password = "thisisalongpassword123!";
            String port = sqlServerEndpoint.Port.ToString();

            String connectionString = $"server={server},{port};user id={user}; password={password}; database={database};";

            SqlConnection connection = new SqlConnection(connectionString);

            using (StreamWriter sw = new StreamWriter("C:\\Temp\\testlog.log", true))
            {
                while (counter <= maxRetries)
                {
                    try
                    {
                        sw.WriteLine($"Attempt {counter}");
                        sw.WriteLine(DateTime.Now);

                        connection.Open();

                        SqlCommand command = connection.CreateCommand();
                        command.CommandText = "SELECT * FROM EventStoreServers";
                        command.ExecuteNonQuery();

                        sw.WriteLine("Connection Opened");

                        connection.Close();

                        break;
                    }
                    catch (SqlException ex)
                    {
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }

                        sw.WriteLine(ex);
                        Thread.Sleep(20000);
                    }
                    finally
                    {
                        counter++;
                    }
                }
            }

            return $"server={containerName};user id={user}; password={password};";
        }
    }
}
