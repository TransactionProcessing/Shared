using System.Text.Json;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using KurrentDB.Client;
using Shared.EventStore.Tests.TestObjects;
using Shared.Serialisation;
using global::EventStore.Client;

namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shared.DomainDrivenDesign.EventSourcing;
using Shared.EventStore.EventHandling;
using SimpleResults;
using Shared.EventStore.SubscriptionWorker;
using Shared.General;
using Shared.Logger;
using Imposter.Abstractions;
using Shouldly;
using Xunit;

public class PersistentSubscriptionTests : IDisposable
{
    public PersistentSubscriptionTests()
    {
        Logger.Initialise(NullLogger.Instance);
        ConfigurationReader.Initialise(new ConfigurationRoot(new List<IConfigurationProvider>()));

        TypeMap.AddType<EstateCreatedEvent>("EstateCreatedEvent");
        StringSerialiser.Initialise(new SystemTextJsonSerializer(new JsonSerializerOptions()));
    }

    [Fact]
    public async Task PersistentSubscription_CanBeCreatedAndReceiveEventsSingleEventHandler()
    {
        PersistentSubscriptionDetails persistentSubscriptionDetails = new("$ce-test", "local-1");
        TestDomainEventHandler eventHandler = new();
        IDomainEventHandlerResolverImposter domainEventHandlerResolver = new IDomainEventHandlerResolverImposter();
        domainEventHandlerResolver.GetDomainEventHandlers(Arg<IDomainEvent>.Any()).Returns(new List<IDomainEventHandler>()
        {
            eventHandler
        });
            
        InMemoryPersistentSubscriptionsClient persistentSubscriptionsClient = new();
        CancellationToken cancellationToken = CancellationToken.None;

            

        var persistentSubscription = PersistentSubscription.Create(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver.Instance());

        await persistentSubscription.ConnectToSubscription(cancellationToken);

        persistentSubscription.Connected.ShouldBeTrue();

        String @event = "{\r\n  \"estateId\": \"4fc2692f-067a-443e-8006-335bf2732248\",\r\n  \"estateName\": \"Demo Estate\"\r\n}\t";

        //Manually add events.
        persistentSubscriptionsClient.WriteEvent(@event, "EstateCreatedEvent", cancellationToken);

        //Crude - but a decent start point
        eventHandler.DomainEvents.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PersistentSubscription_CanBeCreatedAndFilterOutSystemEvent()
    {
        PersistentSubscriptionDetails persistentSubscriptionDetails = new("$ce-test", "local-1");
        TestDomainEventHandler eventHandler = new();
        IDomainEventHandlerResolverImposter domainEventHandlerResolver = new IDomainEventHandlerResolverImposter();
        domainEventHandlerResolver.GetDomainEventHandlers(Arg<IDomainEvent>.Any()).Returns(new List<IDomainEventHandler>()
        {
            eventHandler
        });
        InMemoryPersistentSubscriptionsClient persistentSubscriptionsClient = new();
        CancellationToken cancellationToken = CancellationToken.None;
            
        var persistentSubscription =
            PersistentSubscription.Create(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver.Instance());

        await persistentSubscription.ConnectToSubscription(cancellationToken);

        persistentSubscription.Connected.ShouldBeTrue();

        String @event = "";

        //Manually add events.
        persistentSubscriptionsClient.WriteEvent(@event, "$", cancellationToken);

        //Crude - but a decent start point
        eventHandler.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public async Task PersistentSubscription_CanBeCreatedAndReceiveEventsMultipleEventHandler()
    {
        PersistentSubscriptionDetails persistentSubscriptionDetails = new("$ce-test", "local-1");
        TestDomainEventHandler eventHandler1 = new();
        TestDomainEventHandler eventHandler2 = new();
        IDomainEventHandlerResolverImposter domainEventHandlerResolver = new IDomainEventHandlerResolverImposter();
        domainEventHandlerResolver.GetDomainEventHandlers(Arg<IDomainEvent>.Any()).Returns(new List<IDomainEventHandler>()
        {
            eventHandler1,
            eventHandler2
        });
        InMemoryPersistentSubscriptionsClient persistentSubscriptionsClient = new();
        CancellationToken cancellationToken = CancellationToken.None;
            
        var persistentSubscription =
            PersistentSubscription.Create(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver.Instance());

        await persistentSubscription.ConnectToSubscription(cancellationToken);

        persistentSubscription.Connected.ShouldBeTrue();

        String @event = "{\r\n  \"estateId\": \"4fc2692f-067a-443e-8006-335bf2732248\",\r\n  \"estateName\": \"Demo Estate\"\r\n}\t";

        //Manually add events.
        persistentSubscriptionsClient.WriteEvent(@event, "EstateCreatedEvent", cancellationToken);

        //Crude - but a decent start point
        eventHandler1.DomainEvents.Count.ShouldBe(1);
        eventHandler2.DomainEvents.Count.ShouldBe(1);
    }


    [Fact]
    public async Task PersistentSubscription_FailedHandlerWithEmptyMessage_LogsUsefulMessageWithoutSyntheticException()
    {
        TestLogger loggerMock = new();
        Logger.Initialise(loggerMock);

        try
        {
            PersistentSubscriptionDetails persistentSubscriptionDetails = new("$ce-test", "local-1");
            IDomainEventHandlerResolverImposter domainEventHandlerResolver = new();
            IDomainEventHandlerImposter domainEventHandler = new();
            domainEventHandler.Handle(Arg<IDomainEvent>.Any(), Arg<CancellationToken>.Any())
                .ReturnsAsync(Result.Failure(new List<String>()));

            domainEventHandlerResolver.GetDomainEventHandlers(Arg<IDomainEvent>.Any())
                .Returns(new List<IDomainEventHandler>()
                {
                    domainEventHandler.Instance()
                });

            InMemoryPersistentSubscriptionsClient persistentSubscriptionsClient = new();
            PersistentSubscription persistentSubscription =
                PersistentSubscription.Create(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver.Instance());

            await persistentSubscription.ConnectToSubscription(CancellationToken.None);

            String @event = "{\r\n  \"estateId\": \"4fc2692f-067a-443e-8006-335bf2732248\",\r\n  \"estateName\": \"Demo Estate\"\r\n}\t";

            persistentSubscriptionsClient.WriteEvent(@event, "EstateCreatedEvent", CancellationToken.None);

            loggerMock.ErrorMessages.Count.ShouldBe(1);
            loggerMock.ErrorMessages[0].ShouldContain("Failed to process the event type");
            loggerMock.ErrorMessages[0].ShouldContain("Result was One or more event handlers have failed. Error Messages []");
            loggerMock.ErrorExceptions.Count.ShouldBe(0);
        }
        finally
        {
            Logger.Initialise(NullLogger.Instance);
        }
    }
    [Fact]
    public async Task PersistentSubscription_ConnectToSubscription_RetriesTransientUnavailableFailures()
    {
        this.InitialiseGrpcRetryConfiguration(maxAttempts: 3);

        PersistentSubscriptionDetails persistentSubscriptionDetails = new("$ce-test", "local-1");
        IDomainEventHandlerResolverImposter domainEventHandlerResolver = new();
        FlakyPersistentSubscriptionsClient persistentSubscriptionsClient = new(2);

        PersistentSubscription persistentSubscription =
            PersistentSubscription.Create(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver.Instance());

        await persistentSubscription.ConnectToSubscription(CancellationToken.None);

        persistentSubscription.Connected.ShouldBeTrue();
        persistentSubscriptionsClient.AttemptCount.ShouldBe(3);
    }

    private void InitialiseGrpcRetryConfiguration(int maxAttempts)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AppSettings:GrpcRetryMaxAttempts"] = maxAttempts.ToString(),
            ["AppSettings:GrpcRetryBaseDelayMilliseconds"] = "1",
            ["AppSettings:GrpcRetryMaxDelayMilliseconds"] = "1",
            ["AppSettings:GrpcRetryUseJitter"] = "false",
        });

