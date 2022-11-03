namespace Shared.IntegrationTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Services;
using Shouldly;

public class DockerHelper : BaseDockerHelper
{
    public DockerHelper() :base(){
        
    }

    private void SetHostTraceFolder(String scenarioName) {
        String ciEnvVar = Environment.GetEnvironmentVariable("CI");
        DockerEnginePlatform engineType = DockerHelper.GetDockerEnginePlatform();

        String homeFolder = "txnproc";

        if ((String.IsNullOrEmpty(ciEnvVar) == false) 
            && String.Compare(ciEnvVar, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0) {
            homeFolder = "runner";
        }

        this.HostTraceFolder = engineType switch {
            DockerEnginePlatform.Windows => $"C:\\home\\{homeFolder}\\trace\\{scenarioName}",
            _ => $"//home//{homeFolder}//trace//{scenarioName}"
        };
        this.Trace("HostTraceFolder is [{this.HostTraceFolder}]");
    }

    public override async Task StartContainersForScenarioRun(String scenarioName) {

        this.DockerCredentials.ShouldNotBeNull();
        this.SqlCredentials.ShouldNotBeNull();
        this.SqlServerContainer.ShouldNotBeNull();
        this.SqlServerNetwork.ShouldNotBeNull();

        this.IsSecureEventStore = Environment.GetEnvironmentVariable("IsSecureEventStore") switch {
            null => false,
            {Length: 0} => false,
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

        await this.SetupEventStoreContainer( testNetwork, isSecure:this.IsSecureEventStore);

        await this.SetupMessagingServiceContainer(
                                                  new List<INetworkService> {
                                                                                testNetwork,
                                                                                this.SqlServerNetwork
                                                                            });

        await this.SetupSecurityServiceContainer(testNetwork);

        await this.SetupCallbackHandlerContainer(new List<INetworkService> {
                                                                               testNetwork
                                                                           });

        await this.SetupTestHostContainer(
                                          new List<INetworkService> {
                                                                        testNetwork,
                                                                        this.SqlServerNetwork
                                                                    });

        await this.SetupEstateManagementContainer(new List<INetworkService> {
                                                                                testNetwork,
                                                                                this.SqlServerNetwork
                                                                            });

        await this.SetupEstateReportingContainer(new List<INetworkService> {
                                                                               testNetwork,
                                                                               this.SqlServerNetwork
                                                                           });

        await this.SetupVoucherManagementContainer(new List<INetworkService> {
                                                                                 testNetwork,
                                                                                 this.SqlServerNetwork
                                                                             });

        await this.SetupTransactionProcessorContainer(new List<INetworkService> {
                                                                                    testNetwork,
                                                                                    this.SqlServerNetwork
                                                                                });

        await this.SetupFileProcessorContainer(new List<INetworkService> {
                                                                             testNetwork,
                                                                             this.SqlServerNetwork
                                                                         });

        await this.SetupVoucherManagementAclContainer(new List<INetworkService> {
                                                                                    testNetwork,
                                                                                });

        await this.SetupTransactionProcessorAclContainer(testNetwork);

        await this.LoadEventStoreProjections();

        await this.CreateGenericSubscriptions();
    }

    public override async Task StopContainersForScenarioRun()
    {
        if (this.Containers.Any())
        {
            foreach (IContainerService containerService in this.Containers)
            {
                this.Trace($"Stopping container [{containerService.Name}]");
                containerService.StopOnDispose = true;
                containerService.RemoveOnDispose = true;
                containerService.Dispose();
                this.Trace($"Container [{containerService.Name}] stopped");
            }
        }

        if (this.TestNetworks.Any())
        {
            foreach (INetworkService networkService in this.TestNetworks)
            {
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
            (estateName.Replace(" ", ""), "Reporting", 2),
            ($"EstateManagementSubscriptionStream_{estateName.Replace(" ", "")}", "Estate Management", 0),
            ($"TransactionProcessorSubscriptionStream_{estateName.Replace(" ", "")}", "Transaction Processor", 0),
            ($"FileProcessorSubscriptionStream_{estateName.Replace(" ", "")}", "File Processor", 0)
        };
        foreach ((String streamName, String groupName, Int32 maxRetries) subscription in subscriptions)
        {
            await this.CreatePersistentSubscription(subscription);
        }
    }
}