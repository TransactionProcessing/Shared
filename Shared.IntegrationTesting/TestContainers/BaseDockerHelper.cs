using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using SimpleResults;
using System.Net.Http.Headers;

namespace Shared.IntegrationTesting.TestContainers;

using Docker.DotNet;
using EventStore.Client;
using global::Ductus.FluentDocker.Model.Networks;
using HealthChecks;
using Logger;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public abstract class BaseDockerHelper{
    #region Fields

    public Boolean SkipHealthChecks;

    public Dictionary<ContainerType, Dictionary<String,String>> AdditionalVariables = new();

    public (String URL, String UserName, String Password)? DockerCredentials;

    public ILogger Logger;

    public (String usename, String password) SqlCredentials = ("sa", "thisisalongpassword123!");
    
    public String SqlServerContainerName;

    public Guid TestId;
    
    public String ScenarioName;

    protected String CallbackHandlerContainerName;

    protected Int32 CallbackHandlerPort;

    protected (String clientId, String clientSecret) ClientDetails;

    protected List<(DockerServices, IContainer)> Containers;

    protected String EventStoreContainerName;

    public String ConfigHostContainerName;
    public Int32 ConfigHostPort;
    protected Int32 EventStoreHttpPort;
    protected Int32 EventStoreSecureHttpPort;

    protected String FileProcessorContainerName;

    protected Int32 FileProcessorPort;

    protected readonly IHealthCheckClient HealthCheckClient;

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

    private String sqlTestConnString;

    #endregion

    #region Constructors

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
        
        // Setup the default image details
        this.DockerPlatform = BaseDockerHelper.GetDockerEnginePlatform().Result.Data;
        if (this.DockerPlatform == DockerEnginePlatform.Windows)
        {
            this.ImageDetails.Add(ContainerType.SqlServer, ("iamrjindal/sqlserverexpress:2022", true));
            this.ImageDetails.Add(ContainerType.EventStore, ("stuartferguson/kurrentdb_windows", true));
            this.ImageDetails.Add(ContainerType.MessagingService, ("stuartferguson/messagingservicewindows:master", true));
            this.ImageDetails.Add(ContainerType.SecurityService, ("stuartferguson/securityservicewindows:master", true));
            this.ImageDetails.Add(ContainerType.CallbackHandler, ("stuartferguson/callbackhandlerwindows:master", true));
            this.ImageDetails.Add(ContainerType.TestHost, ("stuartferguson/testhostswindows:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessor, ("stuartferguson/transactionprocessorwindows:master", true));
            this.ImageDetails.Add(ContainerType.FileProcessor, ("stuartferguson/fileprocessorwindows:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessorAcl, ("stuartferguson/transactionprocessoraclwindows:master", true));
            this.ImageDetails.Add(ContainerType.ConfigurationHost, ("stuartferguson/mobileconfigurationwindows:master", true));
            //this.ImageDetails.Add(ContainerType.EstateManangementUI, ("stuartferguson/estatemanagementuiwindows:master", true));
        }
        else
        {
            this.ImageDetails.Add(ContainerType.SqlServer, ("mcr.microsoft.com/mssql/server:2022-latest", true));
            this.ImageDetails.Add(ContainerType.EventStore, ("kurrentplatform/kurrentdb:25.1", true));
            this.ImageDetails.Add(ContainerType.MessagingService, ("stuartferguson/messagingservice:master", true));
            this.ImageDetails.Add(ContainerType.SecurityService, ("stuartferguson/securityservice:master", true));
            this.ImageDetails.Add(ContainerType.CallbackHandler, ("stuartferguson/callbackhandler:master", true));
            this.ImageDetails.Add(ContainerType.TestHost, ("stuartferguson/testhosts:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessor, ("stuartferguson/transactionprocessor:master", true));
            this.ImageDetails.Add(ContainerType.FileProcessor, ("stuartferguson/fileprocessor:master", true));
            this.ImageDetails.Add(ContainerType.TransactionProcessorAcl, ("stuartferguson/transactionprocessoracl:master", true));
            this.ImageDetails.Add(ContainerType.ConfigurationHost, ("stuartferguson/mobileconfiguration:master", true));
            this.ImageDetails.Add(ContainerType.EstateManangementUI, ("stuartferguson/estatemanagementui:latest", true));
        }

        this.HostPorts = new Dictionary<ContainerType, Int32>();
    }

    #endregion

    #region Properties

    public Boolean IsSecureEventStore{ get; protected set; }

    #endregion

    #region Methods

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
            {"EventStoreSettings:ConnectionString", this.GenerateEventStoreConnectionString()},
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
            {"\"EventStoreSettings:Insecure", this.IsSecureEventStore.ToString()}
            
        };
    }

    public static async Task<SimpleResults.Result<DockerEnginePlatform>> GetDockerEnginePlatform(){
        try{
            DockerClient? docker = new DockerClientConfiguration().CreateClient();
            SystemInfoResponse? info = await docker.System.GetSystemInfoAsync();

            return info.OSType switch {
                "linux" => Result.Success(DockerEnginePlatform.Linux),
                "windows" => Result.Success(DockerEnginePlatform.Windows),
                _ => Result.Success(DockerEnginePlatform.Unknown)
            };
        }
        catch(Exception e){
            return Result.Failure($"Unable to determine docker Engine Platform. Exception [{e.Message}]");
        }
    }

    public static DockerClient GetDockerHost() => new DockerClientConfiguration().CreateClient();
    

    public Int32? GetHostPort(ContainerType key) =>
        key switch {
            ContainerType.CallbackHandler => this.CallbackHandlerPort,
            //ContainerType.EventStore => this.EventStoreHttpPort,
            ContainerType.ConfigurationHost => this.ConfigHostPort,
            ContainerType.FileProcessor => this.FileProcessorPort,
            ContainerType.MessagingService => this.MessagingServicePort,
            ContainerType.SecurityService => this.SecurityServicePort,
            ContainerType.TestHost => this.TestHostServicePort,
            ContainerType.TransactionProcessor => this.TransactionProcessorPort,
            ContainerType.TransactionProcessorAcl => this.TransactionProcessorAclPort,
            ContainerType.SqlServer => this.SqlServerPort,
            ContainerType.EstateManangementUI => this.EstateManagementUiPort,
            _ when key == ContainerType.EventStore && this.IsSecureEventStore => this.EventStoreSecureHttpPort,
            _ when key == ContainerType.EventStore && this.IsSecureEventStore == false => this.EventStoreHttpPort,
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
    public DockerEnginePlatform DockerPlatform { get; protected set; }

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
            .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
            .WithPortBinding(DockerPorts.CallbackHandlerDockerPort, true);

        return callbackHandlerContainer;
    }

    public virtual void SetupContainerNames(){
        // Setup the container names
        this.SqlServerContainerName= $"sqlserver{this.TestId:N}";
        this.EventStoreContainerName = $"eventstore{this.TestId:N}";
        this.SecurityServiceContainerName = $"securityservice{this.TestId:N}";
        this.TestHostContainerName = $"testhosts{this.TestId:N}";
        this.CallbackHandlerContainerName = $"callbackhandler{this.TestId:N}";
        this.FileProcessorContainerName = $"fileprocessor{this.TestId:N}";
        this.MessagingServiceContainerName = $"messaging{this.TestId:N}";
        this.TransactionProcessorContainerName = $"transaction{this.TestId:N}";
        this.TransactionProcessorAclContainerName = $"transactionacl{this.TestId:N}";
        this.ConfigHostContainerName = $"mobileconfighost{this.TestId:N}";
        this.EstateManagementUiContainerName = $"estateadministrationui{this.TestId:N}";
    }

    protected Int32 EstateManagementUiPort;
    protected Int32 SqlServerPort;

    protected String EstateManagementUiContainerName;

    public virtual ContainerBuilder SetupEventStoreContainer(){
        this.Trace($"About to Start Event Store Container [{this.DockerPlatform}]");

        Dictionary<String, String> environmentVariables = new(){
            {"EVENTSTORE_RUN_PROJECTIONS","all"},
            {"EVENTSTORE_START_STANDARD_PROJECTIONS","true"},
            {"EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP","true"},
            {"EVENTSTORE_PROJECTION_EXECUTION_TIMEOUT","5000"}
        };
        ContainerBuilder eventStoreContainer = new ContainerBuilder();
        
        if (!this.IsSecureEventStore){
            environmentVariables.Add("EVENTSTORE_INSECURE","true");
        }
        else{
            String certsPath = this.DockerPlatform switch
            {
                DockerEnginePlatform.Windows => "C:\\EventStoreCerts",
                _ => "/etc/eventstore/certs"
            };

            // Copy these to the container
            String path = Path.Combine(Directory.GetCurrentDirectory(), "certs");

            eventStoreContainer = eventStoreContainer.MountHostFolder(this.DockerPlatform, path, certsPath);

            // Certificates configuration
            environmentVariables.Add("EVENTSTORE_CertificateFile",$"{certsPath}/node1/node.crt");
            environmentVariables.Add("EVENTSTORE_CertificatePrivateKeyFile",$"{certsPath}/node1/node.key");
            environmentVariables.Add("EVENTSTORE_TrustedRootCertificatesPath", $"{certsPath}/ca");
            environmentVariables.Add("EVENTSTORE_INSECURE","false");
        }

        if (this.DockerPlatform == DockerEnginePlatform.Linux) {
            String logfilePath = "/var/log/kurrentdb";

            eventStoreContainer = eventStoreContainer.MountHostFolder(this.DockerPlatform, this.HostTraceFolder, logfilePath);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.EventStore).Data;
        this.Trace($"About to Start Event Store Container using image [{imageDetails.imageName}]");

        

        eventStoreContainer = eventStoreContainer.WithName(this.EventStoreContainerName)  // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .WithPortBinding(DockerPorts.EventStoreHttpDockerPort, true);
        
        return eventStoreContainer;
    }
    
    public virtual ContainerBuilder SetupFileProcessorContainer(){
        this.Trace("About to Start File Processor Container");

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add("urls",$"http://*:{DockerPorts.FileProcessorDockerPort}");
        environmentVariables.Add("ConnectionStrings:TransactionProcessorReadModel", this.SetConnectionString("TransactionProcessorReadModel", this.UseSecureSqlServerDatabase));

        String ciEnvVar = Environment.GetEnvironmentVariable("CI");
        Boolean isCi = !String.IsNullOrEmpty(ciEnvVar) && String.Compare(ciEnvVar, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // we are running in CI Linux
            environmentVariables.Add("AppSettings:TemporaryFileLocation","/home/runner/bulkfiles/temporary");

            environmentVariables.Add("AppSettings:FileProfiles:0:ListeningDirectory","/home/runner/bulkfiles/safaricom");
            environmentVariables.Add($"AppSettings:FileProfiles:1:ListeningDirectory","/home/runner/bulkfiles/voucher");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // we are running in CI Mac OS
            environmentVariables.Add("AppSettings:TemporaryFileLocation","/Users/runner/bulkfiles/temporary");

            environmentVariables.Add("AppSettings:FileProfiles:0:ListeningDirectory","/Users/runner/bulkfiles/safaricom");
            environmentVariables.Add("AppSettings:FileProfiles:1:ListeningDirectory","/Users/runner/bulkfiles/voucher");
        }
        else{
            // We know this is now windows
            if (isCi){
                Directory.CreateDirectory("C:\\Users\\runneradmin\\txnproc\\bulkfiles\\temporary");
                Directory.CreateDirectory("C:\\Users\\runneradmin\\txnproc\\bulkfiles\\safaricom");
                Directory.CreateDirectory("C:\\Users\\runneradmin\\txnproc\\bulkfiles\\voucher");

                environmentVariables.Add("AppSettings:TemporaryFileLocation", "C:\\Users\\runneradmin\\txnproc\\bulkfiles\\temporary");
                environmentVariables.Add("AppSettings:FileProfiles:0:ListeningDirectory","C:\\Users\\runneradmin\\txnproc\\bulkfiles\\safaricom");
                environmentVariables.Add("AppSettings:FileProfiles:1:ListeningDirectory","C:\\Users\\runneradmin\\txnproc\\bulkfiles\\voucher");
            }
            else{
                environmentVariables.Add("AppSettings:TemporaryFileLocation","C:\\home\\txnproc\\bulkfiles\\temporary");
                environmentVariables.Add("AppSettings:FileProfiles:0:ListeningDirectory","C:\\Users\\txnproc\\bulkfiles\\safaricom");
                environmentVariables.Add("AppSettings:FileProfiles:1:ListeningDirectory","C:\\Users\\txnproc\\bulkfiles\\voucher");
            }
        }

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
            .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
            .WithPortBinding(DockerPorts.FileProcessorDockerPort, true);
        
        // Mount the folder to upload files
        String uploadFolder = (this.DockerPlatform, isCi) switch{
            (DockerEnginePlatform.Windows, false) => "C:\\home\\txnproc\\reqnroll",
            (DockerEnginePlatform.Windows, true) => "C:\\Users\\runneradmin\\txnproc\\reqnroll",
            _ => "/home/txnproc/reqnroll"
        };

        if (this.DockerPlatform == DockerEnginePlatform.Windows && isCi){
            Directory.CreateDirectory(uploadFolder);
        }

        //String containerFolder = this.DockerPlatform == DockerEnginePlatform.Windows ? "C:\\home\\txnproc\\bulkfiles" : "/home/txnproc/bulkfiles";
        //fileProcessorContainer.Mount(uploadFolder, containerFolder, MountType.ReadWrite);
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
        environmentVariables.Add("ConnectionStrings:TransactionProcessorReadModel", this.SetConnectionString("TransactionProcessorReadModel", this.UseSecureSqlServerDatabase));

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.EstateManangementUI).Data;

        ContainerBuilder containerBuilder = new ContainerBuilder()
            .WithName(this.EstateManagementUiContainerName)
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
            .WithPortBinding(DockerPorts.EstateManagementUIDockerPort,true);

        return containerBuilder;
    }

    public virtual ContainerBuilder SetupConfigHostContainer()
    {
        this.Trace("About to Start Config Host Container");
        Dictionary<String, String> environmentVariables = new();
        environmentVariables.Add("AppSettings:InMemoryDatabase", "true");

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.ConfigurationHost).Data;

        ContainerBuilder configHostContainer = new ContainerBuilder().WithName(this.ConfigHostContainerName)
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
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
            .WithImage(imageDetails.imageName).WithEnvironment(environmentVariables).MountHostFolder(this.DockerPlatform, this.HostTraceFolder).WithPortBinding(DockerPorts.MessagingServiceDockerPort, true);
            //.WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(DockerPorts.MessagingServiceDockerPort));
        
        return messagingServiceContainer;
    }

    public virtual ContainerBuilder SetupSecurityServiceContainer(){
        this.Trace("About to Start Security Container");

        Dictionary<String, String> environmentVariables = this.GetCommonEnvironmentVariables();
        environmentVariables.Add("ServiceOptions:PublicOrigin",$"https://{this.SecurityServiceContainerName}:{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add("ServiceOptions:IssuerUrl",$"https://{this.SecurityServiceContainerName}:{DockerPorts.SecurityServiceDockerPort}");
        environmentVariables.Add("ASPNETCORE_ENVIRONMENT","IntegrationTest");
        environmentVariables.Add("urls",$"https://*:{DockerPorts.SecurityServiceDockerPort}");

        environmentVariables.Add("ServiceOptions:PasswordOptions:RequiredLength","6");
        environmentVariables.Add("ServiceOptions:PasswordOptions:RequireDigit","false");
        environmentVariables.Add("ServiceOptions:PasswordOptions:RequireUpperCase","false");
        environmentVariables.Add("ServiceOptions:UserOptions:RequireUniqueEmail","false");
        environmentVariables.Add("ServiceOptions:SignInOptions:RequireConfirmedEmail","false");

        environmentVariables.Add("ConnectionStrings:PersistedGrantDbContext", this.SetConnectionString( $"PersistedGrantStore-{this.TestId}", this.UseSecureSqlServerDatabase));
        environmentVariables.Add("ConnectionStrings:ConfigurationDbContext", this.SetConnectionString($"Configuration-{this.TestId}", this.UseSecureSqlServerDatabase));
        environmentVariables.Add("ConnectionStrings:AuthenticationDbContext", this.SetConnectionString($"Authentication-{this.TestId}", this.UseSecureSqlServerDatabase));

        Dictionary<String, String> additionalEnvironmentVariables = this.GetAdditionalVariables(ContainerType.SecurityService);

        foreach (KeyValuePair<String, String> additionalEnvironmentVariable in additionalEnvironmentVariables) {
            environmentVariables.Add(additionalEnvironmentVariable.Key, additionalEnvironmentVariable.Value);
        }

        (String imageName, Boolean useLatest) imageDetails = this.GetImageDetails(ContainerType.SecurityService).Data;

        ContainerBuilder securityServiceContainer = new ContainerBuilder()
            .WithName(this.SecurityServiceContainerName)  // similar to WithName()
            .WithImage(imageDetails.imageName)
            .WithEnvironment(environmentVariables)
            .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
            .WithPortBinding(DockerPorts.SecurityServiceDockerPort, true);
        
        // TODO: might need this but not sure yet
        //Int32? hostPort = this.GetHostPort(ContainerType.SecurityService);
        //securityServiceContainer = hostPort == null ? securityServiceContainer.ExposePort(DockerPorts.SecurityServiceDockerPort) : securityServiceContainer.ExposePort(hostPort.Value, DockerPorts.SecurityServiceDockerPort);

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

    //public virtual async Task<IContainer> SetupSqlServerContainer(INetwork networkService){
    //    if (this.SqlCredentials == default)
    //        throw new ArgumentNullException("Sql Credentials have not been set");

    //    IContainer databaseServerContainer = await this.StartContainer2(this.ConfigureSqlContainer,
    //                                                                           new List<INetwork>{
    //                                                                                                        networkService
    //                                                                                                    },
    //                                                                           DockerServices.SqlServer);
        
    //    return databaseServerContainer;
    //}

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
            .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
            .WithPortBinding(DockerPorts.TestHostPort, true);
        
        return testHostContainer;
    }

    public virtual async Task<INetwork> SetupTestNetwork(String networkName = null,
                                                    Boolean reuseIfExists = false){
        networkName = String.IsNullOrEmpty(networkName) ? $"testnw{this.TestId:N}" : networkName;
        NetworkBuilder networkService = this.DockerPlatform switch {
            DockerEnginePlatform.Windows => new NetworkBuilder()
                // Give it a name, or it will be generated (recommended)
                .WithName(networkName)
                // **Crucial step: Specify the Windows-native 'nat' driver**
                .WithDriver(NetworkDriver.Nat).WithReuse(reuseIfExists)
                .WithLabel("reuse-id", networkName),
            _ => new NetworkBuilder().WithName(networkName).WithReuse(reuseIfExists).WithLabel("reuse-id", networkName)
        };
        
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
            .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
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
            .MountHostFolder(this.DockerPlatform, this.HostTraceFolder)
            .WithPortBinding(DockerPorts.TransactionProcessorDockerPort, true);

        // TODO: Extension for multiple networks
        //foreach (INetwork testNetwork in this.TestNetworks) {
        //    transactionProcessorContainer = transactionProcessorContainer.WithNetwork(testNetwork);
        //}

        return transactionProcessorContainer;
    }

    public abstract Task StartContainersForScenarioRun(String scenarioName, DockerServices dockerServices);

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

    protected virtual EventStoreClientSettings ConfigureEventStoreSettings(String username = "admin", String password="changeit"){
        String connectionString = $"esdb://{username}:{password}@127.0.0.1:{this.EventStoreHttpPort}";
        
        connectionString = this.IsSecureEventStore switch{
            true => $"{connectionString}?tls=true&tlsVerifyCert=false",
            _ => $"{connectionString}?tls=false&tlsVerifyCert=false"
        };
        
        return EventStoreClientSettings.Create(connectionString);
    }

    protected virtual async Task CreatePersistentSubscription((String streamName, String groupName, Int32 maxRetryCount) subscription){
        EventStorePersistentSubscriptionsClient client = new(this.ConfigureEventStoreSettings());

        PersistentSubscriptionSettings settings = new(resolveLinkTos:true, StreamPosition.Start, maxRetryCount:subscription.maxRetryCount);
        this.Trace($"Creating persistent subscription Group [{subscription.groupName}] Stream [{subscription.streamName}] Retry Count [{subscription.maxRetryCount}]");
        await client.CreateToStreamAsync(subscription.streamName, subscription.groupName, settings);

        this.Trace($"Subscription Group [{subscription.groupName}] Stream [{subscription.streamName}] created");
    }

    protected async Task DoSqlServerHealthCheck(IContainer containerService){
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
            throw new ApplicationException($"Database container not started in {maxRetries} retries");
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

            using HttpClientHandler httpClientHandler = new();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            using HttpClient client = new(httpClientHandler);
            String authenticationString = "admin:changeit";
            String base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            HttpResponseMessage pingResponse = await client.GetAsync(url).ConfigureAwait(false);
            pingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        },

                        TimeSpan.FromSeconds(300),
                        TimeSpan.FromSeconds(30)).ConfigureAwait(false);

        this.Trace("About to do event store info");

        await Retry.For(async () =>
        {
            String url = $"{scheme}://127.0.0.1:{this.EventStoreHttpPort}/info";

            using HttpClientHandler httpClientHandler = new();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            using HttpClient client = new(httpClientHandler);
            String authenticationString = "admin:changeit";
            String base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authenticationString));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            HttpResponseMessage infoResponse = await client.GetAsync(url).ConfigureAwait(false);

            infoResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            String infoData = await infoResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

            this.Trace(infoData);
        });
    }

    protected async Task DoHealthCheck(ContainerType containerType) {
        (String, Int32) containerDetails = containerType switch {
            ContainerType.CallbackHandler => ("http", this.CallbackHandlerPort),
            ContainerType.FileProcessor => ("http", this.FileProcessorPort),
            ContainerType.MessagingService => ("http", this.MessagingServicePort),
            ContainerType.TestHost => ("http", this.TestHostServicePort),
            ContainerType.TransactionProcessor => ("http", this.TransactionProcessorPort),
            ContainerType.SecurityService => ("https", this.SecurityServicePort),
            ContainerType.TransactionProcessorAcl => ("http", this.TransactionProcessorAclPort),
            //ContainerType.ConfigurationHost => ("http", this.ConfigHostPort),
            //ContainerType.EstateManangementUI => ("https", this.EstateManagementUiPort),
            _ => (null, 0)
        };

        if (containerDetails.Item1 == null)
            return;

        await Retry.For(async () => {
            this.Trace($"About to do health check for {containerType}");

            SimpleResults.Result<String> healthCheckResult = await this.HealthCheckClient.PerformHealthCheck(containerDetails.Item1, "127.0.0.1", containerDetails.Item2, CancellationToken.None);

            if (healthCheckResult.IsSuccess) {
                HealthChecks.HealthCheckResult result = JsonConvert.DeserializeObject<HealthChecks.HealthCheckResult>(healthCheckResult.Data);

                this.Trace($"health check complete for {containerType} result is [{healthCheckResult.Data}]");

                result.Status.ShouldBe(HealthCheckStatus.Healthy.ToString(), $"Service Type: {containerType} Details {healthCheckResult.Data}");
                this.Trace($"health check complete for {containerType}");
            }
            else {
                this.Trace($"health check failed for {containerType}");
                throw new InvalidOperationException($"Health check failed for {containerType} [{healthCheckResult.Message}]");
            }
        }, TimeSpan.FromMinutes(3), TimeSpan.FromSeconds(20));
    }

    protected void Error(String message, Exception ex){
        if (this.Logger.IsInitialised){
            this.Logger.LogError($"{this.TestId}|{message}", ex);
        }
    }
    
    protected virtual String GenerateEventStoreConnectionString(String userName = "admin", String password = "changeit"){
        String eventStoreAddress = $"esdb://{userName}:{password}@{this.EventStoreContainerName}:{DockerPorts.EventStoreHttpDockerPort}?tls=false";

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

    protected virtual async Task LoadEventStoreProjections(){
        //Start our Continuous Projections - we might decide to do this at a different stage, but now lets try here
        String projectionsFolder = "projections/continuous";

        if (!String.IsNullOrWhiteSpace(projectionsFolder)){
            DirectoryInfo di = new(projectionsFolder);

            if (di.Exists){
                FileInfo[] files = di.GetFiles();
                var requiredProjections = this.GetRequiredProjections();
                EventStoreProjectionManagementClient projectionClient = new(this.ConfigureEventStoreSettings());
                List<String> projectionNames = new();
                
                foreach (FileInfo file in files){
                    if (!requiredProjections.Contains(file.Name))
                        continue;

                    String projection = await BaseDockerHelper.RemoveProjectionTestSetup(file);
                    String projectionName = file.Name.Replace(".js", String.Empty);

                    Should.NotThrow(async () => {
                                        this.Trace($"Creating projection [{projectionName}] from file [{file.FullName}]");
                                        try{
                                            await projectionClient.CreateContinuousAsync(projectionName, projection, trackEmittedStreams:true).ConfigureAwait(false);
                                        }
                                        catch (Exception ex) {
                                            // ignored
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

    //protected virtual void SetAdditionalVariables(ContainerType containerType, List<String> variableList){
    //    this.AdditionalVariables.SingleOrDefault(a => a.Key == containerType).Value.AddRange(variableList);
    //}

    protected virtual String SetConnectionString(String databaseName,
                                                 Boolean isSecure = false){
        String encryptValue = String.Empty;
        if (!isSecure){
            encryptValue = ";Encrypt=False";
        }

        String connectionString =
            $"server={this.SqlServerContainerName},1433;user id={this.SqlCredentials.usename};password={this.SqlCredentials.password};database={databaseName}{encryptValue}";
        
        return connectionString;
    }

    
    protected async Task<IContainer> StartContainer2(Func<ContainerBuilder> buildContainerFunc, List<INetwork> networkServices, DockerServices dockerService){
        if ((this.RequiredDockerServices & dockerService) != dockerService)
        {
            return default;
        }

        try{
            ContainerBuilder containerBuilder = buildContainerFunc();
            
            foreach (INetwork networkService in networkServices) {
                containerBuilder = containerBuilder.WithNetwork(networkService);
            }

            IContainer builtContainer = containerBuilder.Build();
                
            await builtContainer.StartAsync();
            this.Trace($"{dockerService} Container Started");
            this.Containers.Add((dockerService, builtContainer));

            //  Do a health check here
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
                DockerServices.ConfigurationHost => ContainerType.ConfigurationHost,
                DockerServices.EstateManagementUI => ContainerType.EstateManangementUI,
                _ => ContainerType.NotSet
            };

            this.SetHostPortForService(type, builtContainer);

            if (this.SkipHealthChecks) {
                this.Trace($"Container [{buildContainerFunc.Method.Name}] health check skipped");
            }
                
            else {
             
                switch (type)
                {
                    case ContainerType.EventStore:
                        await DoEventStoreHealthCheck();
                        break;
                    case ContainerType.SqlServer:
                        await DoSqlServerHealthCheck(builtContainer);
                        break;
                    default:
                        await this.DoHealthCheck(type);
                        break;
                }
            }

            this.Trace($"Container [{buildContainerFunc.Method.Name}] started");
            
            return builtContainer;
        }
        catch (Exception ex){
            // TODO: read console logs
            //if (consoleLogs != null){
            //    while (!consoleLogs.IsFinished){
            //        String s = consoleLogs.TryRead(10000);
            //        this.Trace(s);
            //    }
            //}

            this.Error($"Error starting container [{buildContainerFunc.Method.Name}]", ex);
            throw;
        }
    }

    private void SetHostPortForService(ContainerType type, IContainer startedContainer){
        UInt16 GetPort(Int32 dockerPort) =>
            startedContainer.GetMappedPublicPort(dockerPort);

        switch (type) {
            case ContainerType.EventStore:
                Int32 port = GetPort(DockerPorts.EventStoreHttpDockerPort);
                EventStoreHttpPort = port;
                if (IsSecureEventStore)
                    EventStoreSecureHttpPort = port;
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
            case ContainerType.EstateManangementUI:
                EstateManagementUiPort = GetPort(DockerPorts.EstateManagementUIDockerPort);
                break;
            case ContainerType.SqlServer:
                SqlServerPort = GetPort(DockerPorts.SqlServerDockerPort);
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

    #endregion
}
