using System;
using System.Threading.Tasks;
using Shared.DomainDrivenDesign.EventSourcing;
using Shared.EventStore.Aggregate;
using Shared.EventStore.Tests.TestObjects;
using Shared.Logger;
using Microsoft.Extensions.Caching.Memory;
using Imposter.Abstractions;
using Shouldly;
using SimpleResults;
using System.Threading;
using Xunit;

namespace Shared.EventStore.Tests;

public class AggregateServiceTests {
    private readonly IAggregateRepositoryResolverImposter _repositoryResolverMock;
    private readonly IMemoryCache _memoryCache;
    private readonly AggregateService _aggregateService;

    public AggregateServiceTests() {
        _repositoryResolverMock = new IAggregateRepositoryResolverImposter();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _aggregateService = new AggregateService(_repositoryResolverMock.Instance(), _memoryCache);
        Logger.Logger.Initialise(new NullLogger());
    
    }

    [Fact]
    public async Task Get_ShouldReturnAggregateFromCache_WhenAggregateIsCached() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestAggregate { AggregateId = aggregateId };
        var cacheKey = $"TestAggregate-{aggregateId}";
        this._aggregateService.AddCachedAggregate(typeof(TestAggregate));

        this._memoryCache.Set(cacheKey, aggregate);

        // Act
        var result = await _aggregateService.Get<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(aggregate);
    }

    [Fact]
    public async Task Get_ShouldReturnAggregateFromRepository_WhenNotInCache_AndNotSetToCache() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestAggregate { AggregateId = aggregateId };
        var repositoryMock = new TestAggregateRepository();

        repositoryMock.GetLatestVersionHandler = (_, _) => Task.FromResult(Result.Success(aggregate));

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        // Act
        var result = await _aggregateService.Get<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(aggregate);
    }

    [Fact]
    public async Task Get_ShouldReturnAggregateFromRepository_WhenNotInCache_AndSetToCache_ItemIsCached() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestAggregate { AggregateId = aggregateId };
        var repositoryMock = new TestAggregateRepository();
        var cacheKey = $"TestAggregate-{aggregateId}";
        this._aggregateService.AddCachedAggregate(typeof(TestAggregate));

        repositoryMock.GetLatestVersionHandler = (_, _) => Task.FromResult(Result.Success(aggregate));

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        // Act
        var result = await _aggregateService.Get<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(aggregate);
        _memoryCache.TryGetValue(cacheKey, out var cachedAggregate).ShouldBeTrue();
    }

    [Fact]
    public async Task Get_ShouldReturnAggregateFromRepository_GetLatestFails_FailedResultReturned() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var repositoryMock = new TestAggregateRepository();

        repositoryMock.GetLatestVersionHandler = (_, _) => Task.FromResult<Result<TestAggregate>>(Result.Failure("Error getting latest"));

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        // Act
        var result = await _aggregateService.Get<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_ShouldReturnAggregateFromRepository_GetLatestThrowsException_FailedResultReturned() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var repositoryMock = new TestAggregateRepository();

        repositoryMock.GetLatestVersionHandler = (_, _) => Task.FromException<Result<TestAggregate>>(new Exception("Exception Message"));

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        // Act
        var result = await _aggregateService.Get<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Message.ShouldBe("Exception Message");
    }

    [Fact]
    public async Task Save_ShouldSaveAggregateToRepository_NoCaching() {
        // Arrange
        var aggregate = new TestAggregate { AggregateId = Guid.NewGuid() };
        aggregate.SetAggregateName("1", Guid.NewGuid());
        var repositoryMock = new TestAggregateRepository();

        repositoryMock.SaveChangesHandler = (_, _) => Task.FromResult(Result.Success());

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        // Act
        var result = await _aggregateService.Save(aggregate, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        repositoryMock.SaveChangesInvocations.Count.ShouldBe(1);
        repositoryMock.SaveChangesInvocations[0].Aggregate.ShouldBeSameAs(aggregate);
    }

    [Fact]
    public async Task Save_ShouldSaveAggregateToRepository_AndCacheIt() {
        // Arrange
        var aggregate = new TestAggregate { AggregateId = Guid.NewGuid() };
        aggregate.SetAggregateName("1", Guid.NewGuid());
        var repositoryMock = new TestAggregateRepository();
        this._aggregateService.AddCachedAggregate(typeof(TestAggregate));

        repositoryMock.SaveChangesHandler = (_, _) => Task.FromResult(Result.Success());

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        // Act
        var result = await _aggregateService.Save(aggregate, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        repositoryMock.SaveChangesInvocations.Count.ShouldBe(1);
        repositoryMock.SaveChangesInvocations[0].Aggregate.ShouldBeSameAs(aggregate);
    }

    [Fact]
    public async Task Save_ShouldSaveAggregateToRepository_NoChanges_SaveNotCalled()
    {
        // Arrange
        var aggregate = new TestAggregate { AggregateId = Guid.NewGuid() };
        var repositoryMock = new TestAggregateRepository();
        this._aggregateService.AddCachedAggregate(typeof(TestAggregate));

        repositoryMock.SaveChangesHandler = (_, _) => Task.FromResult(Result.Success());

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        // Act
        var result = await _aggregateService.Save(aggregate, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        repositoryMock.SaveChangesInvocations.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Save_ShouldSaveAggregateToRepository_AndCacheIt_SecondSave_UpdatesCache() {
        // Arrange
        var aggregate = new TestAggregate { AggregateId = Guid.NewGuid() };
        aggregate.SetAggregateName("1", Guid.NewGuid());
        var repositoryMock = new TestAggregateRepository();
        this._aggregateService.AddCachedAggregate(typeof(TestAggregate));

        repositoryMock.SaveChangesHandler = (_, _) => Task.FromResult(Result.Success());

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        // Act
        var result = await _aggregateService.Save(aggregate, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        repositoryMock.SaveChangesInvocations.Count.ShouldBe(1);
        repositoryMock.SaveChangesInvocations[0].Aggregate.ShouldBeSameAs(aggregate);

        _memoryCache.TryGetValue($"TestAggregate-{aggregate.AggregateId}", out TestAggregate cachedAggregate).ShouldBeTrue();
        cachedAggregate.AggregateName.ShouldBe("1");

        aggregate.SetAggregateName("2", Guid.NewGuid());
        result = await _aggregateService.Save(aggregate, CancellationToken.None);

        repositoryMock.SaveChangesInvocations.Count.ShouldBe(2);
        repositoryMock.SaveChangesInvocations[1].Aggregate.ShouldBeSameAs(aggregate);

        _memoryCache.TryGetValue($"TestAggregate-{aggregate.AggregateId}", out cachedAggregate).ShouldBeTrue();
        cachedAggregate.AggregateName.ShouldBe("2");
    }

    [Fact]
    public async Task Save_ShouldSaveAggregateToRepository_SaveFails() {
        // Arrange
        var aggregate = new TestAggregate { AggregateId = Guid.NewGuid() };
        aggregate.SetAggregateName("1", Guid.NewGuid());
        var repositoryMock = new TestAggregateRepository();

        repositoryMock.SaveChangesHandler = (_, _) => Task.FromResult(Result.Failure());

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        // Act
        var result = await _aggregateService.Save(aggregate, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
        repositoryMock.SaveChangesInvocations.Count.ShouldBe(1);
        repositoryMock.SaveChangesInvocations[0].Aggregate.ShouldBeSameAs(aggregate);
    }


    [Fact]
    public async Task GetLatest_ShouldReturnAggregateDataStore() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestAggregate { AggregateId = aggregateId };
        var repositoryMock = new TestAggregateRepository();

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        repositoryMock.GetLatestVersionHandler = (_, _) => Task.FromResult(Result.Success(aggregate));

        // Act
        var result = await _aggregateService.GetLatest<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(aggregate);
    }

    [Fact]
    public async Task GetLatest_GetFailed_ReturnsFailedResult() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var repositoryMock = new TestAggregateRepository();

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        repositoryMock.GetLatestVersionHandler = (_, _) => Task.FromResult<Result<TestAggregate>>(Result.Failure());

        // Act
        var result = await _aggregateService.GetLatest<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task GetLatest_GetLatestThrowsException_ReturnsFailedResult() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var repositoryMock = new TestAggregateRepository();

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        repositoryMock.GetLatestVersionHandler = (_, _) => Task.FromException<Result<TestAggregate>>(new Exception());
        // Act
        var result = await _aggregateService.GetLatest<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task GetLatestVersionFromLastEvent_ShouldReturnAggregateDataStore() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var aggregate = new TestAggregate { AggregateId = aggregateId };
        var repositoryMock = new TestAggregateRepository();

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        repositoryMock.GetLatestVersionFromLastEventHandler = (_, _) => Task.FromResult(Result.Success(aggregate));

        // Act
        var result = await _aggregateService.GetLatestFromLastEvent<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(aggregate);
    }

    [Fact]
    public async Task GetLatestVersionFromLastEvent_GetFailed_ReturnsFailedResult() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var repositoryMock = new TestAggregateRepository();

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        repositoryMock.GetLatestVersionFromLastEventHandler = (_, _) => Task.FromResult<Result<TestAggregate>>(Result.Failure());

        // Act
        var result = await _aggregateService.GetLatestFromLastEvent<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task GetLatestVersionFromLastEvent_GetLatestThrowsException_ReturnsFailedResult() {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var repositoryMock = new TestAggregateRepository();

        _repositoryResolverMock.Resolve<TestAggregate, DomainEvent>().Returns(repositoryMock);

        repositoryMock.GetLatestVersionFromLastEventHandler = (_, _) => Task.FromException<Result<TestAggregate>>(new Exception());
        // Act
        var result = await _aggregateService.GetLatestFromLastEvent<TestAggregate>(aggregateId, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public void AddCachedAggregate_OverrideCacheOptions() {
        this._aggregateService.AddCachedAggregate(typeof(TestAggregate), new MemoryCacheEntryOptions());
    }
}
