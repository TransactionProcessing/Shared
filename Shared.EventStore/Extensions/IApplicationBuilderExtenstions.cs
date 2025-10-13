namespace Shared.EventStore.Extensions;

using EventHandling;
using global::EventStore.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Shared.EventStore.EventStore;
using Shared.TraceHandler;
using SubscriptionWorker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class IApplicationBuilderExtensions
{
    #region Methods

    [ExcludeFromCodeCoverage(Justification = "Cant test with coverage as has thread inside")]
    public static async Task ConfigureSubscriptionService(this IApplicationBuilder applicationBuilder,
                                                          SubscriptionWorkersRoot workerConfig,
                                                          String eventStoreConnectionString,
                                                          Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                          Action<TraceEventType, String,String> traceHandler,
                                                          Func<String, Int32, ISubscriptionRepository> subscriptionRepositoryResolver) {

        using (CancellationTokenSource cts = new())
        {
            if (workerConfig == null)
                throw new ArgumentNullException("No Worker configuration supplied");
            if (subscriptionRepositoryResolver == null)
                throw new ArgumentNullException("No Subscription Repository Resolver supplied");
            if (!workerConfig.InternalSubscriptionService)
                return;
            if (workerConfig.SubscriptionWorkers == null || !workerConfig.SubscriptionWorkers.Any())
                throw new ArgumentNullException("No SubscriptionWorkers supplied");

            ISubscriptionRepository subscriptionRepository = subscriptionRepositoryResolver(eventStoreConnectionString,
                workerConfig.InternalSubscriptionServiceCacheDuration);
            
            // init our SubscriptionRepository
            await subscriptionRepository.PreWarm(cts.Token);

            List<SubscriptionWorker> workers =
                IApplicationBuilderExtensions.ConfigureSubscriptions(subscriptionRepository, workerConfig,
                    eventStoreConnectionString, eventHandlerResolvers, traceHandler);
            foreach (SubscriptionWorker subscriptionWorker in workers)
            {
                await subscriptionWorker.StartAsync(cts.Token);
            }
        }
    }

    internal static List<SubscriptionWorker> GetDomainWorker(Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                             ISubscriptionRepository subscriptionRepository,
                                                             SubscriptionWorkersRoot configuration,
                                                             String eventStoreConnectionString,
                                                             SubscriptionWorkerConfig configurationSubscriptionWorker,
                                                             Action<TraceEventType, String, String> traceHandler) {
        KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Domain");
        List<SubscriptionWorker> workers = new();
        if (ehr.Value == null) 
            return workers;
        for (Int32 i = 0; i < configurationSubscriptionWorker.InstanceCount; i++) {
            SubscriptionWorker worker = ConfigureSubscriptionWorker(subscriptionRepository, configuration, eventStoreConnectionString, traceHandler, ehr, configurationSubscriptionWorker, "DOMAIN");

            workers.Add(worker);
        }

        return workers;
    }

    internal static List<SubscriptionWorker> GetOrderedWorkers(Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                               ISubscriptionRepository subscriptionRepository,
                                                               SubscriptionWorkersRoot configuration,
                                                               String eventStoreConnectionString,
                                                               SubscriptionWorkerConfig configurationSubscriptionWorker,
                                                               Action<TraceEventType, String, String> traceHandler) {
        KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Ordered");
        List<SubscriptionWorker> workers = new();
        if (ehr.Value == null) 
            return workers;
        SubscriptionWorker worker = ConfigureSubscriptionWorker(subscriptionRepository, configuration,
            eventStoreConnectionString, traceHandler, ehr, configurationSubscriptionWorker, "ORDERED");
        workers.Add(worker);
        return workers;
    }

    internal static List<SubscriptionWorker> GetMainWorkers(Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                            ISubscriptionRepository subscriptionRepository,
                                                            SubscriptionWorkersRoot configuration,
                                                            String eventStoreConnectionString,
                                                            SubscriptionWorkerConfig configurationSubscriptionWorker,
                                                            Action<TraceEventType, String, String> traceHandler) {
        List<SubscriptionWorker> workers = new();
        KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Main");
        if (ehr.Value == null) 
            return workers;
        for (Int32 i = 0; i < configurationSubscriptionWorker.InstanceCount; i++)
        {
            SubscriptionWorker worker = ConfigureSubscriptionWorker(subscriptionRepository, configuration,
                eventStoreConnectionString, traceHandler, ehr, configurationSubscriptionWorker, "MAIN");

            workers.Add(worker);
        }

        return workers;
    }

    internal static List<SubscriptionWorker> ConfigureSubscriptions(ISubscriptionRepository subscriptionRepository,
                                                                    SubscriptionWorkersRoot configuration,
                                                                    String eventStoreConnectionString,
                                                                    Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                                    Action<TraceEventType, String, String> traceHandler) {
        List<SubscriptionWorker> workers = new();

        foreach (SubscriptionWorkerConfig configurationSubscriptionWorker in configuration.SubscriptionWorkers) {
            if (!configurationSubscriptionWorker.Enabled)
                continue;

            List<SubscriptionWorker> workersList = configurationSubscriptionWorker switch {
                _ when configurationSubscriptionWorker.IsDomainOnly => GetDomainWorker(eventHandlerResolvers, subscriptionRepository, configuration, eventStoreConnectionString, configurationSubscriptionWorker, traceHandler),
                _ when configurationSubscriptionWorker.IsOrdered => GetOrderedWorkers(eventHandlerResolvers, subscriptionRepository, configuration, eventStoreConnectionString, configurationSubscriptionWorker, traceHandler),
                _ => GetMainWorkers(eventHandlerResolvers, subscriptionRepository, configuration, eventStoreConnectionString, configurationSubscriptionWorker, traceHandler),
            };

            workers.AddRange(workersList);
        }

        return workers;
    }

    private static SubscriptionWorker ConfigureSubscriptionWorker(ISubscriptionRepository subscriptionRepository,
                                                                  SubscriptionWorkersRoot configuration,
                                                                  String eventStoreConnectionString,
                                                                  Action<TraceEventType, String, String> traceHandler,
                                                                  KeyValuePair<String, IDomainEventHandlerResolver> ehr,
                                                                  SubscriptionWorkerConfig configurationSubscriptionWorker,
                                                                  String type) {

        SubscriptionWorker worker = type switch {
            "ORDERED" => SubscriptionWorker.CreateOrderedSubscriptionWorker(eventStoreConnectionString, ehr.Value, subscriptionRepository, configuration.PersistentSubscriptionPollingInSeconds),
            _ => SubscriptionWorker.CreateSubscriptionWorker(eventStoreConnectionString, ehr.Value, subscriptionRepository, configurationSubscriptionWorker.InflightMessages, configuration.PersistentSubscriptionPollingInSeconds),
        };

         worker.ConfigureTracing(type, traceHandler);

        worker.SetIgnoreGroups(configurationSubscriptionWorker.IgnoreGroups);
        worker.SetIgnoreStreams(configurationSubscriptionWorker.IgnoreStreams);
        worker.SetIncludeGroups(configurationSubscriptionWorker.IncludeGroups);
        worker.SetIncludeStreams(configurationSubscriptionWorker.IncludeStreams);
        return worker;
    }

    

    #endregion

}