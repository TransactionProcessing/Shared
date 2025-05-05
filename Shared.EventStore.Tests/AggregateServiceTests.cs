using System;
using System.Threading.Tasks;
using Shared.DomainDrivenDesign.EventSourcing;
using Shared.EventStore.Aggregate;
using Shared.EventStore.Tests.TestObjects;
using Shared.Logger;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Shouldly;
using SimpleResults;
using System.Threading;
using Xunit;

namespace Shared.EventStore.Tests {
    public class AggregateServiceTests {
        private readonly Mock<IAggregateRepositoryResolver> _repositoryResolverMock;
        private readonly IMemoryCache _memoryCache;
        private readonly AggregateService _aggregateService;

        public AggregateServiceTests() {
            _repositoryResolverMock = new Mock<IAggregateRepositoryResolver>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _aggregateService = new AggregateService(_repositoryResolverMock.Object, _memoryCache);
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
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            repositoryMock.Setup(repo => repo.GetLatestVersion(aggregateId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(aggregate));

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

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
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();
            var cacheKey = $"TestAggregate-{aggregateId}";
            this._aggregateService.AddCachedAggregate(typeof(TestAggregate));

            repositoryMock.Setup(repo => repo.GetLatestVersion(aggregateId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(aggregate));

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

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
            var aggregate = new TestAggregate { AggregateId = aggregateId };
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            repositoryMock.Setup(repo => repo.GetLatestVersion(aggregateId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure("Error getting latest"));

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            // Act
            var result = await _aggregateService.Get<TestAggregate>(aggregateId, CancellationToken.None);

            // Assert
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task Get_ShouldReturnAggregateFromRepository_GetLatestThrowsException_FailedResultReturned() {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var aggregate = new TestAggregate { AggregateId = aggregateId };
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            repositoryMock.Setup(repo => repo.GetLatestVersion(aggregateId, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Exception Message"));

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

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
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            repositoryMock.Setup(repo => repo.SaveChanges(aggregate, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            // Act
            var result = await _aggregateService.Save(aggregate, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            repositoryMock.Verify(repo => repo.SaveChanges(aggregate, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Save_ShouldSaveAggregateToRepository_AndCacheIt() {
            // Arrange
            var aggregate = new TestAggregate { AggregateId = Guid.NewGuid() };
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();
            this._aggregateService.AddCachedAggregate(typeof(TestAggregate));

            repositoryMock.Setup(repo => repo.SaveChanges(aggregate, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            // Act
            var result = await _aggregateService.Save(aggregate, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            repositoryMock.Verify(repo => repo.SaveChanges(aggregate, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Save_ShouldSaveAggregateToRepository_AndCacheIt_SecondSave_UpdatesCache() {
            // Arrange
            var aggregate = new TestAggregate { AggregateId = Guid.NewGuid() };
            aggregate.SetAggregateName("1", Guid.NewGuid());
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();
            this._aggregateService.AddCachedAggregate(typeof(TestAggregate));

            repositoryMock.Setup(repo => repo.SaveChanges(aggregate, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            // Act
            var result = await _aggregateService.Save(aggregate, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            repositoryMock.Verify(repo => repo.SaveChanges(aggregate, It.IsAny<CancellationToken>()), Times.Once);

            _memoryCache.TryGetValue($"TestAggregate-{aggregate.AggregateId}", out TestAggregate cachedAggregate).ShouldBeTrue();
            cachedAggregate.AggregateName.ShouldBe("1");

            aggregate.SetAggregateName("2", Guid.NewGuid());
            result = await _aggregateService.Save(aggregate, CancellationToken.None);

            _memoryCache.TryGetValue($"TestAggregate-{aggregate.AggregateId}", out cachedAggregate).ShouldBeTrue();
            cachedAggregate.AggregateName.ShouldBe("2");
        }

        [Fact]
        public async Task Save_ShouldSaveAggregateToRepository_SaveFails() {
            // Arrange
            var aggregate = new TestAggregate { AggregateId = Guid.NewGuid() };
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            repositoryMock.Setup(repo => repo.SaveChanges(aggregate, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure);

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            // Act
            var result = await _aggregateService.Save(aggregate, CancellationToken.None);

            // Assert
            result.IsFailed.ShouldBeTrue();
        }


        [Fact]
        public async Task GetLatest_ShouldReturnAggregateDataStore() {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var aggregate = new TestAggregate { AggregateId = aggregateId };
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            repositoryMock.Setup(repo => repo.GetLatestVersion(aggregateId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(aggregate));

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
            var aggregate = new TestAggregate { AggregateId = aggregateId };
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            repositoryMock.Setup(repo => repo.GetLatestVersion(aggregateId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure());

            // Act
            var result = await _aggregateService.GetLatest<TestAggregate>(aggregateId, CancellationToken.None);

            // Assert
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task GetLatest_GetLatestThrowsException_ReturnsFailedResult() {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var aggregate = new TestAggregate { AggregateId = aggregateId };
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            repositoryMock.Setup(repo => repo.GetLatestVersion(aggregateId, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());
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
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            repositoryMock.Setup(repo => repo.GetLatestVersionFromLastEvent(aggregateId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(aggregate));

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
            var aggregate = new TestAggregate { AggregateId = aggregateId };
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            repositoryMock.Setup(repo => repo.GetLatestVersionFromLastEvent(aggregateId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure());

            // Act
            var result = await _aggregateService.GetLatestFromLastEvent<TestAggregate>(aggregateId, CancellationToken.None);

            // Assert
            result.IsFailed.ShouldBeTrue();
        }

        [Fact]
        public async Task GetLatestVersionFromLastEvent_GetLatestThrowsException_ReturnsFailedResult() {
            // Arrange
            var aggregateId = Guid.NewGuid();
            var aggregate = new TestAggregate { AggregateId = aggregateId };
            var repositoryMock = new Mock<IAggregateRepository<TestAggregate, DomainEvent>>();

            _repositoryResolverMock.Setup(resolver => resolver.Resolve<TestAggregate, DomainEvent>()).Returns(repositoryMock.Object);

            repositoryMock.Setup(repo => repo.GetLatestVersionFromLastEvent(aggregateId, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());
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
}