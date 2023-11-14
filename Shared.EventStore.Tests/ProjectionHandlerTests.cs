namespace Shared.EventStore.Tests
{
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;
    using Moq;
    using ProjectionEngine;
    using Shared.EventStore.Tests.TestObjects;
    using Xunit;

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
            handler.TraceGenerated += this.Handler_TraceGenerated;

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
