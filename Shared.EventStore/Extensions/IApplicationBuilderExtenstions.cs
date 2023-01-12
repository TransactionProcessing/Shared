namespace Shared.EventStore.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventHandling;
using global::EventStore.Client;
using Microsoft.AspNetCore.Builder;
using SubscriptionWorker;

public static class IApplicationBuilderExtenstions
{
    #region Methods

    public static async Task ConfigureSubscriptionService(this IApplicationBuilder applicationBuilder,
                                                          SubscriptionWorkersRoot workerConfig,
                                                          String eventStoreConnectionString,
                                                          EventStoreClientSettings clientSettings,
                                                          Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                          Action<TraceEventType, String,String> traceHandler,
                                                          CancellationToken cancellationToken) {
        if (workerConfig == null)
            throw new Exception("No Worker configuration supplied");
        if (workerConfig.InternalSubscriptionService == false)
            return;

        ISubscriptionRepository subscriptionRepository = SubscriptionRepository.Create(eventStoreConnectionString, workerConfig.InternalSubscriptionServiceCacheDuration);
        // TODO: Some logging....
        ((SubscriptionRepository)subscriptionRepository).Trace += (sender,
                                                                   s) => traceHandler(TraceEventType.Information, "REPOSITORY", s);

        // init our SubscriptionRepository
        await subscriptionRepository.PreWarm(cancellationToken);

        List<SubscriptionWorker> workers =
            IApplicationBuilderExtenstions.ConfigureSubscriptions(subscriptionRepository, workerConfig, eventHandlerResolvers, clientSettings,traceHandler);
        foreach (SubscriptionWorker subscriptionWorker in workers) {
            await subscriptionWorker.StartAsync(cancellationToken);
        }
    }

    private static List<SubscriptionWorker> ConfigureSubscriptions(ISubscriptionRepository subscriptionRepository,
                                                                   SubscriptionWorkersRoot configuration,
                                                                   Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                                   EventStoreClientSettings clientSettings,
                                                                   Action<TraceEventType, String, String> traceHandler) {
        List<SubscriptionWorker> workers = new();

        foreach (SubscriptionWorkerConfig configurationSubscriptionWorker in configuration.SubscriptionWorkers) {
            if (configurationSubscriptionWorker.Enabled == false)
                continue;

            if (configurationSubscriptionWorker.IsOrdered) {
                KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Ordered");

                if (ehr.Value != null) {
                    SubscriptionWorker worker = SubscriptionWorker.CreateOrderedSubscriptionWorker(clientSettings,
                                                                                                   ehr.Value,
                                                                                                   subscriptionRepository,
                                                                                                   configuration.PersistentSubscriptionPollingInSeconds);
                    worker.Trace += (_,
                                     args) => traceHandler(TraceEventType.Information, "ORDERED", args.Message);
                    worker.Warning += (_,
                                       args) => traceHandler(TraceEventType.Warning, "ORDERED", args.Message);
                    worker.Error += (_,
                                     args) => traceHandler(TraceEventType.Error, "ORDERED", args.Message);
                    worker.SetIgnoreGroups(configurationSubscriptionWorker.IgnoreGroups);
                    worker.SetIgnoreStreams(configurationSubscriptionWorker.IgnoreStreams);
                    worker.SetIncludeGroups(configurationSubscriptionWorker.IncludeGroups);
                    worker.SetIncludeStreams(configurationSubscriptionWorker.IncludeStreams);
                    workers.Add(worker);
                }
            }
            else {
                KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Main");
                if (ehr.Value != null) {
                    for (Int32 i = 0; i < configurationSubscriptionWorker.InstanceCount; i++) {
                        SubscriptionWorker worker = SubscriptionWorker.CreateSubscriptionWorker(clientSettings,
                                                                                                ehr.Value,
                                                                                                subscriptionRepository,
                                                                                                configurationSubscriptionWorker.InflightMessages,
                                                                                                configuration.PersistentSubscriptionPollingInSeconds);

                        worker.Trace += (_,
                                         args) => traceHandler(TraceEventType.Information, "MAIN", args.Message);
                        worker.Warning += (_,
                                           args) => traceHandler(TraceEventType.Warning, "MAIN", args.Message);
                        worker.Error += (_,
                                         args) => traceHandler(TraceEventType.Error, "MAIN", args.Message);
                        worker.SetIgnoreGroups(configurationSubscriptionWorker.IgnoreGroups);
                        worker.SetIgnoreStreams(configurationSubscriptionWorker.IgnoreStreams);
                        worker.SetIncludeGroups(configurationSubscriptionWorker.IncludeGroups);
                        worker.SetIncludeStreams(configurationSubscriptionWorker.IncludeStreams);

                        workers.Add(worker);
                    }
                }
            }
        }

        return workers;
    }
    #endregion
}