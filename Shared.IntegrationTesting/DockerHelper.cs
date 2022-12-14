﻿namespace Shared.IntegrationTesting;

using System;
using System.Collections.Generic;
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
        
        if (isCI && FdOs.IsWindows()) {
            if (Directory.Exists(this.HostTraceFolder) == false) {
                this.Trace($"[{this.HostTraceFolder}] does not exist");
                Directory.CreateDirectory(this.HostTraceFolder);
                this.Trace($"[{this.HostTraceFolder}] created");
            }
            else {
                this.Trace($"[{this.HostTraceFolder}] already exists");
            }
        }

        if (isCI && FdOs.IsOsx()) {
            if (Directory.Exists(this.HostTraceFolder) == false) {
                this.Trace($"[{this.HostTraceFolder}] does not exist");
                Directory.CreateDirectory(this.HostTraceFolder);
                this.Trace($"[{this.HostTraceFolder}] created");
            }
            else {
                this.Trace($"[{this.HostTraceFolder}] already exists");
            }
        }

        this.Trace($"HostTraceFolder is [{this.HostTraceFolder}]");
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

        var networkConfig = testNetwork.GetConfiguration(true);
        this.Trace(JsonConvert.SerializeObject(networkConfig));

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
        if (this.Containers.Any()) {
            this.Containers.Reverse();

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