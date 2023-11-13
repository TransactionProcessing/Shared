using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.EventStore.Tests
{
    using System.Diagnostics;
    using System.Reflection.Metadata;
    using System.Threading;
    using DomainDrivenDesign.EventSourcing;
    using EventHandling;
    using Extensions;
    using global::EventStore.Client;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using ProjectionEngine;
    using Shared.Tests;
    using Shouldly;
    using SubscriptionWorker;
    using Xunit;

    public class EventStoreHealthCheckBuilderExtensionsTests
    {
        [Fact]
        public void AddEventStore_HealthCheckAdded(){
            IHealthChecksBuilder builder = new TestHealthChecksBuilder();
            builder.AddEventStore(new EventStoreClientSettings(), null, null, null);
        }
    }

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
            subscriptionWorkersRoot.InternalSubscriptionService = true;
            subscriptionWorkersRoot.SubscriptionWorkers = new List<SubscriptionWorkerConfig>();

            Mock<ISubscriptionRepository> subscriptionRepository = new Mock<ISubscriptionRepository>();
            this.subscriptionRepositoryResolver = (s, i) => subscriptionRepository.Object;

            this.eventHandlerResolvers = new Dictionary<String, IDomainEventHandlerResolver>();
            this.domainEventHandlerResolver = new Mock<IDomainEventHandlerResolver>();
            
        }

        [Fact]
        public async Task ConfigureSubscriptionService_IsOrdered_ConfiguredSuccessfully(){
            
            subscriptionWorkersRoot.SubscriptionWorkers.Add(new SubscriptionWorkerConfig{
                                                                                            Enabled = true,
                                                                                            IsOrdered = true
                                                                                        });
            eventHandlerResolvers.Add("Ordered", domainEventHandlerResolver.Object);

            await this.builder.ConfigureSubscriptionService(subscriptionWorkersRoot,
                                                            "esdb://admin:changeit@127.0.0.1:2113?tls=true&tlsVerifyCert=false",
                                                            new EventStoreClientSettings(),
                                                            eventHandlerResolvers,
                                                            TraceHandler, 
                                                            this.subscriptionRepositoryResolver,
                                                            CancellationToken.None);
        }

        [Fact]
        public async Task ConfigureSubscriptionService_IsConcurrent_ConfiguredSuccessfully()
        {
            subscriptionWorkersRoot.SubscriptionWorkers.Add(new SubscriptionWorkerConfig
            {
                Enabled = true,
                IsOrdered = false,
                InstanceCount = 1
            });
            eventHandlerResolvers.Add("Main", domainEventHandlerResolver.Object);

            await this.builder.ConfigureSubscriptionService(subscriptionWorkersRoot,
                                                            "esdb://admin:changeit@127.0.0.1:2113?tls=true&tlsVerifyCert=false",
                                                            new EventStoreClientSettings(),
                                                            eventHandlerResolvers,
                                                            TraceHandler,
                                                            this.subscriptionRepositoryResolver,
                                                            CancellationToken.None);
        }

        
        [Fact]
        public async Task ConfigureSubscriptionService_NullWorkerConfig_ErrorThrown(){
            this.subscriptionWorkersRoot = null;
            Should.Throw<Exception>(async () => {
                                        await builder.ConfigureSubscriptionService(subscriptionWorkersRoot,
                                                                                   "",
                                                                                   new EventStoreClientSettings(),
                                                                                   eventHandlerResolvers,
                                                                                   TraceHandler,
                                                                                   this.subscriptionRepositoryResolver,
                                                                                   CancellationToken.None);
                                    });
        }

        [Fact]
        public async Task ConfigureSubscriptionService_NullSubscriptionRepositoryResolver_ErrorThrown(){
            this.subscriptionRepositoryResolver = null;
            Should.Throw<Exception>(async () => {
                                        await builder.ConfigureSubscriptionService(subscriptionWorkersRoot,
                                                                                   "",
                                                                                   new EventStoreClientSettings(),
                                                                                   eventHandlerResolvers,
                                                                                   TraceHandler,
                                                                                   this.subscriptionRepositoryResolver,
                                                                                   CancellationToken.None);
                                    });
        }

        [Fact]
        public async Task ConfigureSubscriptionService_NullSubscriptionWorkersList_ErrorThrown()
        {
            subscriptionWorkersRoot.SubscriptionWorkers = null;
         
            Should.Throw<Exception>(async () => {
                                        await builder.ConfigureSubscriptionService(subscriptionWorkersRoot,
                                                                                   "",
                                                                                   new EventStoreClientSettings(),
                                                                                   eventHandlerResolvers,
                                                                                   TraceHandler,
                                                                                   this.subscriptionRepositoryResolver,
                                                                                   CancellationToken.None);
                                    });
        }

        [Fact]
        public async Task ConfigureSubscriptionService_EmptySubscriptionWorkersList_ErrorThrown()
        {
            subscriptionWorkersRoot.SubscriptionWorkers = new List<SubscriptionWorkerConfig>();

            Should.Throw<Exception>(async () => {
                                        await builder.ConfigureSubscriptionService(subscriptionWorkersRoot,
                                                                                   "",
                                                                                   new EventStoreClientSettings(),
                                                                                   eventHandlerResolvers,
                                                                                   TraceHandler,
                                                                                   this.subscriptionRepositoryResolver,
                                                                                   CancellationToken.None);
                                    });
        }

        [Fact]
        public async Task ConfigureSubscriptionService_SubscriptionWorkersNotEnabled_NoErrorThrown(){

            subscriptionWorkersRoot.SubscriptionWorkers.Add(new SubscriptionWorkerConfig
                                                            {
                                                                Enabled = false
                                                            });

            Should.NotThrow(async () => {
                                        await builder.ConfigureSubscriptionService(subscriptionWorkersRoot,
                                                                                   "",
                                                                                   new EventStoreClientSettings(),
                                                                                   eventHandlerResolvers,
                                                                                   TraceHandler,
                                                                                   this.subscriptionRepositoryResolver,
                                                                                   CancellationToken.None);
                                    });
        }

        [Fact]
        public async Task ConfigureSubscriptionService_InternalSubscriptionService_Off_NoError()
        {
            subscriptionWorkersRoot.InternalSubscriptionService = false;
            await builder.ConfigureSubscriptionService(subscriptionWorkersRoot,
                                                       "",
                                                       new EventStoreClientSettings(),
                                                       eventHandlerResolvers,
                                                       TraceHandler,
                                                       this.subscriptionRepositoryResolver,
                                                       CancellationToken.None);
        }

        private void TraceHandler(TraceEventType arg1, String arg2, String arg3){
            
        }
    }

    public class ServiceCollectionExtensionsTests{
        [Fact]
        public async Task ServiceCollectionExtensions_AddEventStoreProjectionManagerClient(){
        }
    }

    public class EventDispatchHelperTests{
        [Fact]
        public async Task EventDispatchHelper_DispatchToHandlers_AllSuccessful(){
            AggregateNameSetEvent @event = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            List<IDomainEventHandler> handlers = new List<IDomainEventHandler>();
            handlers.Add(new TestDomainEventHandler());
            Should.NotThrow(async () => {
                                await @event.DispatchToHandlers(handlers, CancellationToken.None);
                            });
        }

        [Fact]
        public async Task EventDispatchHelper_DispatchToHandlers_HandlerThrowsException_ErrorThrown()
        {
            AggregateNameSetEvent @event = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            List<IDomainEventHandler> handlers = new List<IDomainEventHandler>();
            Mock<IDomainEventHandler> domainEventHandler = new Mock<IDomainEventHandler>();
            domainEventHandler.Setup(s => s.Handle(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>())).Throws<Exception>();
            handlers.Add(domainEventHandler.Object);
            Should.Throw<Exception>(async () => {
                                await @event.DispatchToHandlers(handlers, CancellationToken.None);
                            });
        }
    }

    public record TestState : State{
        public String Name{ get; set; }
    }

    public class ProjectionHandlerTests{
        [Fact]
        public async Task ProjectionHandler_Handle_EventHandled(){
            TestState originalState = new TestState();
            TestState updatedState = new TestState{
                                                      Name = "Test Name"
                                                  };

            Mock<IProjectionStateRepository<TestState>> projectionStateRepository = new Mock<IProjectionStateRepository<TestState>>();
            projectionStateRepository.Setup(p => p.Load(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(originalState);
            Mock<IProjection<TestState>> projection = new Mock<IProjection<TestState>>();
            projection.Setup(p => p.ShouldIHandleEvent(It.IsAny<IDomainEvent>())).Returns(true);
            projection.Setup(p => p.Handle(It.IsAny<TestState>(), It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(updatedState);
            Mock<IStateDispatcher<TestState>> dispatcher = new Mock<IStateDispatcher<TestState>>();
            ProjectionHandler<TestState> handler = new ProjectionHandler<TestState>(projectionStateRepository.Object,
                                                                                    projection.Object,
                                                                                    dispatcher.Object);

            AggregateNameSetEvent @event = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            await handler.Handle(@event, CancellationToken.None);
        }

        [Fact]
        public async Task ProjectionHandler_Handle_StateNotChanged_EventHandled()
        {
            TestState originalState = new TestState();

            Mock<IProjectionStateRepository<TestState>> projectionStateRepository = new Mock<IProjectionStateRepository<TestState>>();
            projectionStateRepository.Setup(p => p.Load(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(originalState);
            Mock<IProjection<TestState>> projection = new Mock<IProjection<TestState>>();
            projection.Setup(p => p.ShouldIHandleEvent(It.IsAny<IDomainEvent>())).Returns(true);
            projection.Setup(p => p.Handle(It.IsAny<TestState>(), It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(originalState);
            Mock<IStateDispatcher<TestState>> dispatcher = new Mock<IStateDispatcher<TestState>>();
            ProjectionHandler<TestState> handler = new ProjectionHandler<TestState>(projectionStateRepository.Object,
                                                                                    projection.Object,
                                                                                    dispatcher.Object);

            AggregateNameSetEvent @event = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            await handler.Handle(@event, CancellationToken.None);
        }

        [Fact]
        public async Task ProjectionHandler_Handle_TraceHandlerSet_EventHandled()
        {
            TestState originalState = new TestState();

            Mock<IProjectionStateRepository<TestState>> projectionStateRepository = new Mock<IProjectionStateRepository<TestState>>();
            projectionStateRepository.Setup(p => p.Load(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(originalState);
            Mock<IProjection<TestState>> projection = new Mock<IProjection<TestState>>();
            projection.Setup(p => p.ShouldIHandleEvent(It.IsAny<IDomainEvent>())).Returns(true);
            projection.Setup(p => p.Handle(It.IsAny<TestState>(), It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(originalState);
            Mock<IStateDispatcher<TestState>> dispatcher = new Mock<IStateDispatcher<TestState>>();
            ProjectionHandler<TestState> handler = new ProjectionHandler<TestState>(projectionStateRepository.Object,
                                                                                    projection.Object,
                                                                                    dispatcher.Object);
            handler.TraceGenerated += Handler_TraceGenerated;

            AggregateNameSetEvent @event = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            await handler.Handle(@event, CancellationToken.None);
        }

        private void Handler_TraceGenerated(object sender, string e)
        {
            
            
        }

        [Fact]
        public async Task ProjectionHandler_Handle_NullEvent_EventHandled()
        {
            Mock<IProjectionStateRepository<TestState>> projectionStateRepository = new Mock<IProjectionStateRepository<TestState>>();
            Mock<IProjection<TestState>> projection = new Mock<IProjection<TestState>>();
            Mock<IStateDispatcher<TestState>> dispatcher = new Mock<IStateDispatcher<TestState>>();
            ProjectionHandler<TestState> handler = new ProjectionHandler<TestState>(projectionStateRepository.Object,
                                                                                    projection.Object,
                                                                                    dispatcher.Object);

            AggregateNameSetEvent @event = null;
            await handler.Handle(@event, CancellationToken.None);
        }

        [Fact]
        public async Task ProjectionHandler_Handle_EventNotHandled_EventHandled()
        {
            TestState originalState = new TestState();
            TestState updatedState = new TestState
                                     {
                                         Name = "Test Name"
                                     };

            Mock<IProjectionStateRepository<TestState>> projectionStateRepository = new Mock<IProjectionStateRepository<TestState>>();
            Mock<IProjection<TestState>> projection = new Mock<IProjection<TestState>>();
            projection.Setup(p => p.ShouldIHandleEvent(It.IsAny<IDomainEvent>())).Returns(false);
            Mock<IStateDispatcher<TestState>> dispatcher = new Mock<IStateDispatcher<TestState>>();
            ProjectionHandler<TestState> handler = new ProjectionHandler<TestState>(projectionStateRepository.Object,
                                                                                    projection.Object,
                                                                                    dispatcher.Object);

            AggregateNameSetEvent @event = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            await handler.Handle(@event, CancellationToken.None);
        }

        [Fact]
        public async Task ProjectionHandler_Handle_NullState_EventHandled(){
            TestState originalState = null;

            Mock<IProjectionStateRepository<TestState>> projectionStateRepository = new Mock<IProjectionStateRepository<TestState>>();
            projectionStateRepository.Setup(p => p.Load(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(originalState);
            Mock<IProjection<TestState>> projection = new Mock<IProjection<TestState>>();
            projection.Setup(p => p.ShouldIHandleEvent(It.IsAny<IDomainEvent>())).Returns(true);
            Mock<IStateDispatcher<TestState>> dispatcher = new Mock<IStateDispatcher<TestState>>();
            ProjectionHandler<TestState> handler = new ProjectionHandler<TestState>(projectionStateRepository.Object,
                                                                                    projection.Object,
                                                                                    dispatcher.Object);

            AggregateNameSetEvent @event = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
            await handler.Handle(@event, CancellationToken.None);
        }
    }
}
