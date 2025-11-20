using System.Net.Http;
using System.Text;
using System.Threading;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using SimpleResults;

namespace Shared.IntegrationTesting.TestContainers;

using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

[Flags]
public enum DockerServices{
    SqlServer = 1,
    EventStore = 2,
    MessagingService = 4,
    SecurityService = 8,
    CallbackHandler = 16,
    TestHost = 32,
    TransactionProcessor = 64,
    FileProcessor = 128,
    TransactionProcessorAcl = 256
}

public abstract class DockerHelper : BaseDockerHelper
{
    protected DockerHelper(Boolean skipHealthChecks=false) :base(skipHealthChecks){
    }
    
    protected  virtual void SetHostTraceFolder(String scenarioName) {
        String ciEnvVar = Environment.GetEnvironmentVariable("CI");
        
        // We are running on linux (CI or local ok)
        // We are running windows local (can use "C:\\home\\txnproc\\trace\\{scenarioName}")
        // We are running windows CI (can use "C:\\Users\\runneradmin\\trace\\{scenarioName}")

        Boolean isCI = (!String.IsNullOrEmpty(ciEnvVar) && String.Compare(ciEnvVar, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            this.HostTraceFolder = $"/home/txnproc/trace/{scenarioName}";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            this.HostTraceFolder = $"/Users/runner/txnproc/trace/{scenarioName}";
        }
        else {
            this.HostTraceFolder = isCI switch {
                false => $"C:\\home\\txnproc\\trace\\{scenarioName}",
                _ => $"C:\\Users\\runneradmin\\txnproc\\trace\\{scenarioName}",
            };
        }
        
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (!Directory.Exists(this.HostTraceFolder)){
                this.Trace($"[{this.HostTraceFolder}] does not exist");
                Directory.CreateDirectory(this.HostTraceFolder);
                this.Trace($"[{this.HostTraceFolder}] created");
            }
            else{
                this.Trace($"[{this.HostTraceFolder}] already exists");
            }
        }
        
        this.Trace($"HostTraceFolder is [{this.HostTraceFolder}]");
    }

    
    public override async Task StartContainersForScenarioRun(String scenarioName, DockerServices dockerServices){
        Result<DockerEnginePlatform> result = await BaseDockerHelper.GetDockerEnginePlatform();
        this.DockerPlatform =result.Data;

        this.DockerCredentials.ShouldNotBeNull();
        this.SqlCredentials.ShouldNotBeNull();
        this.SqlServerContainer.ShouldNotBeNull();
        this.SqlServerNetwork.ShouldNotBeNull();

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
            this.SqlServerNetwork,
            testNetwork
        ];

        await StartContainer2(this.SetupEventStoreContainer, networks, DockerServices.EventStore);
        // TODO: permenant fix for this hack
        await Task.Delay(TimeSpan.FromSeconds(30));
        await StartContainer2(this.SetupMessagingServiceContainer, networks, DockerServices.MessagingService);
        await StartContainer2(this.SetupSecurityServiceContainer, networks, DockerServices.SecurityService);
        await StartContainer2(this.SetupCallbackHandlerContainer, networks, DockerServices.CallbackHandler);
        await StartContainer2(this.SetupTestHostContainer, networks, DockerServices.TestHost);
        await StartContainer2(this.SetupTransactionProcessorContainer, networks, DockerServices.TransactionProcessor);
        await StartContainer2(this.SetupFileProcessorContainer, networks, DockerServices.FileProcessor);
        await StartContainer2(this.SetupTransactionProcessorAclContainer, networks, DockerServices.TransactionProcessorAcl);

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

                String? name = containerService.Item2.Name;
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