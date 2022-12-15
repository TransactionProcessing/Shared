namespace Shared.IntegrationTesting;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using EventStore.Client;
using HealthChecks;
using Logger;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Shouldly;

public enum DockerEnginePlatform
{
    Linux,

    Windows
}

public abstract class BaseDockerHelper
{
    #region Fields

    public (String URL, String UserName, String Password)? DockerCredentials;

    public ILogger Logger;

    public (String usename, String password)? SqlCredentials;

    public IContainerService SqlServerContainer;

    public String SqlServerContainerName;

    public INetworkService SqlServerNetwork;

    public Guid TestId;

    protected String CallbackHandlerContainerName;

    protected Int32 CallbackHandlerPort;

    protected (String clientId, String clientSecret) ClientDetails;

    protected List<IContainerService> Containers;

    protected String EstateManagementContainerName;

    protected Int32 EstateManagementPort;
    
    protected String EventStoreContainerName;

    protected Int32 EventStoreHttpPort;

    protected String FileProcessorContainerName;

    protected Int32 FileProcessorPort;

    protected readonly IHealthCheckClient HealthCheckClient;

    protected String HostTraceFolder;

    protected Dictionary<ContainerType, (String imageName, Boolean useLatest)> ImageDetails = new Dictionary<ContainerType, (String imageName, Boolean useLatest)>();

    protected String MessagingServiceContainerName;

    protected Int32 MessagingServicePort;

    protected (Int32 pollingInterval, Int32 cacheDuration) PersistentSubscriptionSettings = (10, 0);

    protected String SecurityServiceContainerName;

    protected Int32 SecurityServicePort;

    protected String TestHostContainerName;

    protected Int32 TestHostServicePort;

    protected List<INetworkService> TestNetworks;

    protected String TransactionProcessorAclContainerName;

    protected Int32 TransactionProcessorAclPort;

    protected String TransactionProcessorContainerName;

    protected Int32 TransactionProcessorPort;

    protected String VoucherManagementAclContainerName;

    protected Int32 VoucherManagementAclPort;

    protected Boolean UseSecureSqlServerDatabase;

    #endregion

    #region Constructors

