namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EventHandling;
using Extensions;
using global::EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Moq;
using Shouldly;
using SubscriptionWorker;
using TestObjects;
using Xunit;

public class IApplicationBuilderExtenstionsTests{
    private IApplicationBuilder builder;

    private SubscriptionWorkersRoot subscriptionWorkersRoot;

    private Mock<ISubscriptionRepository> subscriptionRepository;

    private Func<String, Int32, ISubscriptionRepository> subscriptionRepositoryResolver;

    private Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers;

    private Mock<IDomainEventHandlerResolver> domainEventHandlerResolver;

    public IApplicationBuilderExtenstionsTests(){
        this.builder = new ApplicationBuilder(new TestServiceProvider());
        this.subscriptionWorkersRoot = new SubscriptionWorkersRoot();
        this.subscriptionWorkersRoot.InternalSubscriptionService = true;
        this.subscriptionWorkersRoot.SubscriptionWorkers = new List<SubscriptionWorkerConfig>();

        Mock<ISubscriptionRepository> subscriptionRepository = new Mock<ISubscriptionRepository>();
        this.subscriptionRepositoryResolver = (s, i) => subscriptionRepository.Object;

        this.eventHandlerResolvers = new Dictionary<String, IDomainEventHandlerResolver>();
        this.domainEventHandlerResolver = new Mock<IDomainEventHandlerResolver>();
            
    }

    [Fact]
    public async Task ConfigureSubscriptionService_IsOrdered_ConfiguredSuccessfully()
    {
        this.subscriptionWorkersRoot.SubscriptionWorkers.Add(new SubscriptionWorkerConfig
        {
            Enabled = true,
            IsOrdered = true
        });
        this.eventHandlerResolvers.Add("Ordered", this.domainEventHandlerResolver.Object);

        await this.builder.ConfigureSubscriptionService(this.subscriptionWorkersRoot,
                                                        "esdb://admin:changeit@127.0.0.1:2113?tls=true&tlsVerifyCert=false",
                                                        new EventStoreClientSettings(),
                                                        this.eventHandlerResolvers,
                                                        null,
                                                        this.subscriptionRepositoryResolver,
                                                        CancellationToken.None);
    }

    [Fact]
    public async Task ConfigureSubscriptionService_IsConcurrent_ConfiguredSuccessfully()
    {
        this.subscriptionWorkersRoot.SubscriptionWorkers.Add(new SubscriptionWorkerConfig
        {
            Enabled = true,
            IsOrdered = false,
            InstanceCount = 1
        });
        this.eventHandlerResolvers.Add("Main", this.domainEventHandlerResolver.Object);

        await this.builder.ConfigureSubscriptionService(this.subscriptionWorkersRoot,
                                                        "esdb://admin:changeit@127.0.0.1:2113?tls=true&tlsVerifyCert=false",
                                                        new EventStoreClientSettings(),
                                                        this.eventHandlerResolvers,
                                                        null,
                                                        this.subscriptionRepositoryResolver,
                                                        CancellationToken.None);
    }


    [Fact]
    public async Task ConfigureSubscriptionService_NullWorkerConfig_ErrorThrown()
    {
        this.subscriptionWorkersRoot = null;
        Should.Throw<Exception>(async () =>
        {
            await this.builder.ConfigureSubscriptionService(this.subscriptionWorkersRoot,
                                                            "",
                                                            new EventStoreClientSettings(),
                                                            this.eventHandlerResolvers,
                                                            null,
                                                            this.subscriptionRepositoryResolver,
                                                            CancellationToken.None);
        });
    }

    [Fact]
    public async Task ConfigureSubscriptionService_NullSubscriptionRepositoryResolver_ErrorThrown()
    {
        this.subscriptionRepositoryResolver = null;
        Should.Throw<Exception>(async () =>
        {
            await this.builder.ConfigureSubscriptionService(this.subscriptionWorkersRoot,
                                                            "",
                                                            new EventStoreClientSettings(),
                                                            this.eventHandlerResolvers,
                                                            null,
                                                            this.subscriptionRepositoryResolver,
                                                            CancellationToken.None);
        });
    }

    [Fact]
    public async Task ConfigureSubscriptionService_NullSubscriptionWorkersList_ErrorThrown()
    {
        this.subscriptionWorkersRoot.SubscriptionWorkers = null;

        Should.Throw<Exception>(async () =>
        {
            await this.builder.ConfigureSubscriptionService(this.subscriptionWorkersRoot,
                                                            "",
                                                            new EventStoreClientSettings(),
                                                            this.eventHandlerResolvers,
                                                            null,
                                                            this.subscriptionRepositoryResolver,
                                                            CancellationToken.None);
        });
    }

    [Fact]
    public async Task ConfigureSubscriptionService_EmptySubscriptionWorkersList_ErrorThrown()
    {
        this.subscriptionWorkersRoot.SubscriptionWorkers = new List<SubscriptionWorkerConfig>();

        Should.Throw<Exception>(async () =>
        {
            await this.builder.ConfigureSubscriptionService(this.subscriptionWorkersRoot,
                                                            "",
                                                            new EventStoreClientSettings(),
                                                            this.eventHandlerResolvers,
                                                            null,
                                                            this.subscriptionRepositoryResolver,
                                                            CancellationToken.None);
        });
    }

    [Fact]
    public async Task ConfigureSubscriptionService_SubscriptionWorkersNotEnabled_NoErrorThrown()
    {

        this.subscriptionWorkersRoot.SubscriptionWorkers.Add(new SubscriptionWorkerConfig
        {
            Enabled = false
        });

        Should.NotThrow(async () =>
        {
            await this.builder.ConfigureSubscriptionService(this.subscriptionWorkersRoot,
                                                            "",
                                                            new EventStoreClientSettings(),
                                                            this.eventHandlerResolvers,
                                                            null,
                                                            this.subscriptionRepositoryResolver,
                                                            CancellationToken.None);
        });
    }

    [Fact]
    public async Task ConfigureSubscriptionService_InternalSubscriptionService_Off_NoError()
    {
        this.subscriptionWorkersRoot.InternalSubscriptionService = false;
        await this.builder.ConfigureSubscriptionService(this.subscriptionWorkersRoot,
                                                        "",
                                                        new EventStoreClientSettings(),
                                                        this.eventHandlerResolvers,
                                                        null,
                                                        this.subscriptionRepositoryResolver,
                                                        CancellationToken.None);
    }
}