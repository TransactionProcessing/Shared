namespace Shared.IntegrationTesting;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
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
using Ductus.FluentDocker.Executors;
using Ductus.FluentDocker.Model.Builders;
using Ductus.FluentDocker.Model.Containers;
using Ductus.FluentDocker.Model.Networks;
using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Services.Extensions;
using EventStore.Client;
using HealthChecks;
using Logger;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using Shouldly;

public enum DockerEnginePlatform{
    Unknown, 
    Linux,
    Windows
}

public abstract class BaseDockerHelper{
    #region Fields

    public Dictionary<ContainerType, List<String>> AdditionalVariables = new Dictionary<ContainerType, List<String>>();

    public (String URL, String UserName, String Password)? DockerCredentials;

    public ILogger Logger;

    public (String usename, String password)? SqlCredentials;

    public IContainerService SqlServerContainer;

    public String SqlServerContainerName;

    public INetworkService SqlServerNetwork;

    public Guid TestId;
    
    public String ScenarioName;

    protected String CallbackHandlerContainerName;

    protected Int32 CallbackHandlerPort;

    protected (String clientId, String clientSecret) ClientDetails;

    protected List<(DockerServices, IContainerService)> Containers;

    protected String EventStoreContainerName;

    protected Int32 EventStoreHttpPort;
    protected Int32 EventStoreSecureHttpPort;

    protected String FileProcessorContainerName;

    protected Int32 FileProcessorPort;

    protected readonly IHealthCheckClient HealthCheckClient;

    protected Dictionary<ContainerType, Int32> HostPorts = new Dictionary<ContainerType, Int32>();

    protected String HostTraceFolder;

    protected Dictionary<ContainerType, (String imageName, Boolean useLatest)> ImageDetails = new();

    protected String MessagingServiceContainerName;

    protected Int32 MessagingServicePort;

    protected (Int32 pollingInterval, Int32 cacheDuration) PersistentSubscriptionSettings = (10, 0);

    public DockerServices RequiredDockerServices;

    protected String SecurityServiceContainerName;

    protected Int32 SecurityServicePort;

    protected String TestHostContainerName;

    protected Int32 TestHostServicePort;

    protected List<INetworkService> TestNetworks;

    protected String TransactionProcessorAclContainerName;

    protected Int32 TransactionProcessorAclPort;

    protected String TransactionProcessorContainerName;

    protected Int32 TransactionProcessorPort;

    protected Boolean UseSecureSqlServerDatabase;

    private String sqlTestConnString;

    #endregion

    #region Constructors

