namespace Shared.IntegrationTesting;

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Ductus.FluentDocker;
using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Services;
using Newtonsoft.Json;
using Shouldly;

[Flags]
public enum DockerServices{
    SqlServer = 1,
    EventStore = 2,
    MessagingService = 4,
    SecurityService = 8,
    CallbackHandler = 16,
    TestHost = 32,
    EstateManagement = 64,
    TransactionProcessor = 128,
    FileProcessor = 256,
    TransactionProcessorAcl = 512 }

public class DockerHelper : BaseDockerHelper
{
    public DockerHelper() :base(){
    }
    
    protected  virtual void SetHostTraceFolder(String scenarioName) {
        String ciEnvVar = Environment.GetEnvironmentVariable("CI");
        
        // We are running on linux (CI or local ok)
        // We are running windows local (can use "C:\\home\\txnproc\\trace\\{scenarioName}")
        // We are running windows CI (can use "C:\\Users\\runneradmin\\trace\\{scenarioName}")

        Boolean isCI = (String.IsNullOrEmpty(ciEnvVar) == false && String.Compare(ciEnvVar, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0);
        if (FdOs.IsLinux()) {
            this.HostTraceFolder = $"/home/txnproc/trace/{scenarioName}";
        }
        else if (FdOs.IsOsx()) {
            this.HostTraceFolder = $"/Users/runner/txnproc/trace/{scenarioName}";
        }
        else {
            this.HostTraceFolder = isCI switch {
                false => $"C:\\home\\txnproc\\trace\\{scenarioName}",
                _ => $"C:\\Users\\runneradmin\\txnproc\\trace\\{scenarioName}",
            };
        }
        
        if (FdOs.IsLinux() == false){
            if (Directory.Exists(this.HostTraceFolder) == false){
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

        Logging.Enabled();

        this.TestId = Guid.NewGuid();

        this.Trace($"Test Id is {this.TestId}");

        this.SetupContainerNames();

        this.ClientDetails = ("serviceClient", "Secret1");

        INetworkService testNetwork = this.SetupTestNetwork();
        this.TestNetworks.Add(testNetwork);

        List<INetworkService> networks = new List<INetworkService>{
                                                                      this.SqlServerNetwork,
                                                                      testNetwork
                                                                  };

        await StartContainer2(this.SetupEventStoreContainer, networks, DockerServices.EventStore);
        // TODO: permenant fix for this hack
        await Task.Delay(TimeSpan.FromSeconds(30));
        await StartContainer2(this.SetupMessagingServiceContainer, networks, DockerServices.MessagingService);
        await StartContainer2(this.SetupSecurityServiceContainer, networks, DockerServices.SecurityService);
        await StartContainer2(this.SetupCallbackHandlerContainer, networks, DockerServices.CallbackHandler);
        await StartContainer2(this.SetupTestHostContainer, networks, DockerServices.TestHost);
        await StartContainer2(this.SetupEstateManagementContainer, networks, DockerServices.EstateManagement);
        await StartContainer2(this.SetupTransactionProcessorContainer, networks, DockerServices.TransactionProcessor);
        await StartContainer2(this.SetupFileProcessorContainer, networks, DockerServices.FileProcessor);
        await StartContainer2(this.SetupTransactionProcessorAclContainer, networks, DockerServices.TransactionProcessorAcl);
        
        await this.LoadEventStoreProjections();
        
        await this.CreateGenericSubscriptions();
    }

    public override async Task StopContainersForScenarioRun() {
        if (this.Containers.Any()) {
            this.Containers.Reverse();

            foreach (IContainerService containerService in this.Containers) {
                this.Trace($"Stopping container [{containerService.Name}]");
                containerService.Stop();
                containerService.Remove(true);
                containerService.Dispose();
                this.Trace($"Container [{containerService.Name}] stopped");
            }
        }

        if (this.TestNetworks.Any()) {
            foreach (INetworkService networkService in this.TestNetworks) {
                networkService.Stop();
                networkService.Remove(true);
            }
        }
    }

    public virtual async Task CreateGenericSubscriptions() {
        List<(String streamName, String groupName, Int32 maxRetries)> subscriptions = new List<(String streamName, String groupName, Int32 maxRetries)>
        {
            ($"$ce-MerchantBalanceArchive", "Transaction Processor - Ordered", 0),
            ($"$et-EstateCreatedEvent", "Transaction Processor - Ordered", 2)
        };
        foreach ((String streamName, String groupName, Int32 maxRetries) subscription in subscriptions) {
            await this.CreatePersistentSubscription(subscription);
        }
    }

    public virtual async Task CreateEstateSubscriptions(String estateName) {
        List<(String streamName, String groupName, Int32 maxRetries)> subscriptions = new List<(String streamName, String groupName, Int32 maxRetries)>
        {
            (estateName.Replace(" ", ""), "Estate Management", 2),
            ($"EstateManagementSubscriptionStream_{estateName.Replace(" ", "")}", "Estate Management - Ordered", 2),
            ($"TransactionProcessorSubscriptionStream_{estateName.Replace(" ", "")}", "Transaction Processor", 2),
            ($"FileProcessorSubscriptionStream_{estateName.Replace(" ", "")}", "File Processor", 2)
        };
        foreach ((String streamName, String groupName, Int32 maxRetries) subscription in subscriptions)
        {
            await this.CreatePersistentSubscription(subscription);
        }
    }
}