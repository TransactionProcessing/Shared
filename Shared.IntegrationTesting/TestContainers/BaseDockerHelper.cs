using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using SimpleResults;
using System.Net.Http.Headers;
using Shared.Serialisation;

namespace Shared.IntegrationTesting.TestContainers;

using Docker.DotNet;
using EventStore.Client;
using HealthChecks;
using KurrentDB.Client;
using Logger;
using Microsoft.Data.SqlClient;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public abstract class BaseDockerHelper{

    public Boolean SkipHealthChecks;

    public Dictionary<ContainerType, Dictionary<String,String>> AdditionalVariables = new();

    public (String URL, String UserName, String Password)? DockerCredentials;

    public ILogger Logger;

    public (String usename, String password) SqlCredentials = ("sa", "thisisalongpassword123!");
    
    public String SqlServerContainerName;

    public Guid TestId;
    
    public String ScenarioName;

    protected String CallbackHandlerContainerName;
    protected String EstateReportingContainerName;

    protected String SubscriptionServiceContainerName;

    protected Int32 CallbackHandlerPort;
    protected Int32 EstateReportingPort;

    protected Int32 SubscriptionServicePort;

    protected (String clientId, String clientSecret) ClientDetails;

    protected List<(DockerServices, IContainer)> Containers;

    protected readonly Object ContainerStateLock = new();

    protected String KurrentDbContainerName;

    public String ConfigHostContainerName;
    public Int32 ConfigHostPort;
    protected Int32 KurrentDbHttpPort;
    protected Int32 KurrentDbSecureHttpPort;

    protected String FileProcessorContainerName;

    protected Int32 FileProcessorPort;

    protected readonly IHealthCheckClient HealthCheckClient;

    protected static readonly HttpClient KurrentDbHealthCheckClient = CreateKurrentDbHealthCheckClient();

    protected Dictionary<ContainerType, Int32> HostPorts = new();

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

    protected List<INetwork> TestNetworks;

    protected String TransactionProcessorAclContainerName;
    
    protected Int32 TransactionProcessorAclPort;

    protected String TransactionProcessorContainerName;

    protected Int32 TransactionProcessorPort;

    protected Boolean UseSecureSqlServerDatabase;

    protected SqlServerTestRunFixture SharedSqlServerFixture;

    private String sqlTestConnString;



    protected BaseDockerHelper(Boolean skipHealthChecks=false) {
        this.SkipHealthChecks = skipHealthChecks;
        this.Containers = new ();
        this.TestNetworks = new List<INetwork>();
        this.HealthCheckClient = new HealthCheckClient(new HttpClient(new SocketsHttpHandler{
                                                                                                SslOptions = new SslClientAuthenticationOptions{
                                                                                                                                                   RemoteCertificateValidationCallback = (sender,
                                                                                                                                                                                          certificate,
                                                                                                                                                                                          chain,
                                                                                                                                                                                          errors) => true
                                                                                                                                               }
                                                                                            }));
        
        this.ImageDetails.Add(ContainerType.SqlServer, ("mcr.microsoft.com/mssql/server:2025-latest", true));
        this.ImageDetails.Add(ContainerType.KurrentDb, ("kurrentplatform/kurrentdb:26.2.2", true));
        this.ImageDetails.Add(ContainerType.MessagingService, ("stuartferguson/messagingservice:master", true));
        this.ImageDetails.Add(ContainerType.SecurityService, ("stuartferguson/securityservice:master", true));
        this.ImageDetails.Add(ContainerType.CallbackHandler, ("stuartferguson/callbackhandler:master", true));
        this.ImageDetails.Add(ContainerType.TestHost, ("stuartferguson/testhosts:master", true));
        this.ImageDetails.Add(ContainerType.TransactionProcessor, ("stuartferguson/transactionprocessor:master", true));
        this.ImageDetails.Add(ContainerType.FileProcessor, ("stuartferguson/fileprocessor:master", true));
        this.ImageDetails.Add(ContainerType.TransactionProcessorAcl, ("stuartferguson/transactionprocessoracl:master", true));
        this.ImageDetails.Add(ContainerType.ConfigurationHost, ("stuartferguson/mobileconfiguration:master", true));
        this.ImageDetails.Add(ContainerType.EstateManagementUI, ("stuartferguson/estatemanagementui:master", true));
        this.ImageDetails.Add(ContainerType.EstateReporting, ("stuartferguson/estatereportingapi:master", true));
        this.ImageDetails.Add(ContainerType.SubscriptionService, ("stuartferguson/subscriptionservice:master", true));

        this.HostPorts = new Dictionary<ContainerType, Int32>();
    }

    public Boolean IsSecureKurrentDb { get; protected set; }

    public virtual Dictionary<String,String> GetAdditionalVariables(ContainerType containerType){
        Dictionary<String, String> result = new();

        Dictionary<String, String>? additional = this.AdditionalVariables.SingleOrDefault(a => a.Key == containerType).Value;
        if (additional != null){
            foreach (KeyValuePair<String, String> item in additional) {
                result.Add(item.Key, item.Value);
            }
        }

        result.Add("Logging:LogLevel:Microsoft","Information");
        result.Add("Logging:LogLevel:Default","Information");
        result.Add("Logging:EventLog:LogLevel","Default=None");

        return result;
    }

    public virtual Dictionary<String, String> GetCommonEnvironmentVariables(){
        Int32 securityServicePort = this.GetSecurityServicePort();

        return new Dictionary<String, String>() {
            {"EventStoreSettings:ConnectionString", this.GenerateKurrentDbConnectionString()},
            {"AppSettings:PersistentSubscriptionPollingInSeconds", this.PersistentSubscriptionSettings.pollingInterval.ToString()},
            {"AppSettings:InternalSubscriptionServiceCacheDuration", this.PersistentSubscriptionSettings.cacheDuration.ToString()},
            {"AppSettings:SubscriptionConfiguration:PersistentSubscriptionPollingInSeconds", this.PersistentSubscriptionSettings.pollingInterval.ToString()},
            {"AppSettings:SubscriptionConfiguration:InternalSubscriptionServiceCacheDuration", this.PersistentSubscriptionSettings.cacheDuration.ToString()},
            {"AppSettings:SecurityService", $"https://{this.SecurityServiceContainerName}:{securityServicePort}"},
            {"SecurityConfiguration:Authority", $"https://{this.SecurityServiceContainerName}:{securityServicePort}"},
            {"AppSettings:ClientId", this.ClientDetails.clientId},
            {"AppSettings:ClientSecret", this.ClientDetails.clientSecret},
            {"AppSettings:MessagingServiceApi", $"http://{this.MessagingServiceContainerName}:{DockerPorts.MessagingServiceDockerPort}"},
            {"AppSettings:TransactionProcessorApi", $"http://{this.TransactionProcessorContainerName}:{DockerPorts.TransactionProcessorDockerPort}"},
            {"AppSettings:FileProcessorApi", $"http://{this.FileProcessorContainerName}:{DockerPorts.FileProcessorDockerPort}"},
            {"ConnectionStrings:HealthCheck", this.SetConnectionString("master", this.UseSecureSqlServerDatabase)},
            {"EventStoreSettings:Insecure", this.IsSecureKurrentDb.ToString()}
            
        };
    }

    protected virtual IReadOnlyList<IReadOnlyList<DockerServices>> GetStartupGroups()
    {
        List<IReadOnlyList<DockerServices>> startupGroups = [
            [DockerServices.SqlServer],
            [DockerServices.KurrentDb],
            [DockerServices.SubscriptionService],
            [
                DockerServices.MessagingService,
                DockerServices.SecurityService,
                DockerServices.CallbackHandler,
                DockerServices.TestHost,
                DockerServices.TransactionProcessor,
                DockerServices.FileProcessor,
                DockerServices.TransactionProcessorAcl,
                DockerServices.ConfigurationHost,
                DockerServices.EstateReporting
            ],
            [
                DockerServices.EstateManagementUI,                
            ]
        ];

        return startupGroups;
    }

    public static DockerClient GetDockerHost() {
        Docker.DotNet.DockerClientBuilder b = new DockerClientBuilder();
        DockerClient docker = b.Build();
        return docker;
    }
    

    public Int32? GetHostPort(ContainerType key) =>
        key switch {
            ContainerType.CallbackHandler => this.CallbackHandlerPort,            
            ContainerType.ConfigurationHost => this.ConfigHostPort,
            ContainerType.FileProcessor => this.FileProcessorPort,
            ContainerType.MessagingService => this.MessagingServicePort,
            ContainerType.SecurityService => this.SecurityServicePort,
            ContainerType.TestHost => this.TestHostServicePort,
            ContainerType.TransactionProcessor => this.TransactionProcessorPort,
            ContainerType.TransactionProcessorAcl => this.TransactionProcessorAclPort,
            ContainerType.SqlServer => this.SqlServerPort,
            ContainerType.EstateManagementUI => this.EstateManagementUiPort,
            ContainerType.EstateReporting => this.EstateReportingPort,
            ContainerType.SubscriptionService => this.SubscriptionServicePort,
            _ when key == ContainerType.KurrentDb && this.IsSecureKurrentDb => this.KurrentDbSecureHttpPort,
            _ when key == ContainerType.KurrentDb && this.IsSecureKurrentDb == false => this.KurrentDbHttpPort,
            _ => null
    };

    public SimpleResults.Result<(String imageName, Boolean useLatest)> GetImageDetails(ContainerType key){
        KeyValuePair<ContainerType, (String imageName, Boolean useLatest)> details = this.ImageDetails.SingleOrDefault(c => c.Key == key);
        if (details.Equals(default(KeyValuePair<ContainerType, (String, Boolean)>))){
            // No details found so throw an error
            return Result.Failure($"No image details found for Container Type [{key}]");
        }

        return Result.Success(details.Value);
    }
    public void SetHostPort(ContainerType key, Int32 hostPort){
        KeyValuePair<ContainerType, Int32> details = this.HostPorts.SingleOrDefault(c => c.Key == key);
        if (!details.Equals(default(KeyValuePair<ContainerType, (String, Boolean)>))){
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
        if (!details.Equals(default(KeyValuePair<ContainerType, (String, Boolean)>))){
            // Found so we can overwrite
            this.ImageDetails[key] = newDetails;
        }
    }

    public virtual ContainerBuilder SetupCallbackHandlerContainer(){
        this.Trace("About to Start Callback Handler Container");

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();

        Dictionary<String, String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.CallbackHandler);

        foreach (KeyValuePair<String, String> additionalEnvironmentVariable in additionalEnvironmentVariables)
        {
            environmentVariables.Add(additionalEnvironmentVariable.Key, additionalEnvironmentVariable.Value);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.CallbackHandler).Data;

        ContainerBuilder callbackHandlerContainer = new ContainerBuilder()
            .WithName(this.CallbackHandlerContainerName)  // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder)
            .WithPortBinding(DockerPorts.CallbackHandlerDockerPort, true);

        return callbackHandlerContainer;
    }

    public virtual void SetupContainerNames(){
        // Setup the container names
        this.SqlServerContainerName= $"sqlserver{this.TestId:N}";
        this.KurrentDbContainerName = $"kurrentdb{this.TestId:N}";
        this.SecurityServiceContainerName = $"securityservice{this.TestId:N}";
        this.TestHostContainerName = $"testhosts{this.TestId:N}";
        this.CallbackHandlerContainerName = $"callbackhandler{this.TestId:N}";
        this.FileProcessorContainerName = $"fileprocessor{this.TestId:N}";
        this.MessagingServiceContainerName = $"messaging{this.TestId:N}";
        this.TransactionProcessorContainerName = $"transaction{this.TestId:N}";
        this.TransactionProcessorAclContainerName = $"transactionacl{this.TestId:N}";
        this.ConfigHostContainerName = $"mobileconfighost{this.TestId:N}";
        this.EstateManagementUiContainerName = $"estateadministrationui{this.TestId:N}";
        this.EstateReportingContainerName = $"estatereporting{this.TestId:N}";
        this.SubscriptionServiceContainerName= $"subscriptionservice{this.TestId:N}";
    }

    protected Int32 EstateManagementUiPort;
    protected Int32 SqlServerPort;

    public void UseSharedSqlServer(SqlServerTestRunFixture fixture)
    {
        this.SharedSqlServerFixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        this.SqlServerContainerName = fixture.ContainerName;
        this.SqlServerPort = fixture.MappedPort;
        this.SqlCredentials = (fixture.UserName, fixture.Password);
    }

    protected String EstateManagementUiContainerName;

    public virtual ContainerBuilder SetupKurrentDbContainer(){
        this.Trace("About to Start Kurrent Db Container");

        Dictionary<String, String> environmentVariables = new(){
            {"EVENTSTORE_RUN_PROJECTIONS","all"},
            {"EVENTSTORE_START_STANDARD_PROJECTIONS","true"},
            {"EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP","true"},
            {"EVENTSTORE_PROJECTION_EXECUTION_TIMEOUT","5000"}
        };
        ContainerBuilder kurrentDbContainer = new();
        
        if (!this.IsSecureKurrentDb){
            environmentVariables.Add("EVENTSTORE_INSECURE","true");
        }
        else{
            String certsPath = "/etc/kurrentdb/certs";

            // Copy these to the container
            String path = Path.Combine(Directory.GetCurrentDirectory(), "certs");

            kurrentDbContainer = kurrentDbContainer.MountHostFolder(path, certsPath);

            // Certificates configuration
            environmentVariables.Add("EVENTSTORE_CertificateFile",$"{certsPath}/node1/node.crt");
            environmentVariables.Add("EVENTSTORE_CertificatePrivateKeyFile",$"{certsPath}/node1/node.key");
            environmentVariables.Add("EVENTSTORE_TrustedRootCertificatesPath", $"{certsPath}/ca");
            environmentVariables.Add("EVENTSTORE_INSECURE","false");
        }

        String logfilePath = "/var/log/kurrentdb";
        kurrentDbContainer = kurrentDbContainer.MountHostFolder(this.HostTraceFolder, logfilePath);

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.KurrentDb).Data;
        this.Trace($"About to Start Kurrent Db Container using image [{imageDetails.imageName}]");

        kurrentDbContainer = kurrentDbContainer.WithName(this.KurrentDbContainerName)  // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .WithPortBinding(DockerPorts.KurrentDbHttpDockerPort, true);
        
        return kurrentDbContainer;
    }
    
    public virtual ContainerBuilder SetupFileProcessorContainer(){
        this.Trace("About to Start File Processor Container");

        String ciEnvVar = Environment.GetEnvironmentVariable("CI");
        Boolean isCi = !String.IsNullOrEmpty(ciEnvVar) && String.Compare(ciEnvVar, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0;

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add("urls",$"http://*:{DockerPorts.FileProcessorDockerPort}");
        environmentVariables.Add("ConnectionStrings:TransactionProcessorReadModel", this.SetConnectionString("TransactionProcessorReadModel", this.UseSecureSqlServerDatabase));

        String bulkFilesRoot = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (isCi ? @"C:\Users\runneradmin\txnproc\bulkfiles" : @"C:\home\txnproc\bulkfiles")
            : (isCi ? "/home/runner/bulkfiles" : "/home/txnproc/bulkfiles");

        environmentVariables.Add("AppSettings:TemporaryFileLocation",Path.Combine(bulkFilesRoot, "temporary"));
        environmentVariables.Add("AppSettings:FileProfiles:0:ListeningDirectory",Path.Combine(bulkFilesRoot, "safaricom"));
        environmentVariables.Add("AppSettings:FileProfiles:1:ListeningDirectory",Path.Combine(bulkFilesRoot, "voucher"));

        Dictionary<String, String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.FileProcessor);

        foreach (KeyValuePair<String, String> additionalEnvironmentVariable in additionalEnvironmentVariables)
        {
            environmentVariables.Add(additionalEnvironmentVariable.Key, additionalEnvironmentVariable.Value);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.FileProcessor).Data;

        ContainerBuilder fileProcessorContainer = new ContainerBuilder()
            .WithName(this.FileProcessorContainerName)  // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder)
            .WithPortBinding(DockerPorts.FileProcessorDockerPort, true);
        
        // Mount the folder to upload files
        String uploadFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (isCi ? @"C:\Users\runneradmin\txnproc\reqnroll" : @"C:\home\txnproc\reqnroll")
            : (isCi ? "/home/runner/reqnroll" : "/home/txnproc/reqnroll");

        String containerFolder = "/home/txnproc/bulkfiles";
        Directory.CreateDirectory(uploadFolder);
        fileProcessorContainer.MountHostFolder(uploadFolder, containerFolder);
        return fileProcessorContainer;
    }

    protected virtual ContainerBuilder SetupEstateManagementUiContainer()
    {
        Trace("About to Start Estate Management UI Container");

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();

        environmentVariables.Remove("AppSettings:ClientId");
        environmentVariables.Remove("AppSettings:ClientSecret");

        environmentVariables.Add("AppSettings:Authority", $"https://{this.SecurityServiceContainerName}:0");  // The port is set to 0 to stop defaulting to 443
        environmentVariables.Add("AppSettings:SecurityServiceLocalPort", $"{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add("AppSettings:SecurityServicePort", $"{this.SecurityServicePort}");
        environmentVariables.Add("AppSettings:HttpClientIgnoreCertificateErrors", $"true");
        //environmentVariables.Add($"AppSettings:PermissionsBypass=true");
        environmentVariables.Add("AppSettings:IsIntegrationTest", "true");
        environmentVariables.Add("ASPNETCORE_ENVIRONMENT", "Development");
        environmentVariables.Add("EstateManagementScope", "estateManagement");
        environmentVariables.Add("urls", "https://*:5004");
        environmentVariables.Add($"AppSettings:ClientId", "estateUIClient");
        environmentVariables.Add($"AppSettings:ClientSecret", "Secret1");
        environmentVariables.Add($"AppSettings:BackEndClientId", "serviceClient");
        environmentVariables.Add($"AppSettings:BackEndClientSecret", "Secret1");
        environmentVariables.Add($"DataReloadConfig:DefaultInSeconds", "1");
        environmentVariables.Add(
            "AppSettings:EstateReportingApi",
            $"http://{this.EstateReportingContainerName}:{DockerPorts.EstateReportingDockerPort}");
        environmentVariables.Add("ConnectionStrings:TransactionProcessorReadModel", this.SetConnectionString("TransactionProcessorReadModel", this.UseSecureSqlServerDatabase));

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.EstateManagementUI).Data;

        ContainerBuilder containerBuilder = new ContainerBuilder()
            .WithName(this.EstateManagementUiContainerName)
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder)
            .WithPortBinding(DockerPorts.EstateManagementUIDockerPort,true);

        return containerBuilder;
    }

    public virtual ContainerBuilder SetupSubscriptionServiceContainer() {
        this.Trace("About to Start Subscription Service Container");

        Dictionary<String, String> environmentVariables = new();
        environmentVariables.Add("ConnectionStrings:KurrentDb", this.GenerateKurrentDbConnectionString());
        environmentVariables.Add("ConnectionStrings:SubscriptionServiceDb", this.SetConnectionString("SubscriptionService", this.UseSecureSqlServerDatabase));
        
        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.SubscriptionService).Data;

        ContainerBuilder subscriptionServiceContainer = new ContainerBuilder().WithName(this.SubscriptionServiceContainerName)
                                                                     .WithImage(imageDetails.imageName)
                                                                     .WithEnvironment(environmentVariables)
                                                                     .MountHostFolder(this.HostTraceFolder)
                                                                     .WithPortBinding(DockerPorts.SubscriptionServiceDockerPort, true);

        return subscriptionServiceContainer;
    }

    public virtual ContainerBuilder SetupConfigHostContainer()
    {
        this.Trace("About to Start Config Host Container");
        Dictionary<String, String> environmentVariables = new();
        environmentVariables.Add("AppSettings:InMemoryDatabase", "true");
        environmentVariables.Add(
            "ConnectionStrings:ConfigurationDatabase",
            this.SetConnectionString("master", this.UseSecureSqlServerDatabase));

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.ConfigurationHost).Data;

        ContainerBuilder configHostContainer = new ContainerBuilder().WithName(this.ConfigHostContainerName)
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder)
            .WithPortBinding(DockerPorts.ConfigHostDockerPort, true);

        return configHostContainer;
    }

    public virtual ContainerBuilder SetupMessagingServiceContainer(){
        this.Trace("About to Start Messaging Service Container");

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add($"urls",$"http://*:{DockerPorts.MessagingServiceDockerPort}");
        environmentVariables.Add("AppSettings:EmailProxy","Integration");
        environmentVariables.Add("AppSettings:SMSProxy","Integration");
        environmentVariables.Add("AppSettings:InternalSubscriptionService","false");

        Dictionary<String, String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.MessagingService);

        foreach (KeyValuePair<String, String> additionalEnvironmentVariable in additionalEnvironmentVariables)
        {
            environmentVariables.Add(additionalEnvironmentVariable.Key, additionalEnvironmentVariable.Value);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.MessagingService).Data;

        ContainerBuilder messagingServiceContainer = new ContainerBuilder().WithName(this.MessagingServiceContainerName) // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder);

        messagingServiceContainer = messagingServiceContainer
            .WithPortBinding(DockerPorts.MessagingServiceDockerPort, true);
        
        return messagingServiceContainer;
    }

    protected IntegrationTestCertificate? IntegrationTestCertificate;

    public virtual ContainerBuilder SetupSecurityServiceContainer()
    {
        this.Trace("About to Start Security Container");

        if (this.IntegrationTestCertificate is null)
        {
            throw new InvalidOperationException("Integration test certificate was not initialised before container setup.");
        }

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add("ServiceOptions:PublicOrigin", $"https://{this.SecurityServiceContainerName}:{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add("ServiceOptions:IssuerUrl", $"https://{this.SecurityServiceContainerName}:{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add("ASPNETCORE_ENVIRONMENT", "IntegrationTest");
        environmentVariables.Add("urls", $"https://*:{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add("ServiceOptions:KestrelOptions:Path", this.IntegrationTestCertificate.GetContainerCertificatePath());
        environmentVariables.Add("ServiceOptions:KestrelOptions:Password", this.IntegrationTestCertificate.Password);
        environmentVariables.Add("ServiceOptions:PasswordOptions:RequiredLength", "6");
        environmentVariables.Add("ServiceOptions:PasswordOptions:RequireDigit", "false");
        environmentVariables.Add("ServiceOptions:PasswordOptions:RequireUpperCase", "false");
        environmentVariables.Add("ServiceOptions:UserOptions:RequireUniqueEmail", "false");
        environmentVariables.Add("ServiceOptions:SignInOptions:RequireConfirmedEmail", "false");
        environmentVariables.Add("ServiceOptions:ManagementBootstrap:Enabled", "true");
        environmentVariables.Add("ServiceOptions:ManagementBootstrap:ClientId", "management-bootstrap");
        environmentVariables.Add("ServiceOptions:ManagementBootstrap:ClientSecret", "management-bootstrap-secret");
        environmentVariables.Add("ServiceOptions:OAuth:LegacyGrantTypeClients:password:0", "merchantClient");
        environmentVariables.Add("ServiceOptions:OAuth:LegacyGrantTypeClients:hybrid:0", "testclient2");
        environmentVariables.Add("ServiceOptions:OAuth:EnableLegacyGrantTypes", "true");
        environmentVariables.Add("ConnectionStrings:PersistedGrantDbContext", this.SetConnectionString($"PersistedGrantStore-{this.TestId}", this.UseSecureSqlServerDatabase));
        environmentVariables.Add("ConnectionStrings:ConfigurationDbContext", this.SetConnectionString($"Configuration-{this.TestId}", this.UseSecureSqlServerDatabase));
        environmentVariables.Add("ConnectionStrings:AuthenticationDbContext", this.SetConnectionString($"Authentication-{this.TestId}", this.UseSecureSqlServerDatabase));

        Dictionary<String, String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.SecurityService);
        if (additionalEnvironmentVariables != null) {
            foreach (KeyValuePair<String, String> additionalEnvironmentVariable in additionalEnvironmentVariables) {
                environmentVariables.Add(additionalEnvironmentVariable.Key, additionalEnvironmentVariable.Value);
            }
        }

        SimpleResults.Result<(String imageName, Boolean useLatest)> imageDetailsResult = this.GetImageDetails(ContainerType.SecurityService);
        if (imageDetailsResult.IsFailed) {
            throw new Exception($"Image details not found for {ContainerType.SecurityService}");
        }

        ContainerBuilder securityServiceContainer = new ContainerBuilder(imageDetailsResult.Data.imageName)
            .WithName(this.SecurityServiceContainerName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder)
            .MountHostFolder(this.IntegrationTestCertificate.CertificateDirectory, this.IntegrationTestCertificate.ContainerCertificateDirectory);            

        Int32? hostPort = this.GetHostPort(ContainerType.SecurityService);
        if (hostPort is null or <= 0) {
            securityServiceContainer = securityServiceContainer.WithPortBinding(DockerPorts.SecurityServiceDockerPort, true);
        }
        else {
            securityServiceContainer = securityServiceContainer.WithPortBinding(DockerPorts.SecurityServiceDockerPort, hostPort.Value);
        }

        return securityServiceContainer;
    }


    public virtual ContainerBuilder ConfigureSqlContainer()
    {
        this.Trace("About to start SQL Server Container");
        
        ContainerBuilder containerService = new ContainerBuilder()
            .WithName(this.SqlServerContainerName)  // similar to WithName()
            .WithImage(this.GetImageDetails(ContainerType.SqlServer).Data.imageName)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SA_PASSWORD", this.SqlCredentials.password)
            .WithPortBinding(1433, true);

        return containerService;
    }

    public virtual ContainerBuilder SetupTestHostContainer(){
        this.Trace("About to Start Test Hosts Container");

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add("ConnectionStrings:TestBankReadModel", this.SetConnectionString("TestBankReadModel", this.UseSecureSqlServerDatabase));
        environmentVariables.Add("ConnectionStrings:PataPawaReadModel", this.SetConnectionString("PataPawaReadModel", this.UseSecureSqlServerDatabase));
        environmentVariables.Add("ASPNETCORE_ENVIRONMENT","IntegrationTest");

        Dictionary<String, String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.TestHost);

        foreach (KeyValuePair<String, String> additionalEnvironmentVariable in additionalEnvironmentVariables) {
            environmentVariables.Add(additionalEnvironmentVariable.Key, additionalEnvironmentVariable.Value);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.TestHost).Data;
        
        ContainerBuilder testHostContainer = new ContainerBuilder()
            .WithName(this.TestHostContainerName)  // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder)
            .WithPortBinding(DockerPorts.TestHostPort, true);
        
        return testHostContainer;
    }

    public virtual ContainerBuilder SetupEstateReportingContainer()
    {
        this.Trace("About to Start Estate Reporting Container");

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();

        Dictionary<String, String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.EstateReporting);

        foreach (KeyValuePair<String, String> additionalEnvironmentVariable in additionalEnvironmentVariables)
        {
            environmentVariables.Add(additionalEnvironmentVariable.Key, additionalEnvironmentVariable.Value);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.EstateReporting).Data;

        ContainerBuilder estateReportingContainer = new ContainerBuilder()
            .WithName(this.EstateReportingContainerName)  // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder)
            .WithPortBinding(DockerPorts.EstateReportingDockerPort, true);

        return estateReportingContainer;
    }

    public virtual async Task<INetwork> SetupTestNetwork(String networkName = null,
                                                    Boolean reuseIfExists = false){
        networkName = String.IsNullOrEmpty(networkName) ? $"testnw{this.TestId:N}" : networkName;
        NetworkBuilder networkService = new NetworkBuilder().WithName(networkName).WithReuse(reuseIfExists).WithLabel("reuse-id", networkName);
        
        return networkService.Build();
    }

    public virtual ContainerBuilder SetupTransactionProcessorAclContainer(){
        this.Trace("About to Start Transaction Processor ACL Container");

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add("urls",$"http://*:{DockerPorts.TransactionProcessorAclDockerPort}");

        Dictionary<String, String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.TransactionProcessorAcl);

        foreach (KeyValuePair<String, String> additionalEnvironmentVariable in additionalEnvironmentVariables) {
            environmentVariables.Add(additionalEnvironmentVariable.Key, additionalEnvironmentVariable.Value);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.TransactionProcessorAcl).Data;

        ContainerBuilder transactionProcessorACLContainer = new ContainerBuilder()
            .WithName(this.TransactionProcessorAclContainerName)  // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder)
            .WithPortBinding(DockerPorts.TransactionProcessorAclDockerPort, true);

        return transactionProcessorACLContainer;
    }

    public virtual ContainerBuilder SetupTransactionProcessorContainer(){
        this.Trace("About to Start Transaction Processor Container");

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add("urls",$"http://*:{DockerPorts.TransactionProcessorDockerPort}");
        environmentVariables.Add("AppSettings:SubscriptionFilter","TransactionProcessor");
        environmentVariables.Add("OperatorConfiguration:Safaricom:Url",$"http://{this.TestHostContainerName}:{DockerPorts.TestHostPort}/api/safaricom");
        environmentVariables
            .Add($"OperatorConfiguration:PataPawaPostPay:Url",$"http://{this.TestHostContainerName}:{DockerPorts.TestHostPort}/PataPawaPostPayService/basichttp");
        environmentVariables.Add($"OperatorConfiguration:PataPawaPrePay:Url",$"http://{this.TestHostContainerName}:{DockerPorts.TestHostPort}/api/patapawaprepay");
        environmentVariables.Add("ConnectionStrings:TransactionProcessorReadModel", this.SetConnectionString( "TransactionProcessorReadModel", this.UseSecureSqlServerDatabase));

        Dictionary<String, String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.TransactionProcessor);

        foreach (KeyValuePair<String, String> additionalEnvironmentVariable in additionalEnvironmentVariables) {
            environmentVariables.Add(additionalEnvironmentVariable.Key, additionalEnvironmentVariable.Value);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.TransactionProcessor).Data;

        ContainerBuilder transactionProcessorContainer = new ContainerBuilder()
            .WithName(this.TransactionProcessorContainerName)  // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.HostTraceFolder)
            .WithPortBinding(DockerPorts.TransactionProcessorDockerPort, true);

        // TODO: Extension for multiple networks
        //foreach (INetwork testNetwork in this.TestNetworks) {
        //    transactionProcessorContainer = transactionProcessorContainer.WithNetwork(testNetwork);
        //}

        return transactionProcessorContainer;
    }

    public abstract Task StartContainersForScenarioRun(String scenarioName,
                                                       DockerServices dockerServices,
                                                       CancellationToken cancellationToken);

    public abstract Task StopContainersForScenarioRun(DockerServices sharedDockerServices);

    protected void CheckSqlConnection(IContainer databaseServerContainer){
        // Try opening a connection
        this.Trace("About to SQL Server Container is running");
        if (String.IsNullOrEmpty(this.sqlTestConnString)) {
            UInt16 sqlServerEndpoint = databaseServerContainer.GetMappedPublicPort("1433");

            String server = "127.0.0.1";
            String database = "master";
            String user = this.SqlCredentials.usename;
            String password = this.SqlCredentials.password;
            String port = sqlServerEndpoint.ToString();

            this.sqlTestConnString = $"server={server},{port};user id={user}; password={password}; database={database};Encrypt=False";
            this.Trace($"Connection String {RedactConnectionString(this.sqlTestConnString)}");
        }

        SqlConnection connection = new(this.sqlTestConnString);
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
        catch(SqlException){
            if (connection.State == ConnectionState.Open){
                connection.Close();
            }
            throw;
        }
    }

    protected virtual KurrentDBClientSettings ConfigureKurrentDbSettings(String username = "admin", String password="changeit"){
        String connectionString = $"esdb://{username}:{password}@127.0.0.1:{this.KurrentDbHttpPort}";
        
        connectionString = this.IsSecureKurrentDb switch{
            true => $"{connectionString}?tls=true&tlsVerifyCert=false",
            _ => $"{connectionString}?tls=false&tlsVerifyCert=false"
        };
        
        return KurrentDBClientSettings.Create(connectionString);
    }

    protected virtual async Task CreatePersistentSubscription((String streamName, String groupName, Int32 maxRetryCount) subscription){
        KurrentDBPersistentSubscriptionsClient client = new(this.ConfigureKurrentDbSettings());
        
        PersistentSubscriptionSettings settings = new(resolveLinkTos:true, StreamPosition.Start, maxRetryCount:subscription.maxRetryCount);
        this.Trace($"Creating persistent subscription Group [{subscription.groupName}] Stream [{subscription.streamName}] Retry Count [{subscription.maxRetryCount}]");
        await client.CreateToStreamAsync(subscription.streamName, subscription.groupName, settings);

        this.Trace($"Subscription Group [{subscription.groupName}] Stream [{subscription.streamName}] created");
    }

    public ISubscriptionServiceClient SubscriptionServiceClient;

    protected virtual async Task<Result> CreateSubscription(EndpointDefinition endpointDefinition, String indexName, String tag, CancellationToken cancellationToken) {
        if (endpointDefinition == null) {
            return Result.Failure("Endpoint definition must not be null");
        }

        if (this.SubscriptionServiceClient == null) {
            return Result.Failure("SubscriptionServiceClient needs initialised");
        }

        // Check if the endpoint exists already
        SubscriptionApiResponse<IReadOnlyCollection<EndpointDefinition>> existingEndpoints = await this.SubscriptionServiceClient.GetEndpointsAsync(cancellationToken);
        if (existingEndpoints.IsSuccessStatusCode == false) {
            return Result.Failure($"Error getting existing endpoints Status Code = {existingEndpoints.StatusCode}");
        }


        if (existingEndpoints.Value.SingleOrDefault(s => s.Url == endpointDefinition.Url) == null) {
            // Need to create a new endpoint here
            SubscriptionApiResponse<EndpointDefinition> result = await this.SubscriptionServiceClient.CreateEndpointAsync(endpointDefinition, cancellationToken);
            if (result.IsSuccessStatusCode == false)
                return Result.Failure($"Error creating endpoint name {endpointDefinition.Name} Status Code = {result.StatusCode}");
        }

        String subscriptionId = $"{indexName}_{endpointDefinition.Name}_{tag}";

        SubscriptionApiResponse<IReadOnlyCollection<SubscriptionDefinition>> subscriptionConfigList = await this.SubscriptionServiceClient.GetSubscriptionConfigurationsAsync(cancellationToken);

        if (subscriptionConfigList.Value.SingleOrDefault(x => x.SubscriptionId == subscriptionId) == null) {
            SubscriptionConfigurationRequest subscriptionConfigurationRequest = new();
            subscriptionConfigurationRequest = subscriptionConfigurationRequest with {
                                                   ContinueOnParked = true,
                                                   CheckpointBatchSize = 100,
                                                   EndpointUrl = endpointDefinition.Url,
                                                   RetryDelaySeconds = 5,
                                                   RetryMaxAttempts = 0,
                                                   SecondaryIndexName = indexName,
                                                   SoftDeleteParked = true,
                                                   SubscriptionId = subscriptionId,
                                                   Tag = tag,
                                                   TimeoutSeconds = 30,
                                               };

            SubscriptionApiResponse<SubscriptionDefinition> result = await this.SubscriptionServiceClient.CreateSubscriptionConfigurationAsync(subscriptionConfigurationRequest, cancellationToken);
            if (result.IsSuccessStatusCode == false)
                return Result.Failure($"Error subscription configuration id {subscriptionConfigurationRequest.SubscriptionId} Status Code = {result.StatusCode}");
        }

        return Result.Success();
    }

    protected async Task DoSqlServerHealthCheck(
        IContainer containerService,
        CancellationToken cancellationToken){
        // Try opening a connection
        const int maxRetries = 10;
        bool connected = false;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                this.Trace($"Connection attempt {attempt}");

                CheckSqlConnection(containerService);
                connected = true;
                break;
            }
            catch (SqlException ex)
            {
                this.Logger.LogError(ex);

                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
        }

        if (!connected)
        {
            throw new ApplicationException(
                $"Database container not started after {maxRetries} attempts");
        }
    }
    protected async Task DoEventStoreHealthCheck(CancellationToken cancellationToken){
        String scheme = this.IsSecureKurrentDb switch
        {
            true => "https",
            _ => "http"
        };
        this.Trace("About to do event store ping");
        await Retry.For(async cancellationToken =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            String url = $"{scheme}://127.0.0.1:{this.KurrentDbHttpPort}/ping";

            using HttpResponseMessage pingResponse = await KurrentDbHealthCheckClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
            pingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        },
                        cancellationToken,
                        TimeSpan.FromSeconds(300),
                        TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        this.Trace("About to do event store info");

        await Retry.For(async cancellationToken =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            String url = $"{scheme}://127.0.0.1:{this.KurrentDbHttpPort}/info";

            using HttpResponseMessage infoResponse = await KurrentDbHealthCheckClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

            infoResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            String infoData = await infoResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            this.Trace(infoData);
        }, cancellationToken);
    }

    private static HttpClient CreateKurrentDbHealthCheckClient()
    {
        HttpClientHandler httpClientHandler = new()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        HttpClient client = new(httpClientHandler);
        String authenticationString = "admin:changeit";
        String base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
        return client;
    }

    protected async Task DoHealthCheck(
        ContainerType containerType,
        CancellationToken cancellationToken) {
        (String, Int32) containerDetails = containerType switch {
            ContainerType.CallbackHandler => ("http", this.CallbackHandlerPort),
            ContainerType.FileProcessor => ("http", this.FileProcessorPort),
            ContainerType.MessagingService => ("http", this.MessagingServicePort),
            ContainerType.TestHost => ("http", this.TestHostServicePort),
            ContainerType.TransactionProcessor => ("http", this.TransactionProcessorPort),
            ContainerType.SecurityService => ("https", this.SecurityServicePort),
            ContainerType.TransactionProcessorAcl => ("http", this.TransactionProcessorAclPort),
            ContainerType.ConfigurationHost => ("http", this.ConfigHostPort),
            ContainerType.EstateManagementUI => ("https", this.EstateManagementUiPort),
            ContainerType.EstateReporting => ("http", this.EstateReportingPort),
            _ => (null, 0)
        };

        if (containerDetails.Item1 == null)
            return;

        await Retry.For(async cancellationToken => {
            cancellationToken.ThrowIfCancellationRequested();
            this.Trace($"About to do health check for {containerType}");

            SimpleResults.Result<String> healthCheckResult = await this.HealthCheckClient.PerformHealthCheck(containerDetails.Item1, "127.0.0.1", containerDetails.Item2, cancellationToken);

            if (healthCheckResult.IsSuccess) {
                HealthChecks.HealthCheckResult result = StringSerialiser.Deserialise<HealthCheckResult>(healthCheckResult.Data, new SerialiserOptions(SerialiserPropertyFormat.CamelCase));

                this.Trace($"health check complete for {containerType} result is [{healthCheckResult.Data}]");

                result.Status.ShouldBe(nameof(HealthCheckStatus.Healthy), $"Service Type: {containerType} Details {healthCheckResult.Data}");
                this.Trace($"health check complete for {containerType}");
            }
            else {
                this.Trace($"health check failed for {containerType}");
                throw new InvalidOperationException($"Health check failed for {containerType} [{healthCheckResult.Message}]");
            }
        }, cancellationToken, TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(20));
    }

    protected void Error(String message, Exception ex){
        if (this.Logger.IsInitialised){
            this.Logger.LogError($"{this.TestId}|{message}", ex);
        }
    }
    
    protected virtual String GenerateKurrentDbConnectionString(String userName = "admin", String password = "changeit"){
        String eventStoreAddress = $"esdb://{userName}:{password}@{this.KurrentDbContainerName}:{DockerPorts.KurrentDbHttpDockerPort}?tls=false";

        return eventStoreAddress;
    }

    protected virtual Int32 GetSecurityServicePort(){
        return DockerPorts.SecurityServiceDockerPort;
    }

    protected virtual List<String> GetRequiredProjections(){
        List<String> requiredProjections = [
            "MerchantAggregator.js",
            "MerchantBalanceCalculator.js",
            "MerchantBalanceProjection.js"
        ];

        return requiredProjections;
    }

    protected virtual async Task LoadEventStoreProjections(CancellationToken cancellationToken){
        //Start our Continuous Projections - we might decide to do this at a different stage, but now lets try here
        String projectionsFolder = "projections/continuous";

        if (!String.IsNullOrWhiteSpace(projectionsFolder))
        {
            DirectoryInfo di = new(projectionsFolder);

            if (di.Exists)
            {
                IReadOnlyList<FileInfo> files = this.GetProjectionFiles();
                var requiredProjections = this.GetRequiredProjections();
                KurrentDBProjectionManagementClient projectionClient = new(this.ConfigureKurrentDbSettings());
                List<String> projectionNames = new();

                foreach (FileInfo file in files)
                {
                    if (!requiredProjections.Contains(file.Name))
                        continue;

                    String projection = await BaseDockerHelper.RemoveProjectionTestSetup(file);
                    String projectionName = file.Name.Replace(".js", String.Empty);


                    this.Trace($"Creating projection [{projectionName}] from file [{file.FullName}]");
                    await this.CreateContinuousProjection(projectionClient, projectionName, projection, cancellationToken)
                        .ConfigureAwait(false);

                    projectionNames.Add(projectionName);
                    this.Trace($"Projection [{projectionName}] created");
                }

                // Now check the create status of each
                foreach (String projectionName in projectionNames)
                {
                    await Retry.For(async cancellationToken =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        ProjectionDetails projectionDetails = await this.GetProjectionStatus(
                                projectionClient,
                                projectionName,
                                cancellationToken)
                            .ConfigureAwait(false);

                        if (IsTerminalProjectionStatus(projectionDetails.Status))
                        {
                            throw new TerminalProjectionException(
                                $"Projection [{projectionName}] entered terminal state [{projectionDetails.Status}]");
                        }

                        projectionDetails.Status.ShouldBe("Running", $"Projection [{projectionName}] is {projectionDetails.Status}");
                        this.Trace($"Projection [{projectionName}] running");
                    },
                    cancellationToken,
                    exception => exception is not TerminalProjectionException,
                    TimeSpan.FromMinutes(3),
                    TimeSpan.FromSeconds(5));
                }
            }
        }

        this.Trace("Loaded projections");
    }

    protected virtual IReadOnlyList<FileInfo> GetProjectionFiles()
    {
        return new DirectoryInfo("projections/continuous").GetFiles();
    }

    protected virtual Task CreateContinuousProjection(
        KurrentDBProjectionManagementClient projectionClient,
        String projectionName,
        String projection,
        CancellationToken cancellationToken)
    {
        return projectionClient.CreateContinuousAsync(
            projectionName,
            projection,
            trackEmittedStreams: true,
            cancellationToken: cancellationToken);
    }

    protected virtual Task<ProjectionDetails> GetProjectionStatus(
        KurrentDBProjectionManagementClient projectionClient,
        String projectionName,
        CancellationToken cancellationToken)
    {
        return projectionClient.GetStatusAsync(
            projectionName,
            cancellationToken: cancellationToken);
    }

    private static Boolean IsTerminalProjectionStatus(String status)
    {
        return String.Equals(status, "Faulted", StringComparison.OrdinalIgnoreCase)
               || String.Equals(status, "Stopped", StringComparison.OrdinalIgnoreCase)
               || String.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase)
               || String.Equals(status, "Aborted", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TerminalProjectionException : InvalidOperationException
    {
        public TerminalProjectionException(String message)
            : base(message)
        {
        }
    }

    //protected virtual void SetAdditionalVariables(ContainerType containerType, List<String> variableList){
    //    this.AdditionalVariables.SingleOrDefault(a => a.Key == containerType).Value.AddRange(variableList);
    //}

    protected virtual String SetConnectionString(String databaseName,
                                                 Boolean isSecure = false){
        String encryptValue = String.Empty;
        if (!isSecure){
            encryptValue = ";Encrypt=False";
        }

        String scenarioDatabaseName = GetScenarioDatabaseName(
            databaseName,
            this.TestId,
            this.SharedSqlServerFixture is not null);

        String connectionString =
            $"server={this.SqlServerContainerName},1433;user id={this.SqlCredentials.usename};password={this.SqlCredentials.password};database={scenarioDatabaseName}{encryptValue}";
        
        return connectionString;
    }

    protected static String GetScenarioDatabaseName(
        String databaseName,
        Guid testId,
        Boolean sharedSqlServer)
    {
        if (sharedSqlServer == false || databaseName == "master")
        {
            return databaseName;
        }

        String compactSuffix = $"-{testId:N}";
        if (databaseName.EndsWith(compactSuffix, StringComparison.OrdinalIgnoreCase))
        {
            return databaseName;
        }

        String dashedSuffix = $"-{testId:D}";
        return databaseName.EndsWith(dashedSuffix, StringComparison.OrdinalIgnoreCase)
            ? $"{databaseName[..^dashedSuffix.Length]}{compactSuffix}"
            : $"{databaseName}{compactSuffix}";
    }

    
    protected static DockerServices GetServicesForStartup(
        DockerServices requiredServices,
        Boolean hasSharedSqlServer)
    {
        return hasSharedSqlServer
            ? requiredServices & ~DockerServices.SqlServer
            : requiredServices;
    }

    protected async Task<IContainer> StartContainer2(
        Func<ContainerBuilder> buildContainerFunc,
        List<INetwork> networkServices,
        DockerServices dockerService,
        CancellationToken cancellationToken){
        if ((this.RequiredDockerServices & dockerService) != dockerService)
        {
            return default;
        }

        IContainer builtContainer = default;
        Boolean containerStarted = false;

        try{

            await Retry.For(async cancellationToken =>
            {
                containerStarted = false;
                ContainerBuilder containerBuilder = buildContainerFunc();

                foreach (INetwork networkService in networkServices) {
                    containerBuilder = containerBuilder.WithNetwork(networkService);
                }

                builtContainer = containerBuilder.Build();
                try
                {
                    await builtContainer.StartAsync(cancellationToken);
                    containerStarted = true;

                    lock (this.ContainerStateLock)
                    {
                        this.Containers.Add((dockerService, builtContainer));
                    }
                }
                catch
                {
                    await this.CaptureContainerLogs(builtContainer, cancellationToken);
                    await builtContainer.DisposeAsync();
                    throw;
                }
            }, cancellationToken);

            this.Trace($"{dockerService} Container Started");
            
            //  Do a health check here
            ContainerType type = dockerService switch{
                DockerServices.CallbackHandler => ContainerType.CallbackHandler,
                DockerServices.MessagingService => ContainerType.MessagingService,
                DockerServices.SecurityService => ContainerType.SecurityService,
                DockerServices.FileProcessor => ContainerType.FileProcessor,
                DockerServices.TestHost => ContainerType.TestHost,
                DockerServices.TransactionProcessor => ContainerType.TransactionProcessor,
                DockerServices.TransactionProcessorAcl => ContainerType.TransactionProcessorAcl,
                DockerServices.KurrentDb => ContainerType.KurrentDb,
                DockerServices.SqlServer => ContainerType.SqlServer,
                DockerServices.ConfigurationHost => ContainerType.ConfigurationHost,
                DockerServices.EstateManagementUI => ContainerType.EstateManagementUI,
                DockerServices.EstateReporting => ContainerType.EstateReporting,
                _ => ContainerType.NotSet
            };

            lock (this.ContainerStateLock)
            {
                this.SetHostPortForService(type, builtContainer);
            }

            if (this.SkipHealthChecks) {
                this.Trace($"Container [{buildContainerFunc.Method.Name}] health check skipped");
            }
                
            else {
             
                switch (type)
                {
                    case ContainerType.KurrentDb:
                         await DoEventStoreHealthCheck(cancellationToken);
                        break;
                    case ContainerType.SqlServer:
                         await DoSqlServerHealthCheck(builtContainer, cancellationToken);
                        break;
                    default:
                         await this.DoHealthCheck(type, cancellationToken);
                        break;
                }
            }

            this.Trace($"Container [{buildContainerFunc.Method.Name}] started");

            return builtContainer;
        }
        catch (Exception ex){
            if (builtContainer is not null && containerStarted)
            {
                await this.CaptureContainerLogs(builtContainer, cancellationToken);
            }

            this.Error($"Error starting container [{buildContainerFunc.Method.Name}]", ex);
            throw;
        }
    }

    protected virtual async Task CaptureContainerLogs(
        IContainer container,
        CancellationToken cancellationToken)
    {
        try
        {
            (String stdout, String stderr) logs = await container.GetLogsAsync(
                DateTime.UtcNow.AddMinutes(-5),
                DateTime.UtcNow,
                timestampsEnabled: true,
                cancellationToken).ConfigureAwait(false);

            if (String.IsNullOrWhiteSpace(logs.stdout) == false)
            {
                this.Trace($"Container [{container.Name}] stdout:\n{logs.stdout}");
            }

            if (String.IsNullOrWhiteSpace(logs.stderr) == false)
            {
                this.Trace($"Container [{container.Name}] stderr:\n{logs.stderr}");
            }
        }
        catch (Exception logException)
        {
            this.Trace($"Unable to capture logs for container: {logException.Message}");
        }
    }

    private void SetHostPortForService(ContainerType type, IContainer startedContainer){
        UInt16 GetPort(Int32 dockerPort) =>
            startedContainer.GetMappedPublicPort(dockerPort);

        switch (type) {
            case ContainerType.KurrentDb:
                Int32 port = GetPort(DockerPorts.KurrentDbHttpDockerPort);
                KurrentDbHttpPort = port;
                if (IsSecureKurrentDb)
                    KurrentDbSecureHttpPort = port;
                break;

            case ContainerType.MessagingService: 
                MessagingServicePort = GetPort(DockerPorts.MessagingServiceDockerPort); 
                break;
            case ContainerType.SecurityService: 
                SecurityServicePort = GetPort(DockerPorts.SecurityServiceDockerPort);
                break;
            case ContainerType.CallbackHandler: 
                CallbackHandlerPort = GetPort(DockerPorts.CallbackHandlerDockerPort); 
                break;
            case ContainerType.TestHost: 
                TestHostServicePort = GetPort(DockerPorts.TestHostPort);
                break;
            case ContainerType.TransactionProcessor:
                TransactionProcessorPort = GetPort(DockerPorts.TransactionProcessorDockerPort); 
                break;
            case ContainerType.FileProcessor: 
                FileProcessorPort = GetPort(DockerPorts.FileProcessorDockerPort); 
                break;
            case ContainerType.TransactionProcessorAcl: 
                TransactionProcessorAclPort = GetPort(DockerPorts.TransactionProcessorAclDockerPort); 
                break;
            case ContainerType.ConfigurationHost:
                ConfigHostPort = GetPort(DockerPorts.ConfigHostDockerPort);
                break;
            case ContainerType.EstateManagementUI:
                EstateManagementUiPort = GetPort(DockerPorts.EstateManagementUIDockerPort);
                break;
            case ContainerType.SqlServer:
                SqlServerPort = GetPort(DockerPorts.SqlServerDockerPort);
                break;
            case ContainerType.EstateReporting:
                EstateReportingPort = GetPort(DockerPorts.EstateReportingDockerPort);
                break;

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


    /// <summary>
    /// Redacts the password portion of a connection string for safe logging.
    /// </summary>
    /// <param name="connString">The full connection string.</param>
    /// <returns>The connection string with the password replaced by [REDACTED]</returns>
    private string RedactConnectionString(string connString)
    {
        if (string.IsNullOrEmpty(connString))
            return connString;
        // Regex to match password=...; (case-insensitive)
        return System.Text.RegularExpressions.Regex.Replace(
            connString,
            @"(password\s*=\s*)([^;]*)(;?)",
            "$1[REDACTED]$3",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
        );
    }

}
