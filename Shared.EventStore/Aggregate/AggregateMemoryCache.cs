using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using Prometheus;
using Shared.EventStore.Aggregate;

namespace Shared.EventStore.Aggregate;

[ExcludeFromCodeCoverage]
public class AggregateMemoryCache {
    private readonly IMemoryCache MemoryCache;

    private readonly ConcurrentDictionary<String, Aggregate> KeyTracker;

    public AggregateMemoryCache(IMemoryCache memoryCache) {
        this.MemoryCache = memoryCache;
        this.KeyTracker = new ConcurrentDictionary<String, Aggregate>();
    }

    private static readonly Dictionary<Type, Boolean> CallbackRegistered = new();

    private static readonly Object CallbackLock = new();

    public void Set<TAggregate>(String key,
                                Aggregate aggregate,
                                MemoryCacheEntryOptions memoryCacheEntryOptions) where TAggregate : Aggregate, new() {
        Type aggregateType = typeof(TAggregate);

        // Ensure the eviction callback is registered only once per TAggregate type
        if (!AggregateMemoryCache.CallbackRegistered.TryGetValue(aggregateType, out Boolean isRegistered) || !isRegistered) {
            Monitor.Enter(AggregateMemoryCache.CallbackLock);

            if (!AggregateMemoryCache.CallbackRegistered.TryGetValue(aggregateType, out isRegistered) || !isRegistered) // Double-check locking
            {
                // Register a callback to remove the item from our internal tracking
                memoryCacheEntryOptions.RegisterPostEvictionCallback((evictedKey, _, _, _) => this.KeyTracker.TryRemove(evictedKey.ToString(), out _));

                AggregateMemoryCache.CallbackRegistered[aggregateType] = true;
            }

            Monitor.Exit(AggregateMemoryCache.CallbackLock);
        }

        // Set the cache entry
        this.MemoryCache.Set(key, aggregate, memoryCacheEntryOptions);
        this.KeyTracker.TryAdd(key, aggregate);

        Counter counterCalls = AggregateService.GetCounterMetric($"AggregateService_{aggregateType.Name}_times_cache_saved");
        counterCalls.Inc();

        Counter counterItems = AggregateService.GetCounterMetric($"AggregateService_{aggregateType.Name}_total_cached_items");
        counterItems.IncTo(this.KeyTracker.Count);
    }

    public Boolean TryGetValueWithMetrics<TAggregate>(String key,
                                                      out TAggregate aggregate) where TAggregate : Aggregate, new() {
        String g = typeof(TAggregate).Name;

        var found = this.MemoryCache.TryGetValue(key, out aggregate);

        if (!found) {
            //TODO: Failed cache hit?
            Counter counterCalls = AggregateService.GetCounterMetric($"AggregateService_{g}_failed_cache_hit");
            counterCalls.Inc();
        }

        return found;
    }

    public Boolean TryGetValue<TAggregate>(String key,
                                           out TAggregate aggregate) where TAggregate : Aggregate, new() {
        return this.MemoryCache.TryGetValue(key, out aggregate);
    }
}