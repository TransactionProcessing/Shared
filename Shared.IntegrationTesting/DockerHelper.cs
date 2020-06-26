namespace Shared.IntegrationTesting
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;
    using Logger;
    using Microsoft.Data.SqlClient;

    /// <summary>
    /// 
    /// </summary>
    public abstract class DockerHelper
    {
        #region Methods

        /// <summary>
        /// Setups the estate management container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="hostFolder">The host folder.</param>
        /// <param name="dockerCredentials">The docker credentials.</param>
        /// <param name="securityServiceContainerName">Name of the security service container.</param>
        /// <param name="eventStoreContainerName">Name of the event store container.</param>
        /// <param name="sqlServerDetails">The SQL server details.</param>
        /// <param name="clientDetails">The client details.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public static IContainerService SetupEstateManagementContainer(String containerName,
                                                                       ILogger logger,
                                                                       String imageName,
                                                                       List<INetworkService> networkServices,
                                                                       String hostFolder,
                                                                       (String URL, String UserName, String Password)? dockerCredentials,
                                                                       String securityServiceContainerName,
                                                                       String eventStoreContainerName,
                                                                       (String sqlServerContainerName, String sqlServerUserName, String sqlServerPassword)
                                                                           sqlServerDetails,
                                                                       (String clientId, String clientSecret) clientDetails,
                                                                       Boolean forceLatestImage = false,
                                                                       Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                       List<String> additionalEnvironmentVariables = null)
        {
            logger.LogInformation("About to Start Estate Management Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables
                .Add($"EventStoreSettings:ConnectionString=ConnectTo=tcp://admin:changeit@{eventStoreContainerName}:{DockerHelper.EventStoreTcpDockerPort};VerboseLogging=true;");
            environmentVariables.Add($"AppSettings:SecurityService=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.EstateManagementDockerPort}");
            environmentVariables
                .Add($"ConnectionStrings:EstateReportingReadModel=\"server={sqlServerDetails.sqlServerContainerName};user id={sqlServerDetails.sqlServerUserName};password={sqlServerDetails.sqlServerPassword};database=EstateReportingReadModel\"");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder estateManagementContainer = new Builder().UseContainer().WithName(containerName).WithEnvironment(environmentVariables.ToArray())
                                                                      .UseImage(imageName, forceLatestImage).ExposePort(DockerHelper.EstateManagementDockerPort)
                                                                      .UseNetwork(networkServices.ToArray()).Mount(hostFolder, "/home", MountType.ReadWrite);

            if (dockerCredentials.HasValue)
            {
                estateManagementContainer.WithCredential(dockerCredentials.Value.URL, dockerCredentials.Value.UserName, dockerCredentials.Value.Password);
            }

            // Now build and return the container                
            IContainerService builtContainer = estateManagementContainer.Build().Start().WaitForPort($"{DockerHelper.EstateManagementDockerPort}/tcp", 30000);

            logger.LogInformation("Estate Management Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the estate reporting container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="hostFolder">The host folder.</param>
        /// <param name="dockerCredentials">The docker credentials.</param>
        /// <param name="securityServiceContainerName">Name of the security service container.</param>
        /// <param name="sqlServerDetails">The SQL server details.</param>
        /// <param name="clientDetails">The client details.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public static IContainerService SetupEstateReportingContainer(String containerName,
                                                                      ILogger logger,
                                                                      String imageName,
                                                                      List<INetworkService> networkServices,
                                                                      String hostFolder,
                                                                      (String URL, String UserName, String Password)? dockerCredentials,
                                                                      String securityServiceContainerName,
                                                                      (String sqlServerContainerName, String sqlServerUserName, String sqlServerPassword)
                                                                          sqlServerDetails,
                                                                      (String clientId, String clientSecret) clientDetails,
                                                                      Boolean forceLatestImage = false,
                                                                      Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                      List<String> additionalEnvironmentVariables = null)
        {
            logger.LogInformation("About to Start Estate Reporting Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"AppSettings:SecurityService=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.EstateReportingDockerPort}");
            environmentVariables
                .Add($"ConnectionStrings:EstateReportingReadModel=\"server={sqlServerDetails.sqlServerContainerName};user id={sqlServerDetails.sqlServerUserName};password={sqlServerDetails.sqlServerPassword};database=EstateReportingReadModel\"");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder estateReportingContainer = new Builder().UseContainer().WithName(containerName).WithEnvironment(environmentVariables.ToArray())
                                                                     .UseImage(imageName, forceLatestImage).ExposePort(DockerHelper.EstateReportingDockerPort)
                                                                     .UseNetwork(networkServices.ToArray()).Mount(hostFolder, "/home", MountType.ReadWrite);

            if (dockerCredentials.HasValue)
            {
                estateReportingContainer.WithCredential(dockerCredentials.Value.URL, dockerCredentials.Value.UserName, dockerCredentials.Value.Password);
            }

            // Now build and return the container                
            IContainerService builtContainer = estateReportingContainer.Build().Start().WaitForPort($"{DockerHelper.EstateReportingDockerPort}/tcp", 30000);

            logger.LogInformation("Estate Reporting Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the event store container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="hostFolder">The host folder.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="usesEventStore2006OrLater">if set to <c>true</c> [uses event store2006 or later].</param>
        /// <returns></returns>
        public static IContainerService SetupEventStoreContainer(String containerName,
                                                                 ILogger logger,
                                                                 String imageName,
                                                                 INetworkService networkService,
                                                                 String hostFolder,
                                                                 Boolean forceLatestImage = false,
                                                                 Boolean usesEventStore2006OrLater = false)
        {
            logger.LogInformation("About to Start Event Store Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add("EVENTSTORE_RUN_PROJECTIONS=all");
            environmentVariables.Add("EVENTSTORE_START_STANDARD_PROJECTIONS=true");

            // Add the development mode switch on ES versions >= 20.06 otherwise 
            // SSL cerificate needed to run
            if (usesEventStore2006OrLater)
            {
                environmentVariables.Add("EVENTSTORE_DEV=true");
                environmentVariables.Add("EVENTSTORE_ENABLE_EXTERNAL_TCP=true");
            }

            var eventStoreContainerBuilder = new Builder().UseContainer().UseImage(imageName, forceLatestImage).ExposePort(DockerHelper.EventStoreHttpDockerPort)
                                                          .WithName(containerName).WithEnvironment(environmentVariables.ToArray()).UseNetwork(networkService)
                                                          .Mount(hostFolder, "/var/log/eventstore", MountType.ReadWrite);

            if (usesEventStore2006OrLater == false)
            {
                eventStoreContainerBuilder = eventStoreContainerBuilder.ExposePort(DockerHelper.EventStoreTcpDockerPort);
            }
                                                                 
            IContainerService eventStoreContainer = eventStoreContainerBuilder.Build().Start().WaitForPort("2113/tcp", 30000);

            logger.LogInformation("Event Store Container Started");

            return eventStoreContainer;
        }

        /// <summary>
        /// Setups the security service container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="hostFolder">The host folder.</param>
        /// <param name="dockerCredentials">The docker credentials.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public static IContainerService SetupSecurityServiceContainer(String containerName,
                                                                      ILogger logger,
                                                                      String imageName,
                                                                      INetworkService networkService,
                                                                      String hostFolder,
                                                                      (String URL, String UserName, String Password)? dockerCredentials,
                                                                      Boolean forceLatestImage = false,
                                                                      List<String> additionalEnvironmentVariables = null)
        {
            logger.LogInformation("About to Start Security Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"ServiceOptions:PublicOrigin=http://{containerName}:{DockerHelper.SecurityServiceDockerPort}");
            environmentVariables.Add($"ServiceOptions:IssuerUrl=http://{containerName}:{DockerHelper.SecurityServiceDockerPort}");
            environmentVariables.Add("ASPNETCORE_ENVIRONMENT=IntegrationTest");
            environmentVariables.Add("urls=http://*:5001");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder securityServiceContainer = new Builder().UseContainer().WithName(containerName)
                                                                     .WithEnvironment(environmentVariables.ToArray()).UseImage(imageName, forceLatestImage)
                                                                     .ExposePort(DockerHelper.SecurityServiceDockerPort).UseNetwork(new List<INetworkService>
                                                                                                                                    {
                                                                                                                                        networkService
                                                                                                                                    }.ToArray()).Mount(hostFolder,
                                                                                                                                                       "/home/txnproc/trace",
                                                                                                                                                       MountType
                                                                                                                                                           .ReadWrite);

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

        /// <summary>
        /// Setups the subscription service container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="hostFolder">The host folder.</param>
        /// <param name="dockerCredentials">The docker credentials.</param>
        /// <param name="securityServiceContainerName">Name of the security service container.</param>
        /// <param name="sqlServerDetails">The SQL server details.</param>
        /// <param name="eventStoreServerId">The event store server identifier.</param>
        /// <param name="clientDetails">The client details.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public static IContainerService SetupSubscriptionServiceContainer(String containerName,
                                                                          ILogger logger,
                                                                          String imageName,
                                                                          List<INetworkService> networkServices,
                                                                          String hostFolder,
                                                                          (String URL, String UserName, String Password)? dockerCredentials,
                                                                          String securityServiceContainerName,
                                                                          (String sqlServerContainerName, String sqlServerUserName, String sqlServerPassword)
                                                                              sqlServerDetails,
                                                                          Guid eventStoreServerId,
                                                                          (String clientId, String clientSecret) clientDetails,
                                                                          Boolean forceLatestImage = false,
                                                                          Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                          List<String> additionalEnvironmentVariables = null)
        {
            logger.LogInformation("About to Start Subscription Service Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"AppSettings:SecurityService=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"AppSettings:EventStoreServerId={eventStoreServerId}");
            environmentVariables.Add($"SecurityConfiguration:Authority=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables
                .Add($"ConnectionStrings:SubscriptionService=\"server={sqlServerDetails.sqlServerContainerName};user id={sqlServerDetails.sqlServerUserName};password={sqlServerDetails.sqlServerPassword};database=SubscriptionServiceConfiguration\"");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder subscriptionServiceContainer = new Builder().UseContainer().WithName(containerName).WithEnvironment(environmentVariables.ToArray())
                                                                         .UseImage(imageName, forceLatestImage).UseNetwork(networkServices.ToArray())
                                                                         .Mount(hostFolder, "/home", MountType.ReadWrite);

            if (dockerCredentials.HasValue)
            {
                subscriptionServiceContainer.WithCredential(dockerCredentials.Value.URL, dockerCredentials.Value.UserName, dockerCredentials.Value.Password);
            }

            // Now build and return the container                
            IContainerService builtContainer = subscriptionServiceContainer.Build().Start();

            logger.LogInformation("Subscription Service Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the test network.
        /// </summary>
        /// <param name="networkName">Name of the network.</param>
        /// <param name="reuseIfExists">if set to <c>true</c> [reuse if exists].</param>
        /// <returns></returns>
        public static INetworkService SetupTestNetwork(String networkName = null,
                                                       Boolean reuseIfExists = false)
        {
            networkName = string.IsNullOrEmpty(networkName) ? $"testnetwork{Guid.NewGuid()}" : networkName;

            // Build a network
            NetworkBuilder networkService = new Builder().UseNetwork(networkName);

            if (reuseIfExists)
            {
                networkService.ReuseIfExist();
            }

            return networkService.Build();
        }

        /// <summary>
        /// Setups the transaction processor acl container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="hostFolder">The host folder.</param>
        /// <param name="dockerCredentials">The docker credentials.</param>
        /// <param name="securityServiceContainerName">Name of the security service container.</param>
        /// <param name="transactionProcessorContainerName">Name of the transaction processor container.</param>
        /// <param name="clientDetails">The client details.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public static IContainerService SetupTransactionProcessorACLContainer(String containerName,
                                                                              ILogger logger,
                                                                              String imageName,
                                                                              INetworkService networkService,
                                                                              String hostFolder,
                                                                              (String URL, String UserName, String Password)? dockerCredentials,
                                                                              String securityServiceContainerName,
                                                                              String transactionProcessorContainerName,
                                                                              (String clientId, String clientSecret) clientDetails,
                                                                              Boolean forceLatestImage = false,
                                                                              Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                              List<String> additionalEnvironmentVariables = null)
        {
            logger.LogInformation("About to Start Transaction Processor ACL Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables.Add($"AppSettings:SecurityService=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.TransactionProcessorACLDockerPort}");
            environmentVariables.Add($"AppSettings:TransactionProcessorApi=http://{transactionProcessorContainerName}:{DockerHelper.TransactionProcessorDockerPort}");
            environmentVariables.Add($"AppSettings:ClientId={clientDetails.clientId}");
            environmentVariables.Add($"AppSettings:ClientSecret={clientDetails.clientSecret}");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder transactionProcessorACLContainer = new Builder()
                                                                .UseContainer().WithName(containerName).WithEnvironment(environmentVariables.ToArray())
                                                                .UseImage(imageName, forceLatestImage).ExposePort(DockerHelper.TransactionProcessorACLDockerPort)
                                                                .UseNetwork(new List<INetworkService>
                                                                            {
                                                                                networkService
                                                                            }.ToArray()).Mount(hostFolder, "/home", MountType.ReadWrite);

            if (dockerCredentials.HasValue)
            {
                transactionProcessorACLContainer.WithCredential(dockerCredentials.Value.URL, dockerCredentials.Value.UserName, dockerCredentials.Value.Password);
            }

            // Now build and return the container                
            IContainerService builtContainer =
                transactionProcessorACLContainer.Build().Start().WaitForPort($"{DockerHelper.TransactionProcessorACLDockerPort}/tcp", 30000);

            logger.LogInformation("Transaction Processor Container ACL Started");

            return builtContainer;
        }

        /// <summary>
        /// Setups the transaction processor container.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkServices">The network services.</param>
        /// <param name="hostFolder">The host folder.</param>
        /// <param name="dockerCredentials">The docker credentials.</param>
        /// <param name="securityServiceContainerName">Name of the security service container.</param>
        /// <param name="estateManagementContainerName">Name of the estate management container.</param>
        /// <param name="eventStoreContainerName">Name of the event store container.</param>
        /// <param name="clientDetails">The client details.</param>
        /// <param name="testhostContainerName">Name of the testhost container.</param>
        /// <param name="forceLatestImage">if set to <c>true</c> [force latest image].</param>
        /// <param name="securityServicePort">The security service port.</param>
        /// <param name="additionalEnvironmentVariables">The additional environment variables.</param>
        /// <returns></returns>
        public static IContainerService SetupTransactionProcessorContainer(String containerName,
                                                                           ILogger logger,
                                                                           String imageName,
                                                                           List<INetworkService> networkServices,
                                                                           String hostFolder,
                                                                           (String URL, String UserName, String Password)? dockerCredentials,
                                                                           String securityServiceContainerName,
                                                                           String estateManagementContainerName,
                                                                           String eventStoreContainerName,
                                                                           (String clientId, String clientSecret) clientDetails,
                                                                           String testhostContainerName,
                                                                           Boolean forceLatestImage = false,
                                                                           Int32 securityServicePort = DockerHelper.SecurityServiceDockerPort,
                                                                           List<String> additionalEnvironmentVariables = null)
        {
            logger.LogInformation("About to Start Transaction Processor Container");

            List<String> environmentVariables = new List<String>();
            environmentVariables
                .Add($"EventStoreSettings:ConnectionString=ConnectTo=tcp://admin:changeit@{eventStoreContainerName}:{DockerHelper.EventStoreTcpDockerPort};VerboseLogging=true;");
            environmentVariables.Add($"AppSettings:SecurityService=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"AppSettings:EstateManagementApi=http://{estateManagementContainerName}:{DockerHelper.EstateManagementDockerPort}");
            environmentVariables.Add($"SecurityConfiguration:Authority=http://{securityServiceContainerName}:{securityServicePort}");
            environmentVariables.Add($"urls=http://*:{DockerHelper.TransactionProcessorDockerPort}");
            environmentVariables.Add($"AppSettings:ClientId={clientDetails.clientId}");
            environmentVariables.Add($"AppSettings:ClientSecret={clientDetails.clientSecret}");

            environmentVariables.Add($"OperatorConfiguration:Safaricom:Url=http://{testhostContainerName}:9000/api/safaricom");

            if (additionalEnvironmentVariables != null)
            {
                environmentVariables.AddRange(additionalEnvironmentVariables);
            }

            ContainerBuilder transactionProcessorContainer = new Builder().UseContainer().WithName(containerName).WithEnvironment(environmentVariables.ToArray())
                                                                          .UseImage(imageName, forceLatestImage).ExposePort(DockerHelper.TransactionProcessorDockerPort)
                                                                          .UseNetwork(networkServices.ToArray()).Mount(hostFolder, "/home", MountType.ReadWrite);

            if (dockerCredentials.HasValue)
            {
                transactionProcessorContainer.WithCredential(dockerCredentials.Value.URL, dockerCredentials.Value.UserName, dockerCredentials.Value.Password);
            }

            // Now build and return the container                
            IContainerService builtContainer = transactionProcessorContainer.Build().Start().WaitForPort($"{DockerHelper.TransactionProcessorDockerPort}/tcp", 30000);

            logger.LogInformation("Transaction Processor Container Started");

            return builtContainer;
        }

        /// <summary>
        /// Starts the containers for scenario run.
        /// </summary>
        /// <param name="scenarioName">Name of the scenario.</param>
        /// <returns></returns>
        public abstract Task StartContainersForScenarioRun(String scenarioName);

        /// <summary>
        /// Starts the SQL container with open connection.
        /// </summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <param name="networkService">The network service.</param>
        /// <param name="hostFolder">The host folder.</param>
        /// <param name="dockerCredentials">The docker credentials.</param>
        /// <param name="sqlUserName">Name of the SQL user.</param>
        /// <param name="sqlPassword">The SQL password.</param>
        /// <returns></returns>
        public static IContainerService StartSqlContainerWithOpenConnection(String containerName,
                                                                            ILogger logger,
                                                                            String imageName,
                                                                            INetworkService networkService,
                                                                            String hostFolder,
                                                                            (String URL, String UserName, String Password)? dockerCredentials,
                                                                            String sqlUserName = "sa",
                                                                            String sqlPassword = "thisisalongpassword123!")
        {
            logger.LogInformation("About to start SQL Server Container");
            IContainerService databaseServerContainer = new Builder().UseContainer().WithName(containerName).UseImage(imageName)
                                                                     .WithEnvironment("ACCEPT_EULA=Y", $"SA_PASSWORD={sqlPassword}").ExposePort(1433)
                                                                     .UseNetwork(networkService).KeepContainer().KeepRunning().ReuseIfExists().Build().Start()
                                                                     .WaitForPort("1433/tcp", 30000);

            logger.LogInformation("SQL Server Container Started");

            logger.LogInformation("About to SQL Server Container is running");
            IPEndPoint sqlServerEndpoint = databaseServerContainer.ToHostExposedEndpoint("1433/tcp");

            // Try opening a connection
            Int32 maxRetries = 10;
            Int32 counter = 1;

            String server = "127.0.0.1";
            String database = "SubscriptionServiceConfiguration";
            String user = sqlUserName;
            String password = sqlPassword;
            String port = sqlServerEndpoint.Port.ToString();

            String connectionString = $"server={server},{port};user id={user}; password={password}; database={database};";
            logger.LogInformation($"Connection String {connectionString}");
            SqlConnection connection = new SqlConnection(connectionString);

            while (counter <= maxRetries)
            {
                try
                {
                    logger.LogInformation($"Database Connection Attempt {counter}");

                    connection.Open();

                    SqlCommand command = connection.CreateCommand();
                    command.CommandText = "SELECT * FROM EventStoreServer";
                    command.ExecuteNonQuery();

                    logger.LogInformation("Connection Opened");

                    connection.Close();
                    logger.LogInformation("SQL Server Container Running");
                    break;
                }
                catch(SqlException ex)
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }

                    logger.LogError(ex);
                    Thread.Sleep(20000);
                }
                finally
                {
                    counter++;
                }
            }

            return databaseServerContainer;
        }

        /// <summary>
        /// Stops the containers for scenario run.
        /// </summary>
        /// <returns></returns>
        public abstract Task StopContainersForScenarioRun();

        #endregion

        #region Others

        /// <summary>
        /// The estate management docker port
        /// </summary>
        public const Int32 EstateManagementDockerPort = 5000;

        /// <summary>
        /// The estate reporting docker port
        /// </summary>
        public const Int32 EstateReportingDockerPort = 5005;

        /// <summary>
        /// The event store HTTP docker port
        /// </summary>
        public const Int32 EventStoreHttpDockerPort = 2113;

        /// <summary>
        /// The event store TCP docker port
        /// </summary>
        public const Int32 EventStoreTcpDockerPort = 1113;

        /// <summary>
        /// The security service docker port
        /// </summary>
        public const Int32 SecurityServiceDockerPort = 5001;

        /// <summary>
        /// The transaction processor acl docker port
        /// </summary>
        public const Int32 TransactionProcessorACLDockerPort = 5003;

        /// <summary>
        /// The transaction processor docker port
        /// </summary>
        public const Int32 TransactionProcessorDockerPort = 5002;

        #endregion
    }
}