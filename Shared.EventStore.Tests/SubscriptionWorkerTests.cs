namespace Shared.EventStore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

    public class SubscriptionWorkerTests
    {
        #region Fields

        private readonly List<PersistentSubscriptionInfo> AllSubscriptions;

        private readonly Mock<IDomainEventHandlerResolver> domainEventHandlerResolver;

        private readonly String EventStoreConnectionString;

        private readonly Func<CancellationToken, Task<List<PersistentSubscriptionInfo>>> getAllSubscriptions;

        private readonly List<IDomainEventHandler> projectionEventHandlers;

        private readonly ISubscriptionRepository SubscriptionRepository;

        #endregion

        #region Constructors

        public SubscriptionWorkerTests()
        {
            Logger.Initialise(NullLogger.Instance);

            this.domainEventHandlerResolver = new();
            
            this.AllSubscriptions = (TestData.GetPersistentSubscriptions_DemoEstate());

            this.getAllSubscriptions = async _ => this.AllSubscriptions;
            this.SubscriptionRepository = Shared.EventStore.SubscriptionWorker.SubscriptionRepository.Create(this.getAllSubscriptions);

            TypeMap.AddType<EstateCreatedEvent>("EstateCreatedEvent");

            this.EventStoreConnectionString = "esdb://admin:changeit@127.0.0.1:2113?tls=false&tlsVerifyCert=false";
        }

        #endregion

        #region Methods

        [Fact]
        public void SubscriptionWorker_CanBeCreated_IsCreated()
        {
            SubscriptionWorker sw = SubscriptionWorker.CreateConcurrentSubscriptionWorker(this.EventStoreConnectionString,
                                                                                          this.domainEventHandlerResolver.Object,
                                                                                          this.SubscriptionRepository).UseInMemory();

            sw.ShouldNotBeNull();
            sw.InflightMessages.ShouldBe(200);
            sw.PersistentSubscriptionPollingInSeconds.ShouldBe(60);
        }

        [Fact]
        public async Task SubscriptionWorker_CanBeCreatedAndReceiveEvents()
        {
            CancellationToken cancellationToken = CancellationToken.None;
            TestDomainEventHandler eventHandler1 = new();
            Int32 inflight = 200;
            Int32 pollingInSeconds = 60;
            this.domainEventHandlerResolver.Setup(d => d.GetDomainEventHandlers(It.IsAny<IDomainEvent>())).Returns(new List<IDomainEventHandler>()
                {
                    eventHandler1
                });

            SubscriptionWorker sw = SubscriptionWorker.CreateConcurrentSubscriptionWorker(this.EventStoreConnectionString,
                                                                                          this.domainEventHandlerResolver.Object,
                                                                                          this.SubscriptionRepository,
                                                                                          inflight,
                                                                                          pollingInSeconds).UseInMemory();

            await sw.StartAsync(cancellationToken);

            //Give our service time to run
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            sw.IsRunning.ShouldBeTrue();

            PersistentSubscription ps = sw.GetPersistentSubscription()[0];

            String @event = "{\r\n  \"estateId\": \"4fc2692f-067a-443e-8006-335bf2732248\",\r\n  \"estateName\": \"Demo Estate\"\r\n}\t";

            ps.Connected.ShouldBeTrue();
            ps.PersistentSubscriptionDetails.InflightMessages.ShouldBe(inflight);

            //Manually add events.
            ((InMemoryPersistentSubscriptionsClient)ps.PersistentSubscriptionsClient).WriteEvent(@event, "EstateCreatedEvent", cancellationToken);

            eventHandler1.DomainEvents.Count.ShouldBe(1);
        }

        [Fact]
        public async Task SubscriptionWorker_CanBeStartedAndStopped()
        {
            CancellationToken cancellationToken = CancellationToken.None;
            SubscriptionWorker sw = SubscriptionWorker.CreateConcurrentSubscriptionWorker(this.EventStoreConnectionString,
                                                                                          this.domainEventHandlerResolver.Object,
                                                                                          this.SubscriptionRepository).UseInMemory();

            await sw.StartAsync(cancellationToken);

            //Give our service time to run
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            sw.IsRunning.ShouldBeTrue();

            await sw.StopAsync(cancellationToken);

            sw.IsRunning.ShouldBe(false);

            var stillConnected = sw.GetPersistentSubscription().Count;

            stillConnected.ShouldBe(0);
        }

        [Fact]
        public async Task SubscriptionWorker_ConnectionDroppedAndIsRestablished()
        {
            CancellationToken cancellationToken = CancellationToken.None;

            SubscriptionWorker sw = SubscriptionWorker.CreateConcurrentSubscriptionWorker(this.EventStoreConnectionString,
                                                                                          this.domainEventHandlerResolver.Object,
                                                                                          this.SubscriptionRepository,
                                                                                          200,
                                                                                          1).UseInMemory();

            await sw.StartAsync(cancellationToken);

            //Give our service time to run
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            sw.IsRunning.ShouldBeTrue();

            var connected = sw.GetPersistentSubscription();
            var originalCount = connected.Count;

            //Drop the connection
            connected[0].SubscriptionDropped("Test");

            var stillConnected = sw.GetPersistentSubscription();

            stillConnected.Count().ShouldBe(originalCount - 1); //One less than original

            //Wait for n seconds on reconnect
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            stillConnected = sw.GetPersistentSubscription();
            stillConnected.Count().ShouldBe(originalCount); //One less than original
        }

        #endregion
    }
}