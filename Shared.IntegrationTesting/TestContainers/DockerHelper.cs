using System.Threading;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Shared.Serialisation;
using SimpleResults;
using System.Linq;

namespace Shared.IntegrationTesting.TestContainers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public abstract class DockerHelper : BaseDockerHelper
{
    protected DockerHelper(Boolean skipHealthChecks=false) :base(skipHealthChecks){
        StringSerialiser.Initialise(new SystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions()));
    }
    
    protected  virtual void SetHostTraceFolder(String scenarioName) {
        String ciEnvVar = Environment.GetEnvironmentVariable("CI");
        
        // We are running on linux (CI or local ok)
        // We are running windows local (can use "C:\\home\\txnproc\\trace\\{scenarioName}")
        // We are running windows CI (can use "C:\\Users\\runneradmin\\trace\\{scenarioName}")

        Boolean isCI = (!String.IsNullOrEmpty(ciEnvVar) && String.Compare(ciEnvVar, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0);
        
        OSPlatform platform = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux :
                              RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? OSPlatform.OSX :
                              OSPlatform.Windows;

        this.HostTraceFolder = (isCI, platform) switch
        {
            (true, var p) when p == OSPlatform.Linux => $"/home/runner/trace/{scenarioName}",
            (true, var p) when p == OSPlatform.OSX => $"/Users/runner/trace/{scenarioName}",
            (true, var p) when p == OSPlatform.Windows => $"C:\\Users\\runneradmin\\trace\\{scenarioName}",

            (false, var p) when p == OSPlatform.Linux => $"/home/txnproc/trace/{scenarioName}",
            (false, var p) when p == OSPlatform.OSX => $"/Users/txnproc/trace/{scenarioName}",
            (false, var p) when p == OSPlatform.Windows => $"C:\\home\\txnproc\\trace\\{scenarioName}",
        };
        
        if (Directory.Exists(this.HostTraceFolder) == false){
            this.Trace($"[{this.HostTraceFolder}] does not exist");
            Directory.CreateDirectory(this.HostTraceFolder);
            this.Trace($"[{this.HostTraceFolder}] created");
        }
        else{
            this.Trace($"[{this.HostTraceFolder}] already exists");
        }
        
        this.Trace($"HostTraceFolder is [{this.HostTraceFolder}]");
    }

    
    public override async Task StartContainersForScenarioRun(String scenarioName, DockerServices dockerServices){
        Result<DockerEnginePlatform> result = await BaseDockerHelper.GetDockerEnginePlatform();
        this.DockerPlatform =result.Data;

        this.RequiredDockerServices = dockerServices;

        this.IsSecureEventStore = Environment.GetEnvironmentVariable("IsSecureEventStore") switch{
            null => false,
            { Length: 0 } => false,
            "false" => false,
            _ => true
        };
        this.SetHostTraceFolder(scenarioName);
        
        this.TestId = Guid.NewGuid();

        this.Trace($"Test Id is {this.TestId}");

        this.SetupContainerNames();

        this.ClientDetails = ("serviceClient", "Secret1");

        INetwork testNetwork = await this.SetupTestNetwork();
        this.TestNetworks.Add(testNetwork);

        List<INetwork> networks = [
            testNetwork,
        ];

        async Task StartWithTrace(Func<DotNet.Testcontainers.Builders.ContainerBuilder> setupContainer, DockerServices service)
        {
            DateTimeOffset startedAt = DateTimeOffset.UtcNow;
            this.Trace($"Starting container [{service}]");
            await StartContainer2(setupContainer, networks, service);
            TimeSpan elapsed = DateTimeOffset.UtcNow - startedAt;
            this.Trace($"Container [{service}] started in {elapsed.TotalSeconds:N1}s");
        }

        Func<DockerServices, Func<DotNet.Testcontainers.Builders.ContainerBuilder>> setupContainerForService = service => service switch
        {
            DockerServices.SqlServer => this.ConfigureSqlContainer,
            DockerServices.EventStore => this.SetupEventStoreContainer,
            DockerServices.MessagingService => this.SetupMessagingServiceContainer,
            DockerServices.SecurityService => this.SetupSecurityServiceContainer,
            DockerServices.CallbackHandler => this.SetupCallbackHandlerContainer,
            DockerServices.TestHost => this.SetupTestHostContainer,
            DockerServices.TransactionProcessor => this.SetupTransactionProcessorContainer,
            DockerServices.FileProcessor => this.SetupFileProcessorContainer,
            DockerServices.TransactionProcessorAcl => this.SetupTransactionProcessorAclContainer,
            DockerServices.ConfigurationHost => this.SetupConfigHostContainer,
            DockerServices.EstateManagementUI => this.SetupEstateManagementUiContainer,
            DockerServices.EstateReporting => this.SetupEstateReportingContainer,
            _ => throw new InvalidOperationException($"No startup plan exists for docker service [{service}]")
        };

        foreach (IReadOnlyList<DockerServices> startupGroup in this.GetStartupGroups())
        {
            await Task.WhenAll(startupGroup.Select(service => StartWithTrace(setupContainerForService(service), service)));
        }

        await this.LoadEventStoreProjections();
        
        await this.CreateSubscriptions();
    }

    //protected virtual async Task CopyEventStoreLogs(IContainer eventStoreContainerService){
    //    try
    //    {
    //        if (this.DockerPlatform == DockerEnginePlatform.Windows)
    //            return;

    //        String logfilePath = "/var/log/kurrentdb";

    //        await eventStoreContainerService.CopyFolderAsync(this.HostTraceFolder, logfilePath);
            
    //    }
    //    catch (Exception ex)
    //    {
    //        this.Trace($"copy failed [{ex.Message}]");
    //    }
    //}
    
    public override async Task StopContainersForScenarioRun(DockerServices sharedDockerServices) {
        if (this.Containers.Any()) {
            this.Containers.Reverse();

            foreach ((DockerServices, IContainer) containerService in this.Containers) {

                if ((sharedDockerServices & containerService.Item1) == containerService.Item1){
                    continue;
                }

                String? name;
                try
                {
                    name = containerService.Item2.Name;
                }
                catch (InvalidOperationException ex)
                {
                    this.Trace($"Skipping container entry that is no longer available [{containerService.Item1}] ({ex.Message})");
                    continue;
                }
                this.Trace($"Stopping container [{name}]");
                //if (name.Contains("eventstore"))
                //{
                //    CopyEventStoreLogs(containerService.Item2);
                //}
                await containerService.Item2.StopAsync(CancellationToken.None);
                await containerService.Item2.DisposeAsync();
                this.Trace($"Container [{name}] stopped");
            }
        }

        if (this.TestNetworks.Any()) {
            foreach (INetwork networkService in this.TestNetworks){
                await networkService.DeleteAsync(CancellationToken.None);
                await networkService.DisposeAsync();
            }
        }
    }

    public abstract Task CreateSubscriptions();
}