        ConfigurationReader.Initialise(builder.Build());
    }

    private sealed class FlakyPersistentSubscriptionsClient : IPersistentSubscriptionsClient
    {
        private readonly int failuresBeforeSuccess;

        public FlakyPersistentSubscriptionsClient(int failuresBeforeSuccess)
        {
            this.failuresBeforeSuccess = failuresBeforeSuccess;
        }

        public int AttemptCount { get; private set; }

        public Task<KurrentDB.Client.PersistentSubscription> SubscribeAsync(String stream,
                                                                             String group,
                                                                             Func<KurrentDB.Client.PersistentSubscription, ResolvedEvent, Int32?,
                                                                                 CancellationToken, Task> eventAppeared,
                                                                             Action<KurrentDB.Client.PersistentSubscription, SubscriptionDroppedReason,
                                                                                 Exception?> subscriptionDropped,
                                                                             UserCredentials? userCredentials,
                                                                             Int32 bufferSize,
                                                                             CancellationToken cancellationToken)
        {
            this.AttemptCount++;

            if (this.AttemptCount <= this.failuresBeforeSuccess)
            {
                throw new RpcException(new Status(StatusCode.Unavailable, "temporary transport failure"));
            }

            return Task.FromResult(default(KurrentDB.Client.PersistentSubscription));
        }
    }

    public void Dispose()
    {
        ConfigurationReader.Initialise(new ConfigurationRoot(new List<IConfigurationProvider>()));
    }
}