    public BaseDockerHelper() {
        this.Containers = new List<IContainerService>();
        this.TestNetworks = new List<INetworkService>();
        this.HealthCheckClient = new HealthCheckClient(new HttpClient(new SocketsHttpHandler {
                                                                                                 SslOptions = new SslClientAuthenticationOptions {
                                                                                                                  RemoteCertificateValidationCallback = (sender,
                                                                                                                      certificate,
                                                                                                                      chain,
                                                                                                                      errors) => true
                                                                                                              }
                                                                                             }));

        // Setup the default image details
        DockerEnginePlatform engineType = BaseDockerHelper.GetDockerEnginePlatform();
        if (engineType == DockerEnginePlatform.Windows)
        {
            this.ImageDetails.Add(ContainerType.SqlServer, ("iamrjindal/sqlserverexpress:2019", true));
            this.ImageDetails.Add(ContainerType.EventStore, ("stuartferguson/eventstore", true));
            this.ImageDetails.Add(ContainerType.MessagingService, ("stuartferguson/messagingservicewindows:master", true));
            this.ImageDetails.Add(ContainerType.SecurityService, ("stuartferguson/securityservicewindows:master", true));
            this.ImageDetails.Add(ContainerType.CallbackHandler, ("stuartferguson/callbackhandlerwindows:master", true));
            this.ImageDetails.Add(ContainerType.TestHost, ("stuartferguson/testhostswindows:master", true));
            this.ImageDetails.Add(ContainerType.EstateManagement, ("stuartferguson/estatemanagementwindows:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessor, ("stuartferguson/transactionprocessorwindows:master", true));
            this.ImageDetails.Add(ContainerType.FileProcessor, ("stuartferguson/fileprocessorwindows:master", true));
            this.ImageDetails.Add(ContainerType.VoucherManagementAcl, ("stuartferguson/vouchermanagementaclwindows:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessorAcl, ("stuartferguson/transactionprocessoraclwindows:master", true));
        }
        else {
            this.ImageDetails.Add(ContainerType.SqlServer, ("mcr.microsoft.com/mssql/server:2019-latest", true));
            this.ImageDetails.Add(ContainerType.EventStore, ("eventstore/eventstore:21.10.0-buster-slim", true));
            this.ImageDetails.Add(ContainerType.MessagingService, ("stuartferguson/messagingservice:master", true));
            this.ImageDetails.Add(ContainerType.SecurityService, ("stuartferguson/securityservice:master", true));
            this.ImageDetails.Add(ContainerType.CallbackHandler, ("stuartferguson/callbackhandler:master", true));
            this.ImageDetails.Add(ContainerType.TestHost, ("stuartferguson/testhosts:master", true));
            this.ImageDetails.Add(ContainerType.EstateManagement, ("stuartferguson/estatemanagement:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessor, ("stuartferguson/transactionprocessor:master", true));
            this.ImageDetails.Add(ContainerType.FileProcessor, ("stuartferguson/fileprocessor:master", true));
            this.ImageDetails.Add(ContainerType.VoucherManagementAcl, ("stuartferguson/vouchermanagementacl:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessorAcl, ("stuartferguson/transactionprocessoracl:master", true));
        }
    }

    #endregion

    #region Properties

    public Boolean IsSecureEventStore { get; protected set; }

    protected String InsecureEventStoreEnvironmentVariable =>
        this.IsSecureEventStore switch {
            true => "EventStoreSettings:Insecure=False",
            _ => "EventStoreSettings:Insecure=True"
        };

    #endregion

    #region Methods

    public List<String> GetCommonEnvironmentVariables(Int32 securityServicePort) {
        var healthCheckConnString = SetConnectionString("ConnectionStrings:HealthCheck", "master", UseSecureSqlServerDatabase);


        return new List<String> {
                                    $"EventStoreSettings:ConnectionString={this.GenerateEventStoreConnectionString()}",
                                    this.InsecureEventStoreEnvironmentVariable,
                                    $"AppSettings:PersistentSubscriptionPollingInSeconds={this.PersistentSubscriptionSettings.pollingInterval}",
                                    $"AppSettings:InternalSubscriptionServiceCacheDuration={this.PersistentSubscriptionSettings.cacheDuration}",
                                    $"AppSettings:SubscriptionConfiguration:PersistentSubscriptionPollingInSeconds={this.PersistentSubscriptionSettings.pollingInterval}",
                                    $"AppSettings:SubscriptionConfiguration:InternalSubscriptionServiceCacheDuration={this.PersistentSubscriptionSettings.cacheDuration}",
                                    $"AppSettings:SecurityService=https://{this.SecurityServiceContainerName}:{securityServicePort}",
                                    $"SecurityConfiguration:Authority=https://{this.SecurityServiceContainerName}:{securityServicePort}",
                                    $"AppSettings:ClientId={this.ClientDetails.clientId}",
                                    $"AppSettings:ClientSecret={this.ClientDetails.clientSecret}",
                                    $"AppSettings:MessagingServiceApi=http://{this.MessagingServiceContainerName}:{DockerPorts.MessagingServiceDockerPort}",
                                    $"AppSettings:TransactionProcessorApi=http://{this.TransactionProcessorContainerName}:{DockerPorts.TransactionProcessorDockerPort}",
                                    $"AppSettings:EstateManagementApi=http://{this.EstateManagementContainerName}:{DockerPorts.EstateManagementDockerPort}",
                                    healthCheckConnString
                                };
    }

    public static DockerEnginePlatform GetDockerEnginePlatform() {
        IHostService docker = BaseDockerHelper.GetDockerHost();
        
        if (docker.Host.IsLinuxEngine()) {
            return DockerEnginePlatform.Linux;
        }

        if (docker.Host.IsWindowsEngine()) {
            return DockerEnginePlatform.Windows;
        }

        throw new Exception("Unknown Engine Type");
    }

    public static IHostService GetDockerHost() {
        IList<IHostService> hosts = new Hosts().Discover();
        IHostService docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        return docker;
    }

    public (String imageName, Boolean useLatest) GetImageDetails(ContainerType key) {
        KeyValuePair<ContainerType, (String imageName, Boolean useLatest)> details = this.ImageDetails.SingleOrDefault(c => c.Key == key);
        if (details.Equals(default(KeyValuePair<ContainerType, (String, Boolean)>))) {
            // No details found so throw an error
            throw new Exception($"No image details found for Container Type [{key}]");
        }

        return details.Value;
    }

    public void SetImageDetails(ContainerType key,
                                (String imageName, Boolean useLatest) newDetails) {
        KeyValuePair<ContainerType, (String imageName, Boolean useLatest)> details = this.ImageDetails.SingleOrDefault(c => c.Key == key);
        if (details.Equals(default(KeyValuePair<ContainerType, (String, Boolean)>)) == false) {
            // Found so we can overwrite
            this.ImageDetails[key] = newDetails;
        }
    }

    public async Task<IContainerService> SetupCallbackHandlerContainer(List<INetworkService> networkServices,
                                                                       List<String> additionalEnvironmentVariables = null) {
        this.Trace("About to Start Callback Handler Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables(DockerPorts.SecurityServiceDockerPort);

        if (additionalEnvironmentVariables != null) {
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder callbackHandlerContainer = new Builder().UseContainer().WithName(this.CallbackHandlerContainerName)
                                                                 .WithEnvironment(environmentVariables.ToArray())
                                                                 .UseImageDetails(this.GetImageDetails(ContainerType.CallbackHandler))
                                                                 .ExposePort(DockerPorts.CallbackHandlerDockerPort)
                                                                 .MountHostFolder(this.HostTraceFolder)
                                                                 .SetDockerCredentials(this.DockerCredentials);

        // Now build and return the container                
        IContainerService builtContainer = callbackHandlerContainer.Build().Start().WaitForPort($"{DockerPorts.CallbackHandlerDockerPort}/tcp", 30000);

        foreach (INetworkService networkService in networkServices) {
            networkService.Attach(builtContainer, false);
        }

        this.Trace("Callback Handler Container Started");
        this.Containers.Add(builtContainer);

        //  Do a health check here
        this.CallbackHandlerPort = builtContainer.ToHostExposedEndpoint($"{DockerPorts.CallbackHandlerDockerPort}/tcp").Port;

        await this.DoHealthCheck(ContainerType.CallbackHandler);
        return builtContainer;
    }

    public virtual void SetupContainerNames() {
        // Setup the container names
        this.EventStoreContainerName = $"eventstore{this.TestId:N}";
        this.SecurityServiceContainerName = $"securityservice{this.TestId:N}";
        this.EstateManagementContainerName = $"estate{this.TestId:N}";
        this.TestHostContainerName = $"testhosts{this.TestId:N}";
        this.CallbackHandlerContainerName = $"callbackhandler{this.TestId:N}";
        this.FileProcessorContainerName = $"fileprocessor{this.TestId:N}";
        this.MessagingServiceContainerName = $"messaging{this.TestId:N}";
        this.TransactionProcessorContainerName = $"transaction{this.TestId:N}";
        this.TransactionProcessorAclContainerName = $"transactionacl{this.TestId:N}";
        this.VoucherManagementAclContainerName = $"vouchermanagementacl{this.TestId:N}";
    }

    protected virtual String SetConnectionString(String settingName,
                                                 String databaseName,
                                                 Boolean isSecure = false) {
        String encryptValue = String.Empty;
        if (isSecure == false) {
            encryptValue = ";Encrypt=False";
        }

        String connectionString =
            $"{settingName}=\"server={this.SqlServerContainerName},1433;user id={this.SqlCredentials.Value.usename};password={this.SqlCredentials.Value.password};database={databaseName}{encryptValue}\"";

        return connectionString;
    }

    public virtual async Task<IContainerService> SetupEstateManagementContainer(List<INetworkService> networkServices,
                                                                                Int32 securityServicePort = DockerPorts.SecurityServiceDockerPort,
                                                                                List<String> additionalEnvironmentVariables = null) {
        this.Trace("About to Start Estate Management Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables(securityServicePort);
        environmentVariables.Add($"urls=http://*:{DockerPorts.EstateManagementDockerPort}");
        environmentVariables.Add(SetConnectionString("ConnectionStrings:EstateReportingReadModel", "EstateReportingReadModel", UseSecureSqlServerDatabase));
        if (additionalEnvironmentVariables != null) {
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder estateManagementContainer = new Builder().UseContainer().WithName(this.EstateManagementContainerName)
                                                                  .WithEnvironment(environmentVariables.ToArray())
                                                                  .UseImageDetails(this.GetImageDetails(ContainerType.EstateManagement))
                                                                  .ExposePort(DockerPorts.EstateManagementDockerPort)
                                                                  .MountHostFolder(this.HostTraceFolder)
                                                                  .SetDockerCredentials(this.DockerCredentials);

        // Now build and return the container                
        IContainerService builtContainer = estateManagementContainer.Build().Start().WaitForPort($"{DockerPorts.EstateManagementDockerPort}/tcp", 30000);

        foreach (INetworkService networkService in networkServices)
        {
            networkService.Attach(builtContainer, false);
            var networkConfig = networkService.GetConfiguration(true);
            this.Trace(JsonConvert.SerializeObject(networkConfig));
        }
        
        this.Trace("Estate Management Container Started");
        this.Containers.Add(builtContainer);

        //  Do a health check here
        this.EstateManagementPort = builtContainer.ToHostExposedEndpoint($"{DockerPorts.EstateManagementDockerPort}/tcp").Port;
        await this.DoHealthCheck(ContainerType.EstateManagement);
        return builtContainer;
    }
    
    public virtual async Task<IContainerService> SetupEventStoreContainer(INetworkService networkService,
                                                                          Boolean isSecure = false) {
        this.Trace("About to Start Event Store Container");

        List<String> environmentVariables = new() {
                                                      "EVENTSTORE_RUN_PROJECTIONS=all",
                                                      "EVENTSTORE_START_STANDARD_PROJECTIONS=true",
                                                      "EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=true",
                                                      "EVENTSTORE_ENABLE_EXTERNAL_TCP=true"
                                                  };


        String containerPath = BaseDockerHelper.GetDockerEnginePlatform() switch {
            DockerEnginePlatform.Windows => "C:\\Logs",
            _ => "/var/log/eventstore"
        };

        ContainerBuilder eventStoreContainerBuilder = new Builder().UseContainer().UseImageDetails(this.GetImageDetails(ContainerType.EventStore))
                                                                   .ExposePort(DockerPorts.EventStoreHttpDockerPort)
                                                                   .ExposePort(DockerPorts.EventStoreTcpDockerPort)
                                                                   .WithName(this.EventStoreContainerName)
                                                                   .MountHostFolder(this.HostTraceFolder, containerPath);

        if (isSecure == false) {
            environmentVariables.Add("EVENTSTORE_INSECURE=true");
        }
        else {
            // Copy these to the container
            String path = Path.Combine(Directory.GetCurrentDirectory(), "certs");

            eventStoreContainerBuilder = eventStoreContainerBuilder.Mount(path, "/etc/eventstore/certs", MountType.ReadWrite);

            // Certificates configuration
            environmentVariables.Add("EVENTSTORE_CertificateFile=/etc/eventstore/certs/node1/node.crt");
            environmentVariables.Add("EVENTSTORE_CertificatePrivateKeyFile=/etc/eventstore/certs/node1/node.key");
            environmentVariables.Add("EVENTSTORE_TrustedRootCertificatesPath=/etc/eventstore/certs/ca");
        }

        eventStoreContainerBuilder = eventStoreContainerBuilder.WithEnvironment(environmentVariables.ToArray());

        IContainerService eventStoreContainer = eventStoreContainerBuilder.Build().Start();
        networkService.Attach(eventStoreContainer,false);

        await Retry.For(async () => { eventStoreContainer = eventStoreContainer.WaitForPort($"{DockerPorts.EventStoreHttpDockerPort}/tcp"); });

        this.EventStoreHttpPort = eventStoreContainer.ToHostExposedEndpoint($"{DockerPorts.EventStoreHttpDockerPort}/tcp").Port;
        this.Trace($"EventStore Http Port: [{this.EventStoreHttpPort}]");

        this.Trace("Event Store Container Started");

        this.Containers.Add(eventStoreContainer);
        return eventStoreContainer;
    }

    public virtual async Task<IContainerService> SetupFileProcessorContainer(List<INetworkService> networkServices,
                                                                             Int32 securityServicePort = DockerPorts.SecurityServiceDockerPort,
                                                                             List<String> additionalEnvironmentVariables = null) {
        this.Trace("About to Start File Processor Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables(securityServicePort);
        environmentVariables.Add($"urls=http://*:{DockerPorts.FileProcessorDockerPort}");
        environmentVariables.Add(SetConnectionString("ConnectionStrings:EstateReportingReadModel", "EstateReportingReadModel", UseSecureSqlServerDatabase));

        DockerEnginePlatform enginePlatform = BaseDockerHelper.GetDockerEnginePlatform();
        String ciEnvVar = Environment.GetEnvironmentVariable("CI");
        Boolean isCi = String.IsNullOrEmpty(ciEnvVar) == false && String.Compare(ciEnvVar, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0;

        if (enginePlatform == DockerEnginePlatform.Linux) {
            // we are running in CI Linux
            environmentVariables.Add($"AppSettings:TemporaryFileLocation={"/home/runner/bulkfiles/temporary"}");

            environmentVariables.Add($"AppSettings:FileProfiles:0:ListeningDirectory={"/home/runner/bulkfiles/safaricom"}");
            environmentVariables.Add($"AppSettings:FileProfiles:1:ListeningDirectory={"/home/runner/bulkfiles/voucher"}");
        }
        else {
            // We know this is now windows
            if (isCi) {
                Directory.CreateDirectory("C:\\Users\\runneradmin\\txnproc\\bulkfiles\\temporary");
                Directory.CreateDirectory("C:\\Users\\runneradmin\\txnproc\\bulkfiles\\safaricom");
                Directory.CreateDirectory("C:\\Users\\runneradmin\\txnproc\\bulkfiles\\voucher");

                environmentVariables.Add($"AppSettings:TemporaryFileLocation=\"C:\\Users\\runneradmin\\txnproc\\bulkfiles\\temporary\"");
                environmentVariables.Add($"AppSettings:TemporaryFileLocation=\"C:\\Users\\runneradmin\\txnproc\\bulkfiles\\safaricom\"");
                environmentVariables.Add($"AppSettings:TemporaryFileLocation=\"C:\\Users\\runneradmin\\txnproc\\bulkfiles\\voucher\"");
            }
            else {
                environmentVariables.Add($"AppSettings:TemporaryFileLocation=\"C:\\home\\txnproc\\bulkfiles\\temporary\"");
                environmentVariables.Add($"AppSettings:TemporaryFileLocation=\"C:\\Users\\txnproc\\bulkfiles\\safaricom\"");
                environmentVariables.Add($"AppSettings:TemporaryFileLocation=\"C:\\Users\\txnproc\\bulkfiles\\voucher\"");
            }
        }
        
        if (additionalEnvironmentVariables != null) {
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder fileProcessorContainer = new Builder().UseContainer().WithName(this.FileProcessorContainerName).WithEnvironment(environmentVariables.ToArray())
                                                               .UseImageDetails(this.GetImageDetails(ContainerType.FileProcessor))
                                                               .ExposePort(DockerPorts.FileProcessorDockerPort)
                                                               .MountHostFolder(this.HostTraceFolder).SetDockerCredentials(this.DockerCredentials);

        // Mount the folder to upload files
        String uploadFolder = (enginePlatform, isCi) switch {
            (DockerEnginePlatform.Windows, false) => "C:\\home\\txnproc\\specflow",
            (DockerEnginePlatform.Windows, true) => "C:\\Users\\runneradmin\\txnproc\\specflow",
            _ => "/home/txnproc/specflow"
        };
            
                              //== DockerEnginePlatform.Windows ? "C:\\home\\txnproc\\specflow" : "/home/txnproc/specflow";
        if (enginePlatform == DockerEnginePlatform.Windows && isCi) {
            Directory.CreateDirectory(uploadFolder);
        }

        String containerFolder = enginePlatform == DockerEnginePlatform.Windows ? "C:\\home\\txnproc\\bulkfiles" : "/home/txnproc/bulkfiles";
        fileProcessorContainer.Mount(uploadFolder, containerFolder, MountType.ReadWrite);

        // Now build and return the container                
        IContainerService builtContainer = fileProcessorContainer.Build().Start().WaitForPort($"{DockerPorts.FileProcessorDockerPort}/tcp", 30000);

        foreach (INetworkService networkService in networkServices) {
            networkService.Attach(builtContainer, false);
        }

        this.Trace("File Processor Container Started");
        this.Containers.Add(builtContainer);

        //  Do a health check here
        this.FileProcessorPort = builtContainer.ToHostExposedEndpoint($"{DockerPorts.FileProcessorDockerPort}/tcp").Port;
        await this.DoHealthCheck(ContainerType.FileProcessor);
        return builtContainer;
    }

    public virtual async Task<IContainerService> SetupMessagingServiceContainer(List<INetworkService> networkServices,
                                                                                Int32 securityServicePort = DockerPorts.SecurityServiceDockerPort,
                                                                                List<String> additionalEnvironmentVariables = null) {
        this.Trace("About to Start Messaging Service Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables(securityServicePort);
        environmentVariables.Add($"urls=http://*:{DockerPorts.MessagingServiceDockerPort}");
        environmentVariables.Add("AppSettings:EmailProxy=Integration");
        environmentVariables.Add("AppSettings:SMSProxy=Integration");
        environmentVariables.Add("AppSettings:InternalSubscriptionService=false");

        if (additionalEnvironmentVariables != null) {
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder messagingServiceContainer = new Builder().UseContainer().WithName(this.MessagingServiceContainerName)
                                                                  .WithEnvironment(environmentVariables.ToArray())
                                                                  .UseImageDetails(this.GetImageDetails(ContainerType.MessagingService))
                                                                  .ExposePort(DockerPorts.MessagingServiceDockerPort)
                                                                  .MountHostFolder(this.HostTraceFolder).SetDockerCredentials(this.DockerCredentials);

        // Now build and return the container                
        IContainerService builtContainer = messagingServiceContainer.Build().Start();

        foreach (INetworkService networkService in networkServices) {
            networkService.Attach(builtContainer, false);
        }

        this.Trace("Messaging Service Container Started");
        this.Containers.Add(builtContainer);

        //  Do a health check here
        this.MessagingServicePort = builtContainer.ToHostExposedEndpoint($"{DockerPorts.MessagingServiceDockerPort}/tcp").Port;
        await this.DoHealthCheck(ContainerType.MessagingService);
        return builtContainer;
    }

    public virtual async Task<IContainerService> SetupSecurityServiceContainer(INetworkService networkService,
                                                                               List<String> additionalEnvironmentVariables = null) {
        this.Trace("About to Start Security Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables(DockerPorts.SecurityServiceDockerPort);
        environmentVariables.Add($"ServiceOptions:PublicOrigin=https://{this.SecurityServiceContainerName}:{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add($"ServiceOptions:IssuerUrl=https://{this.SecurityServiceContainerName}:{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add("ASPNETCORE_ENVIRONMENT=IntegrationTest");
        environmentVariables.Add($"urls=https://*:{DockerPorts.SecurityServiceDockerPort}");

        if (additionalEnvironmentVariables != null) {
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder securityServiceContainer = new Builder().UseContainer().WithName(this.SecurityServiceContainerName)
                                                                 .WithEnvironment(environmentVariables.ToArray())
                                                                 .UseImageDetails(this.GetImageDetails(ContainerType.SecurityService))
                                                                 .ExposePort(DockerPorts.SecurityServiceDockerPort)
                                                                 .MountHostFolder(this.HostTraceFolder)
                                                                 .SetDockerCredentials(this.DockerCredentials);

        // Now build and return the container                
        IContainerService builtContainer = securityServiceContainer.Build().Start().WaitForPort($"{DockerPorts.SecurityServiceDockerPort}/tcp", 30000);
        
        networkService.Attach(builtContainer, false);
        
        this.Trace("Security Service Container Started");
        this.Containers.Add(builtContainer);

        //  Do a health check here
        this.SecurityServicePort = builtContainer.ToHostExposedEndpoint($"{DockerPorts.SecurityServiceDockerPort}/tcp").Port;
        await this.DoHealthCheck(ContainerType.SecurityService);

        return builtContainer;
    }

    public virtual IContainerService SetupSqlServerContainer(INetworkService networkService) {
        if (this.SqlCredentials == default)
            throw new Exception("Sql Credentials have not been set");

        this.Trace("About to start SQL Server Container");
        ContainerBuilder containerService = new Builder().UseContainer().WithName(this.SqlServerContainerName)
                                                         .UseImageDetails(this.GetImageDetails(ContainerType.SqlServer))
                                                         .WithEnvironment("ACCEPT_EULA=Y", $"SA_PASSWORD={this.SqlCredentials.Value.password}")
                                                         .ExposePort(1433)
                                                         .KeepContainer().KeepRunning().ReuseIfExists()
                                                         .SetDockerCredentials(this.DockerCredentials);

        IContainerService databaseServerContainer = containerService.Build().Start()
                                                                    .WaitForPort("1433/tcp", 30000);

        networkService.Attach(databaseServerContainer, false);

        var networkConfig = networkService.GetConfiguration(true);
        this.Trace(JsonConvert.SerializeObject(networkConfig));

        this.Trace("SQL Server Container Started");
        // Try opening a connection
        Int32 maxRetries = 10;
        Int32 counter = 1;

        if (networkService != null) {
            this.Trace("About to SQL Server Container is running");
            IPEndPoint sqlServerEndpoint = databaseServerContainer.ToHostExposedEndpoint("1433/tcp");
            
            String server = "127.0.0.1";
            String database = "master";
            String user = this.SqlCredentials.Value.usename;
            String password = this.SqlCredentials.Value.password;
            String port = sqlServerEndpoint.Port.ToString();

            String connectionString = $"server={server},{port};user id={user}; password={password}; database={database};Encrypt=False";
            this.Trace($"Connection String {connectionString}");
            SqlConnection connection = new SqlConnection(connectionString);

            while (counter <= maxRetries) {
                try {
                    this.Trace($"Database Connection Attempt {counter}");

                    connection.Open();

                    SqlCommand command = connection.CreateCommand();
                    //command.CommandText = "SELECT * FROM sys.databases";
                    command.CommandText = "SELECT 1;";
                    command.ExecuteNonQuery();

                    this.Trace("Connection Opened");

                    connection.Close();
                    this.Trace("SQL Server Container Running");
                    break;
                }
                catch(SqlException ex) {
                    if (connection.State == ConnectionState.Open) {
                        connection.Close();
                    }

                    this.Logger.LogError(ex);
                    Thread.Sleep(20000);
                }
                finally {
                    counter++;
                }
            }
        }

        if (counter >= maxRetries) {
            // We have got to the end and still not opened the connection
            throw new Exception($"Database container not started in {maxRetries} retries");
        }

        return databaseServerContainer;
    }

    public virtual async Task<IContainerService> SetupTestHostContainer(List<INetworkService> networkServices,
                                                                        List<String> additionalEnvironmentVariables = null) {
        this.Trace("About to Start Test Hosts Container");

        List<String> environmentVariables = new List<String>();
        environmentVariables.Add(SetConnectionString("ConnectionStrings:TestBankReadModel", "TestBankReadModel", UseSecureSqlServerDatabase));
        environmentVariables.Add(SetConnectionString("ConnectionStrings:PataPawaReadModel", "PataPawaReadModel", UseSecureSqlServerDatabase));
        environmentVariables.Add("ASPNETCORE_ENVIRONMENT=IntegrationTest");

        if (additionalEnvironmentVariables != null) {
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.TestHost);
        ContainerBuilder testHostContainer = new Builder().UseContainer().WithName(this.TestHostContainerName).WithEnvironment(environmentVariables.ToArray())
                                                          .UseImageDetails(this.GetImageDetails(ContainerType.TestHost)).ExposePort(DockerPorts.TestHostPort)
                                                          .MountHostFolder(this.HostTraceFolder)
                                                          .SetDockerCredentials(this.DockerCredentials);
        // Now build and return the container                
        IContainerService builtContainer = testHostContainer.Build().Start().WaitForPort($"{DockerPorts.TestHostPort}/tcp", 30000);

        foreach (INetworkService networkService in networkServices) {
            networkService.Attach(builtContainer,false);
        }

        this.Trace("Test Hosts Container Started");
        this.Containers.Add(builtContainer);

        //  Do a health check here
        this.TestHostServicePort = builtContainer.ToHostExposedEndpoint($"{DockerPorts.TestHostPort}/tcp").Port;
        //await Retry.For(async () => {
        //                    HealthCheckResult healthCheck =
        //                        await this.HealthCheckClient.PerformHealthCheck("http", "127.0.0.1", this.TestHostServicePort, CancellationToken.None);
        //                    healthCheck.Status.ShouldBe(HealthCheckStatus.Healthy.ToString());
        //                });

        return builtContainer;
    }

    public virtual INetworkService SetupTestNetwork(String networkName = null,
                                                    Boolean reuseIfExists = false) {

        networkName = String.IsNullOrEmpty(networkName) ? $"testnw{this.TestId:N}" : networkName;
        DockerEnginePlatform engineType = BaseDockerHelper.GetDockerEnginePlatform();

        if (engineType == DockerEnginePlatform.Windows) {
            var docker = BaseDockerHelper.GetDockerHost();
            var network = docker.GetNetworks().Where(nw => nw.Name == networkName).SingleOrDefault();
            if (network == null) {
                Dictionary<String, String> driverOptions = new Dictionary<String, String>();
                driverOptions.Add("com.docker.network.windowsshim.networkname", networkName);

                network = docker.CreateNetwork(networkName,
                                               new NetworkCreateParams {
                                                                           Driver = "nat",
                                                                           DriverOptions = driverOptions,
                                                                           Attachable = true,
                                                                       });
            }

            return network;
        }

        if (engineType == DockerEnginePlatform.Linux) {
            // Build a network
            NetworkBuilder networkService = new Builder().UseNetwork(networkName).ReuseIfExist();

            return networkService.Build();
        }

        return null;
    }

    public virtual async Task<IContainerService> SetupTransactionProcessorAclContainer(INetworkService networkService,
                                                                                       Int32 securityServicePort = DockerPorts.SecurityServiceDockerPort,
                                                                                       List<String> additionalEnvironmentVariables = null) {
        this.Trace("About to Start Transaction Processor ACL Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables(securityServicePort);
        environmentVariables.Add($"urls=http://*:{DockerPorts.TransactionProcessorAclDockerPort}");

        if (additionalEnvironmentVariables != null) {
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder transactionProcessorACLContainer = new Builder().UseContainer().WithName(this.TransactionProcessorAclContainerName)
                                                                         .WithEnvironment(environmentVariables.ToArray())
                                                                         .UseImageDetails(this.GetImageDetails(ContainerType.TransactionProcessorAcl))
                                                                         .ExposePort(DockerPorts.TransactionProcessorAclDockerPort)
                                                                         .MountHostFolder(this.HostTraceFolder)
                                                                         .SetDockerCredentials(this.DockerCredentials);

        // Now build and return the container                
        IContainerService builtContainer = transactionProcessorACLContainer.Build().Start().WaitForPort($"{DockerPorts.TransactionProcessorAclDockerPort}/tcp", 30000);
        networkService.Attach(builtContainer,false);
        this.Trace("Transaction Processor Container ACL Started");

        this.Containers.Add(builtContainer);

        //  Do a health check here
        this.TransactionProcessorAclPort = builtContainer.ToHostExposedEndpoint($"{DockerPorts.TransactionProcessorAclDockerPort}/tcp").Port;
        await this.DoHealthCheck(ContainerType.TransactionProcessorAcl);

        return builtContainer;
    }

    public virtual async Task<IContainerService> SetupTransactionProcessorContainer(List<INetworkService> networkServices,
                                                                                    Int32 securityServicePort = DockerPorts.SecurityServiceDockerPort,
                                                                                    List<String> additionalEnvironmentVariables = null) {
        this.Trace("About to Start Transaction Processor Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables(securityServicePort);
        environmentVariables.Add($"urls=http://*:{DockerPorts.TransactionProcessorDockerPort}");
        environmentVariables.Add("AppSettings:SubscriptionFilter=TransactionProcessor");
        environmentVariables.Add($"OperatorConfiguration:Safaricom:Url=http://{this.TestHostContainerName}:{DockerPorts.TestHostPort}/api/safaricom");
        environmentVariables
            .Add($"OperatorConfiguration:PataPawaPostPay:Url=http://{this.TestHostContainerName}:{DockerPorts.TestHostPort}/PataPawaPostPayService/basichttp");
        environmentVariables.Add(SetConnectionString("ConnectionStrings:TransactionProcessorReadModel", "TransactionProcessorReadModel", UseSecureSqlServerDatabase));
        environmentVariables.Add(SetConnectionString("ConnectionStrings:EstateReportingReadModel", "EstateReportingReadModel", UseSecureSqlServerDatabase));

        if (additionalEnvironmentVariables != null) {
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder transactionProcessorContainer = new Builder().UseContainer().WithName(this.TransactionProcessorContainerName)
                                                                      .WithEnvironment(environmentVariables.ToArray())
                                                                      .UseImageDetails(this.GetImageDetails(ContainerType.TransactionProcessor))
                                                                      .ExposePort(DockerPorts.TransactionProcessorDockerPort)
                                                                      .MountHostFolder(this.HostTraceFolder)
                                                                      .SetDockerCredentials(this.DockerCredentials);

        // Now build and return the container                
        IContainerService builtContainer = transactionProcessorContainer.Build().Start().WaitForPort($"{DockerPorts.TransactionProcessorDockerPort}/tcp", 30000);
        foreach (INetworkService networkService in networkServices)
        {
            networkService.Attach(builtContainer, false);
        }
        this.Trace("Transaction Processor Container Started");
        this.Containers.Add(builtContainer);

        //  Do a health check here
        this.TransactionProcessorPort = builtContainer.ToHostExposedEndpoint($"{DockerPorts.TransactionProcessorDockerPort}/tcp").Port;
        await this.DoHealthCheck(ContainerType.TransactionProcessor);
        return builtContainer;
    }

    public virtual async Task<IContainerService> SetupVoucherManagementAclContainer(List<INetworkService> networkServices,
                                                                                    Int32 securityServicePort = DockerPorts.SecurityServiceDockerPort,
                                                                                    List<String> additionalEnvironmentVariables = null) {
        this.Trace("About to Start Voucher Management ACL Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables(securityServicePort);
        environmentVariables.Add($"urls=http://*:{DockerPorts.VoucherManagementAclDockerPort}");

        if (additionalEnvironmentVariables != null) {
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder voucherManagementAclContainer = new Builder().UseContainer().WithName(this.VoucherManagementAclContainerName)
                                                                      .WithEnvironment(environmentVariables.ToArray())
                                                                      .UseImageDetails(this.GetImageDetails(ContainerType.VoucherManagementAcl))
                                                                      .ExposePort(DockerPorts.VoucherManagementAclDockerPort)
                                                                      .MountHostFolder(this.HostTraceFolder)
                                                                      .SetDockerCredentials(this.DockerCredentials);

        // Now build and return the container                
        IContainerService builtContainer = voucherManagementAclContainer.Build().Start().WaitForPort($"{DockerPorts.VoucherManagementAclDockerPort}/tcp", 30000);
        foreach (INetworkService networkService in networkServices)
        {
            networkService.Attach(builtContainer, false);
        }
        this.Trace("Voucher Management ACL Container Started");
        this.Containers.Add(builtContainer);

        //  Do a health check here
        this.VoucherManagementAclPort = builtContainer.ToHostExposedEndpoint($"{DockerPorts.VoucherManagementAclDockerPort}/tcp").Port;
        await this.DoHealthCheck(ContainerType.VoucherManagementAcl);

        return builtContainer;
    }
    
    public abstract Task StartContainersForScenarioRun(String scenarioName);

    public abstract Task StopContainersForScenarioRun();

    protected virtual EventStoreClientSettings ConfigureEventStoreSettings() {
        EventStoreClientSettings settings = new EventStoreClientSettings();
        settings.ConnectivitySettings = EventStoreClientConnectivitySettings.Default;

        String connectionString = $"esdb://admin:changeit@127.0.0.1:{this.EventStoreHttpPort}";

        if (this.IsSecureEventStore) {
            connectionString = $"{connectionString}?tls=true&tlsVerifyCert=false";
            settings.ConnectivitySettings.Insecure = false;
            settings.DefaultCredentials = new UserCredentials("admin", "changeit");
        }
        else {
            connectionString = $"{connectionString}?tls=false&tlsVerifyCert=false";
            settings.ConnectivitySettings.Insecure = true;
        }

        settings.ConnectivitySettings.Address = new Uri(connectionString);

        return settings;
    }

    protected virtual async Task CreatePersistentSubscription((String streamName, String groupName, Int32 maxRetryCount) subscription) {
        EventStorePersistentSubscriptionsClient client = new EventStorePersistentSubscriptionsClient(this.ConfigureEventStoreSettings());

        PersistentSubscriptionSettings settings = new PersistentSubscriptionSettings(resolveLinkTos:true, StreamPosition.Start, maxRetryCount:subscription.maxRetryCount);
        this.Trace($"Creating persistent subscription Group [{subscription.groupName}] Stream [{subscription.streamName}] Retry Count [{subscription.maxRetryCount}]");
        await client.CreateAsync(subscription.streamName, subscription.groupName, settings);

        this.Trace($"Subscription Group [{subscription.groupName}] Stream [{subscription.streamName}] created");
    }

    protected async Task DoHealthCheck(ContainerType containerType) {
        (String, Int32) containerDetails = containerType switch {
            ContainerType.CallbackHandler => ("http", this.CallbackHandlerPort),
            ContainerType.EstateManagement => ("http", this.EstateManagementPort),
            ContainerType.FileProcessor => ("http", this.FileProcessorPort),
            ContainerType.MessagingService => ("http", this.MessagingServicePort),
            ContainerType.TestHost => ("http", this.TestHostServicePort),
            ContainerType.TransactionProcessor => ("http", this.TransactionProcessorPort),
            ContainerType.SecurityService => ("https", this.SecurityServicePort),
            ContainerType.VoucherManagementAcl => ("http", this.VoucherManagementAclPort),
            ContainerType.TransactionProcessorAcl => ("http", this.TransactionProcessorAclPort),
            _ => (null, 0)
        };

        if (containerDetails.Item1 == null)
            return;

        await Retry.For(async () => {
                            this.Trace($"About to do health check for {containerType}");

                            String healthCheck =
                                await this.HealthCheckClient.PerformHealthCheck(containerDetails.Item1, "127.0.0.1", containerDetails.Item2, CancellationToken.None);

                            HealthCheckResult result = JsonConvert.DeserializeObject<HealthCheckResult>(healthCheck);

                            this.Trace($"health check complete for {containerType} result is [{healthCheck}]");

                            result.Status.ShouldBe(HealthCheckStatus.Healthy.ToString(), $"Service Type: {containerType} Details {healthCheck}");
                            this.Trace($"health check complete for {containerType}");
        }, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(20));
    }

    protected virtual String GenerateEventStoreConnectionString() {
        String eventStoreAddress = $"esdb://admin:changeit@{this.EventStoreContainerName}:{DockerPorts.EventStoreHttpDockerPort}?tls=false";

        return eventStoreAddress;
    }

    protected virtual async Task LoadEventStoreProjections() {
        //Start our Continuous Projections - we might decide to do this at a different stage, but now lets try here
        String projectionsFolder = "projections/continuous";
        IPAddress[] ipAddresses = Dns.GetHostAddresses("127.0.0.1");

        if (!String.IsNullOrWhiteSpace(projectionsFolder)) {
            DirectoryInfo di = new DirectoryInfo(projectionsFolder);

            if (di.Exists) {
                FileInfo[] files = di.GetFiles();

                EventStoreProjectionManagementClient projectionClient = new EventStoreProjectionManagementClient(this.ConfigureEventStoreSettings());
                List<String> projectionNames = new List<String>();

                foreach (FileInfo file in files) {
                    String projection = await BaseDockerHelper.RemoveProjectionTestSetup(file);
                    String projectionName = file.Name.Replace(".js", String.Empty);

                    Should.NotThrow(async () => {
                                        this.Trace($"Creating projection [{projectionName}] from file [{file.FullName}]");
                                        await projectionClient.CreateContinuousAsync(projectionName, projection, trackEmittedStreams:true).ConfigureAwait(false);

                                        projectionNames.Add(projectionName);
                                        this.Trace($"Projection [{projectionName}] created");
                                    },
                                    $"Projection [{projectionName}] error");
                }

                // Now check the create status of each
                foreach (String projectionName in projectionNames) {
                    Should.NotThrow(async () => {
                                        ProjectionDetails projectionDetails = await projectionClient.GetStatusAsync(projectionName);

                                        projectionDetails.Status.ShouldBe("Running", $"Projection [{projectionName}] is {projectionDetails.Status}");

                                        this.Trace($"Projection [{projectionName}] running");
                                    },
                                    "Error getting Projection [{projectionName}] status");
                }
            }
        }

        this.Trace("Loaded projections");
    }

    protected void Trace(String traceMessage) {
        if (this.Logger.IsInitialised) {
            this.Logger.LogInformation($"{this.TestId}|{traceMessage}");
        }
    }

    private static async Task<String> RemoveProjectionTestSetup(FileInfo file) {
        // Read the file
        String[] projectionLines = await File.ReadAllLinesAsync(file.FullName);

        // Find the end of the test setup code
        Int32 index = Array.IndexOf(projectionLines, "//endtestsetup");
        List<String> projectionLinesList = projectionLines.ToList();

        // Remove the test setup code
        projectionLinesList.RemoveRange(0, index + 1);
        // Rebuild the string from the lines
        String projection = String.Join(Environment.NewLine, projectionLinesList);

        return projection;
    }

    #endregion
}