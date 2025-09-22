using Microsoft.Extensions.Caching.Memory;
using Shared.DomainDrivenDesign.EventSourcing;
using SimpleResults;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;

namespace Shared.EventStore.Aggregate;

public interface IAggregateService
{
    void AddCachedAggregate(Type aggregateType, MemoryCacheEntryOptions memoryCacheEntryOptions);

    Task<Result<TAggregate>> Get<TAggregate>(Guid aggregateId,
                                             CancellationToken cancellationToken) where TAggregate : Aggregate, new();

    Task<SimpleResults.Result<TAggregate>> GetLatest<TAggregate>(Guid aggregateId,
                                                                 CancellationToken cancellationToken) where TAggregate : Aggregate, new();

    Task<SimpleResults.Result<TAggregate>> GetLatestFromLastEvent<TAggregate>(Guid aggregateId,
                                                                              CancellationToken cancellationToken) where TAggregate : Aggregate, new();

    Task<SimpleResults.Result<TAggregate>> GetLatestAggregateAsync<TAggregate>(Guid aggregateId,
                                                                               Func<IAggregateRepository<TAggregate, DomainEvent>, Guid, CancellationToken, Task<SimpleResults.Result<TAggregate>>> getLatestVersionFunc,
                                                                               CancellationToken cancellationToken) where TAggregate : Aggregate, new();

    Task<Result> Save<TAggregate>(TAggregate aggregate,
                                  CancellationToken cancellationToken) where TAggregate : Aggregate, new();
}

public class AggregateService : IAggregateService
{
    private readonly IAggregateRepositoryResolver AggregateRepositoryResolver;
    private readonly AggregateMemoryCache Cache;
    private readonly List<(Type, MemoryCacheEntryOptions, Object)> AggregateTypes;