    public BaseDockerHelper(){
        this.Containers = new ();
        this.TestNetworks = new List<INetworkService>();
        this.HealthCheckClient = new HealthCheckClient(new HttpClient(new SocketsHttpHandler{
                                                                                                SslOptions = new SslClientAuthenticationOptions{
                                                                                                                                                   RemoteCertificateValidationCallback = (sender,
                                                                                                                                                                                          certificate,
                                                                                                                                                                                          chain,
                                                                                                                                                                                          errors) => true
                                                                                                                                               }
                                                                                            }));

        // Setup the default image details
        DockerEnginePlatform engineType = BaseDockerHelper.GetDockerEnginePlatform();
        if (engineType == DockerEnginePlatform.Windows){
            this.ImageDetails.Add(ContainerType.SqlServer, ("iamrjindal/sqlserverexpress:2019", true));
            this.ImageDetails.Add(ContainerType.EventStore, ("stuartferguson/eventstore_windows", true));
            this.ImageDetails.Add(ContainerType.MessagingService, ("stuartferguson/messagingservicewindows:master", true));
            this.ImageDetails.Add(ContainerType.SecurityService, ("stuartferguson/securityservicewindows:master", true));
            this.ImageDetails.Add(ContainerType.CallbackHandler, ("stuartferguson/callbackhandlerwindows:master", true));
            this.ImageDetails.Add(ContainerType.TestHost, ("stuartferguson/testhostswindows:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessor, ("stuartferguson/transactionprocessorwindows:master", true));
            this.ImageDetails.Add(ContainerType.FileProcessor, ("stuartferguson/fileprocessorwindows:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessorAcl, ("stuartferguson/transactionprocessoraclwindows:master", true));
        }
        else{
            if (FdOs.IsLinux()){
                this.ImageDetails.Add(ContainerType.SqlServer, ("mcr.microsoft.com/mssql/server:2019-latest", true));
            }
            else{
                this.ImageDetails.Add(ContainerType.SqlServer, ("mcr.microsoft.com/azure-sql-edge", true));
            }

            this.ImageDetails.Add(ContainerType.EventStore, ("eventstore/eventstore:24.10.0-jammy", true));
            this.ImageDetails.Add(ContainerType.MessagingService, ("stuartferguson/messagingservice:master", true));
            this.ImageDetails.Add(ContainerType.SecurityService, ("stuartferguson/securityservice:master", true));
            this.ImageDetails.Add(ContainerType.CallbackHandler, ("stuartferguson/callbackhandler:master", true));
            this.ImageDetails.Add(ContainerType.TestHost, ("stuartferguson/testhosts:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessor, ("stuartferguson/transactionprocessor:master", true));
            this.ImageDetails.Add(ContainerType.FileProcessor, ("stuartferguson/fileprocessor:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessorAcl, ("stuartferguson/transactionprocessoracl:master", true));
        }

        this.HostPorts = new Dictionary<ContainerType, Int32>();
        Logging.Enabled();
        
    }

    #endregion

    #region Properties

    public Boolean IsSecureEventStore{ get; protected set; }

    protected String InsecureEventStoreEnvironmentVariable =>
        this.IsSecureEventStore switch{
            true => "EventStoreSettings:Insecure=False",
            _ => "EventStoreSettings:Insecure=True"
        };

    #endregion

    #region Methods

    public virtual List<String> GetAdditionalVariables(ContainerType containerType){
        List<String> result = new List<String>();

        var additional = this.AdditionalVariables.SingleOrDefault(a => a.Key == containerType).Value;
        if (additional != null){
            result.AddRange(additional);
        }

        result.Add("Logging:LogLevel:Microsoft=Information");
        result.Add("Logging:LogLevel:Default=Information");
        result.Add("Logging:EventLog:LogLevel:Default=None");

        return result;
    }

    public List<String> GetCommonEnvironmentVariables(){
        Int32 securityServicePort = this.GetSecurityServicePort();

        String healthCheckConnString = this.SetConnectionString("ConnectionStrings:HealthCheck", "master", this.UseSecureSqlServerDatabase);

        return new List<String>{
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
                                   healthCheckConnString
                               };
    }

    public static DockerEnginePlatform GetDockerEnginePlatform(){
        try{
            IHostService docker = BaseDockerHelper.GetDockerHost();

            if (docker.Host.IsLinuxEngine()){
                return DockerEnginePlatform.Linux;
            }

            if (docker.Host.IsWindowsEngine()){
                return DockerEnginePlatform.Windows;
            }

            return DockerEnginePlatform.Unknown;
        }
        catch(Exception e){
            throw new Exception("Unable to determine docker Engine Platform", e);
        }
    }

    public static IHostService GetDockerHost(){
        IList<IHostService> hosts = new Hosts().Discover();
        IHostService docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
        return docker;
    }

    public Int32? GetHostPort(ContainerType key){
        KeyValuePair<ContainerType, Int32> details = this.HostPorts.SingleOrDefault(c => c.Key == key);
        if (details.Equals(default(KeyValuePair<ContainerType, Int32>))){
            // No details found so return a null
            return null;
        }

        return details.Value;
    }

    public (String imageName, Boolean useLatest) GetImageDetails(ContainerType key){
        KeyValuePair<ContainerType, (String imageName, Boolean useLatest)> details = this.ImageDetails.SingleOrDefault(c => c.Key == key);
        if (details.Equals(default(KeyValuePair<ContainerType, (String, Boolean)>))){
            // No details found so throw an error
            throw new Exception($"No image details found for Container Type [{key}]");
        }

        return details.Value;
    }
    protected DockerEnginePlatform DockerPlatform;

    public void SetHostPort(ContainerType key, Int32 hostPort){
        KeyValuePair<ContainerType, Int32> details = this.HostPorts.SingleOrDefault(c => c.Key == key);
        if (details.Equals(default(KeyValuePair<ContainerType, (String, Boolean)>)) == false){
            // Found so we can overwrite
            this.HostPorts[key] = hostPort;
        }
        else{
            this.HostPorts.Add(key, hostPort);
        }
    }

    public void SetImageDetails(ContainerType key,
                                (String imageName, Boolean useLatest) newDetails){
        KeyValuePair<ContainerType, (String imageName, Boolean useLatest)> details = this.ImageDetails.SingleOrDefault(c => c.Key == key);
        if (details.Equals(default(KeyValuePair<ContainerType, (String, Boolean)>)) == false){
            // Found so we can overwrite
            this.ImageDetails[key] = newDetails;
        }
    }

    public virtual ContainerBuilder SetupCallbackHandlerContainer(){
        this.Trace("About to Start Callback Handler Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables();

        List<String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.CallbackHandler);

        if (additionalEnvironmentVariables != null){
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder callbackHandlerContainer = new Builder().UseContainer().WithName(this.CallbackHandlerContainerName)
                                                                 .WithEnvironment(environmentVariables.ToArray())
                                                                 .UseImageDetails(this.GetImageDetails(ContainerType.CallbackHandler))
                                                                 .ExposePort(DockerPorts.CallbackHandlerDockerPort)
                                                                 .MountHostFolder(this.DockerPlatform,this.HostTraceFolder)
                                                                 .SetDockerCredentials(this.DockerCredentials);

        return callbackHandlerContainer;
    }

    public virtual void SetupContainerNames(){
        // Setup the container names
        this.EventStoreContainerName = $"eventstore{this.TestId:N}";
        this.SecurityServiceContainerName = $"securityservice{this.TestId:N}";
        this.TestHostContainerName = $"testhosts{this.TestId:N}";
        this.CallbackHandlerContainerName = $"callbackhandler{this.TestId:N}";
        this.FileProcessorContainerName = $"fileprocessor{this.TestId:N}";
        this.MessagingServiceContainerName = $"messaging{this.TestId:N}";
        this.TransactionProcessorContainerName = $"transaction{this.TestId:N}";
        this.TransactionProcessorAclContainerName = $"transactionacl{this.TestId:N}";
    }
    
    public virtual ContainerBuilder SetupEventStoreContainer(){
        this.Trace("About to Start Event Store Container");

        List<String> environmentVariables = new(){
                                                     "EVENTSTORE_RUN_PROJECTIONS=all",
                                                     "EVENTSTORE_START_STANDARD_PROJECTIONS=true",
                                                     "EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=true",
                                                     //"EVENTSTORE_ENABLE_EXTERNAL_TCP=true",
                                                     "EVENTSTORE_PROJECTION_EXECUTION_TIMEOUT=5000"
                                                 };
        
        String certsPath = this.DockerPlatform switch
        {
            DockerEnginePlatform.Windows => "C:\\EventStoreCerts",
            _ => "/etc/eventstore/certs"
        };

        ContainerBuilder eventStoreContainerBuilder = new Builder().UseContainer().UseImageDetails(this.GetImageDetails(ContainerType.EventStore))
                                                                   .ExposePort(DockerPorts.EventStoreHttpDockerPort).ExposePort(DockerPorts.EventStoreTcpDockerPort)
                                                                   .WithName(this.EventStoreContainerName);

        if (this.IsSecureEventStore == false){
            environmentVariables.Add("EVENTSTORE_INSECURE=true");
        }
        else{
            // Copy these to the container
            String path = Path.Combine(Directory.GetCurrentDirectory(), "certs");

            eventStoreContainerBuilder = eventStoreContainerBuilder.Mount(path, certsPath, MountType.ReadWrite);

            // Certificates configuration
            environmentVariables.Add($"EVENTSTORE_CertificateFile={certsPath}/node1/node.crt");
            environmentVariables.Add($"EVENTSTORE_CertificatePrivateKeyFile={certsPath}/node1/node.key");
            environmentVariables.Add($"EVENTSTORE_TrustedRootCertificatesPath={certsPath}/ca");
            environmentVariables.Add("EVENTSTORE_INSECURE=false");
        }

        eventStoreContainerBuilder = eventStoreContainerBuilder.WithEnvironment(environmentVariables.ToArray());

        if (eventStoreContainerBuilder == null){
            this.Trace("eventStoreContainerBuilder is null");
        }

        return eventStoreContainerBuilder;
    }

    public virtual ContainerBuilder SetupFileProcessorContainer(){
        this.Trace("About to Start File Processor Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add($"urls=http://*:{DockerPorts.FileProcessorDockerPort}");
        environmentVariables.Add(this.SetConnectionString("ConnectionStrings:EstateReportingReadModel", "EstateReportingReadModel", this.UseSecureSqlServerDatabase));

        String ciEnvVar = Environment.GetEnvironmentVariable("CI");
        Boolean isCi = String.IsNullOrEmpty(ciEnvVar) == false && String.Compare(ciEnvVar, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0;

        if (FdOs.IsLinux()){
            // we are running in CI Linux
            environmentVariables.Add($"AppSettings:TemporaryFileLocation={"/home/runner/bulkfiles/temporary"}");

            environmentVariables.Add($"AppSettings:FileProfiles:0:ListeningDirectory={"/home/runner/bulkfiles/safaricom"}");
            environmentVariables.Add($"AppSettings:FileProfiles:1:ListeningDirectory={"/home/runner/bulkfiles/voucher"}");
        }
        else if (FdOs.IsOsx()){
            // we are running in CI Mac OS
            environmentVariables.Add($"AppSettings:TemporaryFileLocation={"/Users/runner/bulkfiles/temporary"}");

            environmentVariables.Add($"AppSettings:FileProfiles:0:ListeningDirectory={"/Users/runner/bulkfiles/safaricom"}");
            environmentVariables.Add($"AppSettings:FileProfiles:1:ListeningDirectory={"/Users/runner/bulkfiles/voucher"}");
        }
        else{
            // We know this is now windows
            if (isCi){
                Directory.CreateDirectory("C:\\Users\\runneradmin\\txnproc\\bulkfiles\\temporary");
                Directory.CreateDirectory("C:\\Users\\runneradmin\\txnproc\\bulkfiles\\safaricom");
                Directory.CreateDirectory("C:\\Users\\runneradmin\\txnproc\\bulkfiles\\voucher");

                environmentVariables.Add("AppSettings:TemporaryFileLocation=\"C:\\Users\\runneradmin\\txnproc\\bulkfiles\\temporary\"");
                environmentVariables.Add("AppSettings:FileProfiles:0:ListeningDirectory=\"C:\\Users\\runneradmin\\txnproc\\bulkfiles\\safaricom\"");
                environmentVariables.Add("AppSettings:FileProfiles:1:ListeningDirectory=\"C:\\Users\\runneradmin\\txnproc\\bulkfiles\\voucher\"");
            }
            else{
                environmentVariables.Add("AppSettings:TemporaryFileLocation=\"C:\\home\\txnproc\\bulkfiles\\temporary\"");
                environmentVariables.Add("AppSettings:FileProfiles:0:ListeningDirectory=\"C:\\Users\\txnproc\\bulkfiles\\safaricom\"");
                environmentVariables.Add("AppSettings:FileProfiles:1:ListeningDirectory=\"C:\\Users\\txnproc\\bulkfiles\\voucher\"");
            }
        }

        List<String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.FileProcessor);

        if (additionalEnvironmentVariables != null){
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder fileProcessorContainer = new Builder().UseContainer().WithName(this.FileProcessorContainerName)
                                                               .WithEnvironment(environmentVariables.ToArray())
                                                               .UseImageDetails(this.GetImageDetails(ContainerType.FileProcessor))
                                                               .ExposePort(DockerPorts.FileProcessorDockerPort).MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
                                                               .SetDockerCredentials(this.DockerCredentials);

        // Mount the folder to upload files
        String uploadFolder = (this.DockerPlatform, isCi) switch{
            (DockerEnginePlatform.Windows, false) => "C:\\home\\txnproc\\reqnroll",
            (DockerEnginePlatform.Windows, true) => "C:\\Users\\runneradmin\\txnproc\\reqnroll",
            _ => "/home/txnproc/reqnroll"
        };

        if (this.DockerPlatform == DockerEnginePlatform.Windows && isCi){
            Directory.CreateDirectory(uploadFolder);
        }

        String containerFolder = this.DockerPlatform == DockerEnginePlatform.Windows ? "C:\\home\\txnproc\\bulkfiles" : "/home/txnproc/bulkfiles";
        fileProcessorContainer.Mount(uploadFolder, containerFolder, MountType.ReadWrite);
        return fileProcessorContainer;
    }

    public virtual ContainerBuilder SetupMessagingServiceContainer(){
        this.Trace("About to Start Messaging Service Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add($"urls=http://*:{DockerPorts.MessagingServiceDockerPort}");
        environmentVariables.Add("AppSettings:EmailProxy=Integration");
        environmentVariables.Add("AppSettings:SMSProxy=Integration");
        environmentVariables.Add("AppSettings:InternalSubscriptionService=false");

        List<String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.MessagingService);

        if (additionalEnvironmentVariables != null){
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder messagingServiceContainer = new Builder().UseContainer().WithName(this.MessagingServiceContainerName)
                                                                  .WithEnvironment(environmentVariables.ToArray())
                                                                  .UseImageDetails(this.GetImageDetails(ContainerType.MessagingService))
                                                                  .ExposePort(DockerPorts.MessagingServiceDockerPort)
                                                                  .MountHostFolder(this.DockerPlatform, this.HostTraceFolder).SetDockerCredentials(this.DockerCredentials);

        return messagingServiceContainer;
    }

    public virtual ContainerBuilder SetupSecurityServiceContainer(){
        this.Trace("About to Start Security Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add($"ServiceOptions:PublicOrigin=https://{this.SecurityServiceContainerName}:{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add($"ServiceOptions:IssuerUrl=https://{this.SecurityServiceContainerName}:{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add("ASPNETCORE_ENVIRONMENT=IntegrationTest");
        environmentVariables.Add($"urls=https://*:{DockerPorts.SecurityServiceDockerPort}");

        environmentVariables.Add("ServiceOptions:PasswordOptions:RequiredLength=6");
        environmentVariables.Add("ServiceOptions:PasswordOptions:RequireDigit=false");
        environmentVariables.Add("ServiceOptions:PasswordOptions:RequireUpperCase=false");
        environmentVariables.Add("ServiceOptions:UserOptions:RequireUniqueEmail=false");
        environmentVariables.Add("ServiceOptions:SignInOptions:RequireConfirmedEmail=false");

        List<String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.SecurityService);

        if (additionalEnvironmentVariables != null){
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder securityServiceContainer = new Builder().UseContainer().WithName(this.SecurityServiceContainerName)
                                                                 .WithEnvironment(environmentVariables.ToArray())
                                                                 .UseImageDetails(this.GetImageDetails(ContainerType.SecurityService))
                                                                 .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
                                                                 .SetDockerCredentials(this.DockerCredentials);

        Int32? hostPort = this.GetHostPort(ContainerType.SecurityService);
        if (hostPort == null){
            securityServiceContainer = securityServiceContainer.ExposePort(DockerPorts.SecurityServiceDockerPort);
        }
        else{
            securityServiceContainer = securityServiceContainer.ExposePort(hostPort.Value, DockerPorts.SecurityServiceDockerPort);
        }

        // Now build and return the container                
        return securityServiceContainer;
    }

    public virtual ContainerBuilder ConfigureSqlContainer()
    {
        this.Trace("About to start SQL Server Container");
        ContainerBuilder containerService = new Builder().UseContainer().WithName(this.SqlServerContainerName)
                                                         .UseImageDetails(this.GetImageDetails(ContainerType.SqlServer))
                                                         .WithEnvironment("ACCEPT_EULA=Y", $"SA_PASSWORD={this.SqlCredentials.Value.password}")
                                                         .ExposePort(1433)
                                                         .KeepContainer().KeepRunning().ReuseIfExists()
                                                         .SetDockerCredentials(this.DockerCredentials);
        
        return containerService;
    }

    public virtual async Task<IContainerService> SetupSqlServerContainer(INetworkService networkService){
        if (this.SqlCredentials == default)
            throw new Exception("Sql Credentials have not been set");

        IContainerService databaseServerContainer = await this.StartContainer2(this.ConfigureSqlContainer,
                                                                               new List<INetworkService>{
                                                                                                            networkService
                                                                                                        },
                                                                               DockerServices.SqlServer);
        
        return databaseServerContainer;
    }

    public virtual ContainerBuilder SetupTestHostContainer(){
        this.Trace("About to Start Test Hosts Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add(this.SetConnectionString("ConnectionStrings:TestBankReadModel", "TestBankReadModel", this.UseSecureSqlServerDatabase));
        environmentVariables.Add(this.SetConnectionString("ConnectionStrings:PataPawaReadModel", "PataPawaReadModel", this.UseSecureSqlServerDatabase));
        environmentVariables.Add("ASPNETCORE_ENVIRONMENT=IntegrationTest");

        List<String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.TestHost);

        if (additionalEnvironmentVariables != null){
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.TestHost);
        ContainerBuilder testHostContainer = new Builder().UseContainer().WithName(this.TestHostContainerName).WithEnvironment(environmentVariables.ToArray())
                                                          .UseImageDetails(this.GetImageDetails(ContainerType.TestHost)).ExposePort(DockerPorts.TestHostPort)
                                                          .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
                                                          .SetDockerCredentials(this.DockerCredentials);
        
        return testHostContainer;
    }

    public virtual INetworkService SetupTestNetwork(String networkName = null,
                                                    Boolean reuseIfExists = false){
        networkName = String.IsNullOrEmpty(networkName) ? $"testnw{this.TestId:N}" : networkName;
        DockerEnginePlatform engineType = BaseDockerHelper.GetDockerEnginePlatform();

        if (engineType == DockerEnginePlatform.Windows){
            var docker = BaseDockerHelper.GetDockerHost();
            var network = docker.GetNetworks().Where(nw => nw.Name == networkName).SingleOrDefault();
            if (network == null){
                Dictionary<String, String> driverOptions = new Dictionary<String, String>();
                driverOptions.Add("com.docker.network.windowsshim.networkname", networkName);

                network = docker.CreateNetwork(networkName,
                                               new NetworkCreateParams{
                                                                          Driver = "nat",
                                                                          DriverOptions = driverOptions,
                                                                          Attachable = true,
                                                                      });
            }

            return network;
        }

        if (engineType == DockerEnginePlatform.Linux){
            // Build a network
            NetworkBuilder networkService = new Builder().UseNetwork(networkName).ReuseIfExist();

            return networkService.Build();
        }

        return null;
    }

    public virtual ContainerBuilder SetupTransactionProcessorAclContainer(){
        this.Trace("About to Start Transaction Processor ACL Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add($"urls=http://*:{DockerPorts.TransactionProcessorAclDockerPort}");

        List<String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.TransactionProcessorAcl);

        if (additionalEnvironmentVariables != null){
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder transactionProcessorACLContainer = new Builder().UseContainer().WithName(this.TransactionProcessorAclContainerName)
                                                                         .WithEnvironment(environmentVariables.ToArray())
                                                                         .UseImageDetails(this.GetImageDetails(ContainerType.TransactionProcessorAcl))
                                                                         .ExposePort(DockerPorts.TransactionProcessorAclDockerPort)
                                                                         .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
                                                                         .SetDockerCredentials(this.DockerCredentials);

        return transactionProcessorACLContainer;
    }

    public virtual ContainerBuilder SetupTransactionProcessorContainer(){
        this.Trace("About to Start Transaction Processor Container");

        List<String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add($"urls=http://*:{DockerPorts.TransactionProcessorDockerPort}");
        environmentVariables.Add("AppSettings:SubscriptionFilter=TransactionProcessor");
        environmentVariables.Add($"OperatorConfiguration:Safaricom:Url=http://{this.TestHostContainerName}:{DockerPorts.TestHostPort}/api/safaricom");
        environmentVariables
            .Add($"OperatorConfiguration:PataPawaPostPay:Url=http://{this.TestHostContainerName}:{DockerPorts.TestHostPort}/PataPawaPostPayService/basichttp");
        environmentVariables.Add(this.SetConnectionString("ConnectionStrings:TransactionProcessorReadModel", "TransactionProcessorReadModel", this.UseSecureSqlServerDatabase));
        environmentVariables.Add(this.SetConnectionString("ConnectionStrings:EstateReportingReadModel", "EstateReportingReadModel", this.UseSecureSqlServerDatabase));

        List<String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.FileProcessor);

        if (additionalEnvironmentVariables != null){
            environmentVariables.AddRange(additionalEnvironmentVariables);
        }

        ContainerBuilder transactionProcessorContainer = new Builder().UseContainer().WithName(this.TransactionProcessorContainerName)
                                                                      .WithEnvironment(environmentVariables.ToArray())
                                                                      .UseImageDetails(this.GetImageDetails(ContainerType.TransactionProcessor))
                                                                      .ExposePort(DockerPorts.TransactionProcessorDockerPort)
                                                                      .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
                                                                      .SetDockerCredentials(this.DockerCredentials);

        
        return transactionProcessorContainer;
    }

    public abstract Task StartContainersForScenarioRun(String scenarioName, DockerServices dockerServices);

    public abstract Task StopContainersForScenarioRun(DockerServices sharedDockerServices);

    protected void CheckSqlConnection(IContainerService databaseServerContainer){
        // Try opening a connection
        this.Trace("About to SQL Server Container is running");
        if (String.IsNullOrEmpty(this.sqlTestConnString)){
            IPEndPoint sqlServerEndpoint = databaseServerContainer.ToHostExposedEndpoint("1433/tcp");

            String server = "127.0.0.1";
            String database = "master";
            String user = this.SqlCredentials.Value.usename;
            String password = this.SqlCredentials.Value.password;
            String port = sqlServerEndpoint.Port.ToString();

            this.sqlTestConnString = $"server={server},{port};user id={user}; password={password}; database={database};Encrypt=False";
            this.Trace($"Connection String {this.sqlTestConnString}");
        }

        SqlConnection connection = new SqlConnection(this.sqlTestConnString);
        try{
            connection.Open();

            SqlCommand command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.Prepare();
            command.ExecuteNonQuery();

            this.Trace("Connection Opened");

            connection.Close();
            this.Trace("SQL Server Container Running");
        }
        catch(SqlException ex){
            if (connection.State == ConnectionState.Open){
                connection.Close();
            }
            throw;
        }
    }

    protected virtual EventStoreClientSettings ConfigureEventStoreSettings(){
        String connectionString = $"esdb://admin:changeit@127.0.0.1:{this.EventStoreHttpPort}";
        
        connectionString = this.IsSecureEventStore switch{
            true => $"{connectionString}?tls=true&tlsVerifyCert=false",
            _ => $"{connectionString}?tls=false&tlsVerifyCert=false"
        };
        
        return EventStoreClientSettings.Create(connectionString);
    }

    protected virtual async Task CreatePersistentSubscription((String streamName, String groupName, Int32 maxRetryCount) subscription){
        EventStorePersistentSubscriptionsClient client = new EventStorePersistentSubscriptionsClient(this.ConfigureEventStoreSettings());

        PersistentSubscriptionSettings settings = new PersistentSubscriptionSettings(resolveLinkTos:true, StreamPosition.Start, maxRetryCount:subscription.maxRetryCount);
        this.Trace($"Creating persistent subscription Group [{subscription.groupName}] Stream [{subscription.streamName}] Retry Count [{subscription.maxRetryCount}]");
        await client.CreateAsync(subscription.streamName, subscription.groupName, settings);

        this.Trace($"Subscription Group [{subscription.groupName}] Stream [{subscription.streamName}] created");
    }

    protected async Task DoSqlServerHealthCheck(IContainerService containerService){
        // Try opening a connection
        Int32 maxRetries = 10;
        Int32 counter = 1;

        while (counter <= maxRetries){
            try{
                this.Trace($"Connection attempt {counter}");
                CheckSqlConnection(containerService);
                break;
            }
            catch(SqlException ex){
                this.Logger.LogError(ex);
                await Task.Delay(30000);
            }
            finally{
                counter++;
            }
        }

        if (counter >= maxRetries)
        {
            // We have got to the end and still not opened the connection
            throw new Exception($"Database container not started in {maxRetries} retries");
        }
    }
    protected async Task DoEventStoreHealthCheck(){
        String scheme = this.IsSecureEventStore switch
        {
            true => "https",
            _ => "http"
        };
        this.Trace("About to do event store ping");
        await Retry.For(async () =>
        {
            String url = $"{scheme}://127.0.0.1:{this.EventStoreHttpPort}/ping";

            using (HttpClientHandler httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message,
                                                                               cert,
                                                                               chain,
                                                                               errors) =>
                {
                    return true;
                };
                using (HttpClient client = new HttpClient(httpClientHandler))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new BasicAuthenticationHeaderValue("admin", "changeit");

                    HttpResponseMessage pingResponse = await client.GetAsync(url).ConfigureAwait(false);
                    pingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
                }
            }
        },

                        TimeSpan.FromSeconds(300),
                        TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        this.Trace("About to do event store info");

        await Retry.For(async () =>
        {
            String url = $"{scheme}://127.0.0.1:{this.EventStoreHttpPort}/info";

            using (HttpClientHandler httpClientHandler = new HttpClientHandler())
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (message,
                                                                               cert,
                                                                               chain,
                                                                               errors) =>
                {
                    return true;
                };
                using (HttpClient client = new HttpClient(httpClientHandler))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new BasicAuthenticationHeaderValue("admin", "changeit");

                    HttpResponseMessage infoResponse = await client.GetAsync(url).ConfigureAwait(false);

                    infoResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
                    String infoData = await infoResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                    this.Trace(infoData);
                }
            }
        });
    }

    protected async Task DoHealthCheck(ContainerType containerType){
        (String, Int32) containerDetails = containerType switch{
            ContainerType.CallbackHandler => ("http", this.CallbackHandlerPort),
            ContainerType.FileProcessor => ("http", this.FileProcessorPort),
            ContainerType.MessagingService => ("http", this.MessagingServicePort),
            ContainerType.TestHost => ("http", this.TestHostServicePort),
            ContainerType.TransactionProcessor => ("http", this.TransactionProcessorPort),
            ContainerType.SecurityService => ("https", this.SecurityServicePort),
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
                        },
                        TimeSpan.FromMinutes(3),
                        TimeSpan.FromSeconds(20));
    }

    protected void Error(String message, Exception ex){
        if (this.Logger.IsInitialised){
            this.Logger.LogError($"{this.TestId}|{message}", ex);
        }
    }

    protected virtual String GenerateEventStoreConnectionString(){
        String eventStoreAddress = $"esdb://admin:changeit@{this.EventStoreContainerName}:{DockerPorts.EventStoreHttpDockerPort}?tls=false";

        return eventStoreAddress;
    }

    protected virtual Int32 GetSecurityServicePort(){
        return DockerPorts.SecurityServiceDockerPort;
    }

    protected virtual List<String> GetRequiredProjections(){
        List<String> requiredProjections = new List<String>();

        requiredProjections.Add("CallbackHandlerEnricher.js");
        requiredProjections.Add("EstateAggregator.js");
        requiredProjections.Add("MerchantAggregator.js");
        requiredProjections.Add("MerchantBalanceCalculator.js");
        requiredProjections.Add("MerchantBalanceProjection.js");

        return requiredProjections;
    }

    protected virtual async Task LoadEventStoreProjections(){
        //Start our Continuous Projections - we might decide to do this at a different stage, but now lets try here
        String projectionsFolder = "projections/continuous";
        IPAddress[] ipAddresses = Dns.GetHostAddresses("127.0.0.1");

        if (!String.IsNullOrWhiteSpace(projectionsFolder)){
            DirectoryInfo di = new DirectoryInfo(projectionsFolder);

            if (di.Exists){
                FileInfo[] files = di.GetFiles();
                var requiredProjections = this.GetRequiredProjections();
                EventStoreProjectionManagementClient projectionClient = new EventStoreProjectionManagementClient(this.ConfigureEventStoreSettings());
                List<String> projectionNames = new List<String>();
                
                foreach (FileInfo file in files){
                    if (requiredProjections.Contains(file.Name) == false)
                        continue;

                    String projection = await BaseDockerHelper.RemoveProjectionTestSetup(file);
                    String projectionName = file.Name.Replace(".js", String.Empty);

                    Should.NotThrow(async () => {
                                        this.Trace($"Creating projection [{projectionName}] from file [{file.FullName}]");
                                        try{
                                            await projectionClient.CreateContinuousAsync(projectionName, projection, trackEmittedStreams:true).ConfigureAwait(false);
                                        }
                                        catch(Exception ex){
                                        }

                                        projectionNames.Add(projectionName);
                                        this.Trace($"Projection [{projectionName}] created");
                                    },
                                    $"Projection [{projectionName}] error");
                }

                // Now check the create status of each
                foreach (String projectionName in projectionNames){
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

    protected virtual void SetAdditionalVariables(ContainerType containerType, List<String> variableList){
        this.AdditionalVariables.SingleOrDefault(a => a.Key == containerType).Value.AddRange(variableList);
    }

    protected virtual String SetConnectionString(String settingName,
                                                 String databaseName,
                                                 Boolean isSecure = false){
        String encryptValue = String.Empty;
        if (isSecure == false){
            encryptValue = ";Encrypt=False";
        }

        String connectionString =
            $"{settingName}=\"server={this.SqlServerContainerName},1433;user id={this.SqlCredentials.Value.usename};password={this.SqlCredentials.Value.password};database={databaseName}{encryptValue}\"";
        
        return connectionString;
    }

    protected async Task<IContainerService> StartContainer2(Func<ContainerBuilder> buildContainerFunc, List<INetworkService> networkServices, DockerServices dockerService){
        if ((this.RequiredDockerServices & dockerService) != dockerService)
        {
            return default;
        }

        ConsoleStream<String> consoleLogs = null;
        try{
            ContainerBuilder containerBuilder = buildContainerFunc();

            IContainerService builtContainer = containerBuilder.Build();
            
            consoleLogs = builtContainer.Logs(true);
            IContainerService startedContainer = builtContainer.Start();
            foreach (INetworkService networkService in networkServices)
            {
                networkService.Attach(startedContainer, false);
            }

            this.Trace($"{dockerService} Container Started");
            this.Containers.Add((dockerService, startedContainer));

            //  Do a health check here
            //this.MessagingServicePort = 
            ContainerType type = dockerService switch{
                DockerServices.CallbackHandler => ContainerType.CallbackHandler,
                DockerServices.MessagingService => ContainerType.MessagingService,
                DockerServices.SecurityService => ContainerType.SecurityService,
                DockerServices.FileProcessor => ContainerType.FileProcessor,
                DockerServices.TestHost => ContainerType.TestHost,
                DockerServices.TransactionProcessor => ContainerType.TransactionProcessor,
                DockerServices.TransactionProcessorAcl => ContainerType.TransactionProcessorAcl,
                DockerServices.EventStore=> ContainerType.EventStore,
                DockerServices.SqlServer => ContainerType.SqlServer,
                _ => ContainerType.NotSet
            };

            this.SetHostPortForService(type, startedContainer);

            switch(type){
                case ContainerType.EventStore:
                    await DoEventStoreHealthCheck();
                    break;
                case ContainerType.SqlServer:
                    await DoSqlServerHealthCheck(startedContainer);
                    break;
                default:
                    await this.DoHealthCheck(type);
                    break;
            }

            this.Trace($"Container [{buildContainerFunc.Method.Name}] started");

            return startedContainer;
        }
        catch (Exception ex){
            if (consoleLogs != null){
                while (consoleLogs.IsFinished == false){
                    String s = consoleLogs.TryRead(10000);
                    this.Trace(s);
                }
            }

            this.Error($"Error starting container [{buildContainerFunc.Method.Name}]", ex);
            throw;
        }
    }

    private void SetHostPortForService(ContainerType type, IContainerService startedContainer){
        switch(type){
            case ContainerType.EventStore:
                if (this.IsSecureEventStore) {
                    this.EventStoreSecureHttpPort = startedContainer.ToHostExposedEndpoint($"{DockerPorts.EventStoreHttpDockerPort}/tcp").Port;
                }
                this.EventStoreHttpPort = startedContainer.ToHostExposedEndpoint($"{DockerPorts.EventStoreHttpDockerPort}/tcp").Port;
                break;
            case ContainerType.MessagingService:
                this.MessagingServicePort = startedContainer.ToHostExposedEndpoint($"{DockerPorts.MessagingServiceDockerPort}/tcp").Port;
                break;
            case ContainerType.SecurityService:
                this.SecurityServicePort = startedContainer.ToHostExposedEndpoint($"{DockerPorts.SecurityServiceDockerPort}/tcp").Port;
                break;
            case ContainerType.CallbackHandler:
                this.CallbackHandlerPort = startedContainer.ToHostExposedEndpoint($"{DockerPorts.CallbackHandlerDockerPort}/tcp").Port;
                break;
            case ContainerType.TestHost:
                this.TestHostServicePort = startedContainer.ToHostExposedEndpoint($"{DockerPorts.TestHostPort}/tcp").Port;
                break;
            case ContainerType.TransactionProcessor:
                this.TransactionProcessorPort = startedContainer.ToHostExposedEndpoint($"{DockerPorts.TransactionProcessorDockerPort}/tcp").Port;
                break;
            case ContainerType.FileProcessor:
                this.FileProcessorPort = startedContainer.ToHostExposedEndpoint($"{DockerPorts.FileProcessorDockerPort}/tcp").Port;
                break;
            case ContainerType.TransactionProcessorAcl:
                this.TransactionProcessorAclPort = startedContainer.ToHostExposedEndpoint($"{DockerPorts.TransactionProcessorAclDockerPort}/tcp").Port;
                break;
            default:
                break;
        }
    }

    protected async Task<IContainerService> StartContainer(Func<List<INetworkService>, Task<IContainerService>> startContainerFunc, List<INetworkService> networkServices, DockerServices dockerService){
        if ((this.RequiredDockerServices & dockerService) != dockerService){
            return default;
        }

        var d = BaseDockerHelper.GetDockerHost();
        

        try
        {
            return await startContainerFunc(networkServices);
        }
        catch(Exception ex){

            this.Error($"Error starting container [{startContainerFunc.Method.Name}]", ex);
            throw;
        }
    }

    public void Trace(String traceMessage){
        if (this.Logger.IsInitialised){
            this.Logger.LogInformation($"{this.TestId}|{this.ScenarioName}|{traceMessage}");
        }
    }

    private static async Task<String> RemoveProjectionTestSetup(FileInfo file){
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
