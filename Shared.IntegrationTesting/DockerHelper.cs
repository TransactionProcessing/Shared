namespace Shared.IntegrationTesting;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ductus.FluentDocker.Common;
using Ductus.FluentDocker.Services;
using Shouldly;

public class DockerHelper : BaseDockerHelper
{
    public DockerHelper() :base(){
        
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

        this.HostTraceFolder = FdOs.IsWindows() switch {
            true => $"C:\\home\\txnproc\\trace\\{scenarioName}",
            _ => $"//home//txnproc//trace//{scenarioName}"
        };

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
                                                                            },
                                                  this.PersistentSubscriptionSettings);

        await this.SetupEstateReportingContainer(new List<INetworkService> {
                                                                               testNetwork,
                                                                               this.SqlServerNetwork
                                                                           },
                                                 this.PersistentSubscriptionSettings);

        await this.SetupVoucherManagementContainer(new List<INetworkService> {
                                                                                 testNetwork,
                                                                                 this.SqlServerNetwork
                                                                             });

        await this.SetupTransactionProcessorContainer(new List<INetworkService> {
                                                                                    testNetwork,
                                                                                    this.SqlServerNetwork
                                                                                },
                                                      this.PersistentSubscriptionSettings);

        await this.SetupFileProcessorContainer(new List<INetworkService> {
                                                                             testNetwork,
                                                                             this.SqlServerNetwork
                                                                         },
                                               this.PersistentSubscriptionSettings);

        await this.SetupVoucherManagementAclContainer(new List<INetworkService> {
                                                                                    testNetwork,
                                                                                });

        await this.SetupTransactionProcessorAclContainer(testNetwork);

        await this.LoadEventStoreProjections();

        await this.CreateGenericSubscriptions();


    }

    public override async Task StopContainersForScenarioRun()
    {
        //await this.RemoveEstateReadModel().ConfigureAwait(false);

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
        List<(String streamName, String groupName, Int32 maxRetries)> subscriptions = new List<(String streamName, String groupName, Int32 maxRetries)>();
        subscriptions.Add(($"$ce-MerchantBalanceArchive", "Transaction Processor - Ordered", 0));
        subscriptions.Add(($"$et-EstateCreatedEvent", "Transaction Processor - Ordered", 2));
        foreach ((String streamName, String groupName, Int32 maxRetries) subscription in subscriptions) {
            await this.CreatePersistentSubscription(subscription);
        }
    }

    public virtual async Task CreateEstateSubscriptions(String estateName) {
        List<(String streamName, String groupName, Int32 maxRetries)> subscriptions = new List<(String streamName, String groupName, Int32 maxRetries)>();
        subscriptions.Add((estateName.Replace(" ", ""), "Reporting", 2));
        subscriptions.Add(($"EstateManagementSubscriptionStream_{estateName.Replace(" ", "")}", "Estate Management", 0));
        subscriptions.Add(($"TransactionProcessorSubscriptionStream_{estateName.Replace(" ", "")}", "Transaction Processor", 0));
        foreach ((String streamName, String groupName, Int32 maxRetries) subscription in subscriptions)
        {
            await this.CreatePersistentSubscription(subscription);
        }
    }
}