    private static readonly MemoryCacheEntryOptions DefaultMemoryCacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30))
        .RegisterPostEvictionCallback(AggregateService.EvictionCallback);

    public AggregateService(IAggregateRepositoryResolver aggregateRepositoryResolver,
                            IMemoryCache cache)
    {
        this.AggregateRepositoryResolver = aggregateRepositoryResolver;
        this.Cache = new AggregateMemoryCache(cache);

        //We update this list to contain MemoryCacheEntryOptions
        // TODO: We might make this configurable in the future
        this.AggregateTypes = new();
    }

    internal static void EvictionCallback(Object key,
                                          Object value,
                                          EvictionReason reason,
                                          Object state)
    {
        Logger.Logger.LogWarning($"Key [{key}] of type [{value.GetType()}] removed from the cache {reason.ToString()}");
    }

    internal (Type, MemoryCacheEntryOptions, Object) GetAggregateType<TAggregate>() where TAggregate : Aggregate, new()
    {
        return this.AggregateTypes.SingleOrDefault(a => a.Item1 == typeof(TAggregate));
    }

    internal void SetCache<TAggregate>((Type, MemoryCacheEntryOptions, Object) aggregateType,
                                       Aggregate aggregate) where TAggregate : Aggregate, new()
    {
        //Changed the trace here. 
        //We have at least one scenario where something in aggregateType is null, and stopped us actually setting the cache!
        //This approach should be safer.
        if (aggregate == null)
        {
            Logger.Logger.LogWarning("aggregate is null");
        }

        Logger.Logger.LogWarning("About to save to cache.");

        String g = typeof(TAggregate).Name;
        String key = $"{g}-{aggregate.AggregateId}";

        this.Cache.Set<TAggregate>(key, aggregate, aggregateType.Item2);
    }

    public void AddCachedAggregate(Type aggregateType, MemoryCacheEntryOptions memoryCacheEntryOptions = null)
    {

        MemoryCacheEntryOptions localMemoryCacheEntryOptions = DefaultMemoryCacheEntryOptions;
        if (memoryCacheEntryOptions != null)
        {
            localMemoryCacheEntryOptions = memoryCacheEntryOptions;
        }

        this.AggregateTypes.Add((aggregateType, localMemoryCacheEntryOptions, new Object()));
    }

    public async Task<Result<TAggregate>> Get<TAggregate>(Guid aggregateId,
                                                          CancellationToken cancellationToken) where TAggregate : Aggregate, new()
    {
        Debug.WriteLine("In Get");
        (Type, MemoryCacheEntryOptions, Object) at = GetAggregateType<TAggregate>();
        TAggregate aggregate = default;
        String g = typeof(TAggregate).Name;
        String key = $"{g}-{aggregateId}";

        // Check the cache
        if (at != default && this.Cache.TryGetValue(key, out aggregate))
        {
            return Result.Success(aggregate);
        }

        if (at == default)
        {
            // We don't use caching for this aggregate so just hit GetLatest
            Result<TAggregate> getResult = await this.GetLatest<TAggregate>(aggregateId, cancellationToken);

            if (getResult.IsFailed)
            {
                return getResult;
            }

            return Result.Success(getResult.Data);
        }

        try
        {
            // Lock
            Monitor.Enter(at.Item3);

            if (this.Cache.TryGetValueWithMetrics<TAggregate>(key, out TAggregate cachedAggregate))
            {
                return Result.Success(cachedAggregate);
            }

            // Not found in cache so call GetLatest
            SimpleResults.Result<TAggregate> aggregateResult = this.GetLatest<TAggregate>(aggregateId, cancellationToken).Result;

            if (aggregateResult.IsSuccess)
            {
                aggregate = aggregateResult.Data;
                this.SetCache<TAggregate>(at, aggregateResult.Data);
                return Result.Success(aggregate);
            }

            Logger.Logger.LogWarning($"aggregateResult failed {aggregateResult.Message}");
            return aggregateResult;
        }
        finally
        {
            // Release
            Monitor.Exit(at.Item3);
        }
    }

    public async Task<SimpleResults.Result<TAggregate>> GetLatest<TAggregate>(Guid aggregateId,
                                                                              CancellationToken cancellationToken) where TAggregate : Aggregate, new()
    {
        return await this.GetLatestAggregateAsync<TAggregate>(aggregateId, (repo,
                                                                            id,
                                                                            cancellation) => repo.GetLatestVersion(id, cancellation), cancellationToken);
    }

    public async Task<SimpleResults.Result<TAggregate>> GetLatestFromLastEvent<TAggregate>(Guid aggregateId,
                                                                                           CancellationToken cancellationToken) where TAggregate : Aggregate, new()
    {
        return await this.GetLatestAggregateAsync<TAggregate>(aggregateId, (repo,
                                                                            id,
                                                                            cancellation) => repo.GetLatestVersionFromLastEvent(id, cancellation), cancellationToken);
    }

    public async Task<SimpleResults.Result<TAggregate>> GetLatestAggregateAsync<TAggregate>(Guid aggregateId,
                                                                                            Func<IAggregateRepository<TAggregate, DomainEvent>, Guid, CancellationToken, Task<SimpleResults.Result<TAggregate>>> getLatestVersionFunc,
                                                                                            CancellationToken cancellationToken) where TAggregate : Aggregate, new()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        IAggregateRepository<TAggregate, DomainEvent> repository = this.AggregateRepositoryResolver.Resolve<TAggregate, DomainEvent>();

        String g = typeof(TAggregate).Name;
        String m = "AggregateService";
        Counter counterCalls = AggregateService.GetCounterMetric($"{m}_{g}_times_rehydrated");
        Histogram histogramMetric = AggregateService.GetHistogramMetric($"{m}_{g}_rehydrated");

        counterCalls.Inc();
        TAggregate aggregate = null;
        try
        {
            var aggregateResult = await getLatestVersionFunc(repository, aggregateId, cancellationToken);
            if (aggregateResult.IsFailed)
                return aggregateResult;
            aggregate = aggregateResult.Data;
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }

        stopwatch.Stop();
        histogramMetric.Observe(stopwatch.Elapsed.TotalSeconds);

        return Result.Success(aggregate);
    }


    public async Task<Result> Save<TAggregate>(TAggregate aggregate,
                                               CancellationToken cancellationToken) where TAggregate : Aggregate, new()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        IAggregateRepository<TAggregate, DomainEvent> repository = this.AggregateRepositoryResolver.Resolve<TAggregate, DomainEvent>();

        String g = typeof(TAggregate).Name;
        String m = $"AggregateService";
        Counter counterCalls = AggregateService.GetCounterMetric($"{m}_{g}_times_saved");
        Histogram histogramMetric = AggregateService.GetHistogramMetric($"{m}_{g}_saved");

        counterCalls.Inc();

        // TODO: Check the pending events so dont save blindly, this would need a change to the base aggregate ?
        Result result = await repository.SaveChanges(aggregate, cancellationToken);

        stopwatch.Stop();

        histogramMetric.Observe(stopwatch.Elapsed.TotalSeconds);

        if (result.IsFailed)
        {
            // Get out before any caching
            return result;
        }

        (Type, MemoryCacheEntryOptions, Object) at = GetAggregateType<TAggregate>();

        if (at != default)
        {
            this.SetCache<TAggregate>(at, aggregate);
        }

        return result;
    }

    public static readonly ConcurrentDictionary<String, Counter> DynamicCounter = new();

    public static readonly ConcurrentDictionary<String, Histogram> DynamicHistogram = new();

    private static readonly Func<String, String, String> FormatMetricName = (methodName,
                                                                             metricType) => $"{methodName}_{metricType}";

    public static Histogram GetHistogramMetric(String methodName)
    {
        String n = AggregateService.FormatMetricName(methodName, nameof(Histogram).ToLower());

        HistogramConfiguration histogramConfiguration = new()
        {
            Buckets = new[] { 1.0, 2.0, 5.0, 10.0, Double.PositiveInfinity }
        };

        var histogram = AggregateService.DynamicHistogram.GetOrAdd(methodName,
            name => Metrics.CreateHistogram(name: n,
                help: $"Histogram of the execution time for {n}",
                histogramConfiguration));

        return histogram;
    }

    public static Counter GetCounterMetric(String methodName)
    {
        String n = AggregateService.FormatMetricName(methodName, nameof(Counter).ToLower());

        var counter = AggregateService.DynamicCounter.GetOrAdd(methodName, name => Metrics.CreateCounter(name: n, help: $"Total number times executed {n}"));

        return counter;
    }
}