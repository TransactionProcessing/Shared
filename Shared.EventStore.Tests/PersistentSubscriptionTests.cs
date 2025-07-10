using Microsoft.Extensions.Configuration;
using Shared.EventStore.Tests.TestObjects;

namespace Shared.EventStore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;
    using EventHandling;
    using General;
    using Logger;
    using Moq;
    using Shouldly;
    using SubscriptionWorker;
    using Xunit;

    public class PersistentSubscriptionTests
    {
        public PersistentSubscriptionTests()
        {
            Logger.Initialise(NullLogger.Instance);
            ConfigurationReader.Initialise(new ConfigurationRoot(new List<IConfigurationProvider>()));

            TypeMap.AddType<EstateCreatedEvent>("EstateCreatedEvent");
        }

        [Fact]
        public async Task PersistentSubscription_CanBeCreatedAndReceiveEventsSingleEventHandler()
        {
            PersistentSubscriptionDetails persistentSubscriptionDetails = new("$ce-test", "local-1");
            TestDomainEventHandler eventHandler = new();
            Mock<IDomainEventHandlerResolver> domainEventHandlerResolver = new Mock<IDomainEventHandlerResolver>();
            domainEventHandlerResolver.Setup(d => d.GetDomainEventHandlers(It.IsAny<IDomainEvent>())).Returns(new List<IDomainEventHandler>()
                                                                                                              {
                                                                                                                  eventHandler
                                                                                                              });
            
            InMemoryPersistentSubscriptionsClient persistentSubscriptionsClient = new();
            CancellationToken cancellationToken = CancellationToken.None;

            

            var persistentSubscription = PersistentSubscription.Create(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver.Object);

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
            Mock<IDomainEventHandlerResolver> domainEventHandlerResolver = new Mock<IDomainEventHandlerResolver>();
            domainEventHandlerResolver.Setup(d => d.GetDomainEventHandlers(It.IsAny<IDomainEvent>())).Returns(new List<IDomainEventHandler>()
                                                                                                              {
                                                                                                                  eventHandler
                                                                                                              });
            InMemoryPersistentSubscriptionsClient persistentSubscriptionsClient = new();
            CancellationToken cancellationToken = CancellationToken.None;
            
            var persistentSubscription =
                PersistentSubscription.Create(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver.Object);

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
            Mock<IDomainEventHandlerResolver> domainEventHandlerResolver = new Mock<IDomainEventHandlerResolver>();
            domainEventHandlerResolver.Setup(d => d.GetDomainEventHandlers(It.IsAny<IDomainEvent>())).Returns(new List<IDomainEventHandler>()
                                                                                                              {
                                                                                                                  eventHandler1,
                                                                                                                  eventHandler2
                                                                                                              });
            InMemoryPersistentSubscriptionsClient persistentSubscriptionsClient = new();
            CancellationToken cancellationToken = CancellationToken.None;
            
            var persistentSubscription =
                PersistentSubscription.Create(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver.Object);

            await persistentSubscription.ConnectToSubscription(cancellationToken);

            persistentSubscription.Connected.ShouldBeTrue();

            String @event = "{\r\n  \"estateId\": \"4fc2692f-067a-443e-8006-335bf2732248\",\r\n  \"estateName\": \"Demo Estate\"\r\n}\t";

            //Manually add events.
            persistentSubscriptionsClient.WriteEvent(@event, "EstateCreatedEvent", cancellationToken);

            //Crude - but a decent start point
            eventHandler1.DomainEvents.Count.ShouldBe(1);
            eventHandler2.DomainEvents.Count.ShouldBe(1);
        }
    }
}