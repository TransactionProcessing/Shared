using Shared.EventStore.Tests.TestObjects;

namespace Shared.EventStore.Tests;

using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;
using Imposter.Abstractions;
using ProjectionEngine;
using Xunit;

public class ProjectionHandlerTests{
    [Fact]
    public async Task ProjectionHandler_Handle_EventHandled(){
        TestState originalState = new();
        TestState updatedState = new() {
            Name = "Test Name"
        };

        TestProjectionStateRepository projectionStateRepository = new();
        projectionStateRepository.LoadHandler = (_, _) => Task.FromResult(originalState);
        TestProjection projection = new();
        projection.ShouldHandleHandler = _ => true;
        projection.HandleHandler = (_, _, _) => Task.FromResult(updatedState);
        TestStateDispatcher dispatcher = new();
        ProjectionHandler<TestState> handler = new(projectionStateRepository,
            projection,
            dispatcher);

        AggregateNameSetEvent @event = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        await handler.Handle(@event, CancellationToken.None);
    }

    [Fact]
    public async Task ProjectionHandler_Handle_StateNotChanged_EventHandled()
    {
        TestState originalState = new();

        TestProjectionStateRepository projectionStateRepository = new();
        projectionStateRepository.LoadHandler = (_, _) => Task.FromResult(originalState);
        TestProjection projection = new();
        projection.ShouldHandleHandler = _ => true;
        projection.HandleHandler = (_, _, _) => Task.FromResult(originalState);
        TestStateDispatcher dispatcher = new();
        ProjectionHandler<TestState> handler = new(projectionStateRepository,
            projection,
            dispatcher);

        AggregateNameSetEvent @event = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        await handler.Handle(@event, CancellationToken.None);
    }

    [Fact]
    public async Task ProjectionHandler_Handle_TraceHandlerSet_EventHandled()
    {
        TestState originalState = new();

        TestProjectionStateRepository projectionStateRepository = new();
        projectionStateRepository.LoadHandler = (_, _) => Task.FromResult(originalState);
        TestProjection projection = new();
        projection.ShouldHandleHandler = _ => true;
        projection.HandleHandler = (_, _, _) => Task.FromResult(originalState);
        TestStateDispatcher dispatcher = new();
        ProjectionHandler<TestState> handler = new(projectionStateRepository,
            projection,
            dispatcher);

        AggregateNameSetEvent @event = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        await handler.Handle(@event, CancellationToken.None);
    }


    [Fact]
    public async Task ProjectionHandler_Handle_NullEvent_EventHandled()
    {
        TestProjectionStateRepository projectionStateRepository = new TestProjectionStateRepository();
        TestProjection projection = new TestProjection();
        TestStateDispatcher dispatcher = new TestStateDispatcher();
        ProjectionHandler<TestState> handler = new ProjectionHandler<TestState>(projectionStateRepository,
            projection,
            dispatcher);

        AggregateNameSetEvent @event = null;
        await handler.Handle(@event, CancellationToken.None);
    }

    [Fact]
    public async Task ProjectionHandler_Handle_EventNotHandled_EventHandled()
    {
        TestProjectionStateRepository projectionStateRepository = new();
        TestProjection projection = new();
        projection.ShouldHandleHandler = _ => false;
        TestStateDispatcher dispatcher = new();
        ProjectionHandler<TestState> handler = new(projectionStateRepository,
            projection,
            dispatcher);

        AggregateNameSetEvent @event = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        await handler.Handle(@event, CancellationToken.None);
    }

    [Fact]
    public async Task ProjectionHandler_Handle_NullState_EventHandled(){
        TestState originalState = null;

        TestProjectionStateRepository projectionStateRepository = new();
        projectionStateRepository.LoadHandler = (_, _) => Task.FromResult(originalState);
        TestProjection projection = new();
        projection.ShouldHandleHandler = _ => true;
        TestStateDispatcher dispatcher = new();
        ProjectionHandler<TestState> handler = new(projectionStateRepository,
            projection,
            dispatcher);

        AggregateNameSetEvent @event = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        await handler.Handle(@event, CancellationToken.None);
    }
}
