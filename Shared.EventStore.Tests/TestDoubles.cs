using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shared.DomainDrivenDesign.EventSourcing;
using Shared.EventStore.Aggregate;
using Shared.EventStore.ProjectionEngine;
using Shared.EventStore.Tests.TestObjects;
using SimpleResults;
using Shared.Logger;

namespace Shared.EventStore.Tests;

internal sealed class TestAggregateRepository : IAggregateRepository<TestAggregate, DomainEvent>
{
    public Func<Guid, CancellationToken, Task<Result<TestAggregate>>> GetLatestVersionHandler { get; set; }
    public Func<TestAggregate, CancellationToken, Task<Result>> SaveChangesHandler { get; set; }
    public Func<Guid, CancellationToken, Task<Result<TestAggregate>>> GetLatestVersionFromLastEventHandler { get; set; }

    public Task<Result<TestAggregate>> GetLatestVersion(Guid aggregateId, CancellationToken cancellationToken) =>
        GetLatestVersionHandler?.Invoke(aggregateId, cancellationToken) ?? Task.FromResult<Result<TestAggregate>>(default);

    public Task<Result> SaveChanges(TestAggregate aggregate, CancellationToken cancellationToken) =>
        SaveChangesHandler?.Invoke(aggregate, cancellationToken) ?? Task.FromResult(Result.Success());

    public Task<Result<TestAggregate>> GetLatestVersionFromLastEvent(Guid aggregateId, CancellationToken cancellationToken) =>
        GetLatestVersionFromLastEventHandler?.Invoke(aggregateId, cancellationToken) ?? Task.FromResult<Result<TestAggregate>>(default);
}

internal sealed class TestProjectionStateRepository : IProjectionStateRepository<TestState>
{
    public Func<IDomainEvent, CancellationToken, Task<TestState>> LoadHandler { get; set; }
    public Task<TestState> Load(IDomainEvent @event, CancellationToken cancellationToken) => LoadHandler?.Invoke(@event, cancellationToken);
    public Task<TestState> Load(Guid estateId, Guid stateId, CancellationToken cancellationToken) => Task.FromResult<TestState>(null);
    public Task<TestState> Save(TestState state, IDomainEvent @event, CancellationToken cancellationToken) => Task.FromResult(state);
}

internal sealed class TestProjection : IProjection<TestState>
{
    public Func<IDomainEvent, bool> ShouldHandleHandler { get; set; }
    public Func<TestState, IDomainEvent, CancellationToken, Task<TestState>> HandleHandler { get; set; }
    public bool ShouldIHandleEvent(IDomainEvent domainEvent) => ShouldHandleHandler?.Invoke(domainEvent) ?? false;
    public Task<TestState> Handle(TestState state, IDomainEvent domainEvent, CancellationToken cancellationToken) => HandleHandler?.Invoke(state, domainEvent, cancellationToken);
}

internal sealed class TestStateDispatcher : IStateDispatcher<TestState>
{
    public Task Dispatch(TestState state, IDomainEvent @event, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class TestLogger : ILogger
{
    public bool IsInitialised { get; set; }
    public List<string> ErrorMessages { get; } = new();
    public List<Exception> ErrorExceptions { get; } = new();
    public void LogCritical(Exception exception) { }
    public void LogCritical(string message, Exception exception) { }
    public void LogDebug(string message) { }
    public void LogError(Exception exception) => ErrorExceptions.Add(exception);
    public void LogError(string message) => ErrorMessages.Add(message);
    public void LogError(string message, Exception exception) => ErrorMessages.Add(message);
    public void LogInformation(string message) { }
    public void LogTrace(string message) { }
    public void LogWarning(string message) { }
}
