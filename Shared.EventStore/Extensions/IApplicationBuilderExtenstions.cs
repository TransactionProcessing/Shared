namespace Shared.EventStore.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventHandling;
using global::EventStore.Client;
using Microsoft.AspNetCore.Builder;
using SubscriptionWorker;

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

    internal static List<SubscriptionWorker> ConfigureSubscriptions(ISubscriptionRepository subscriptionRepository,
                                                                   SubscriptionWorkersRoot configuration,
                                                                   String eventStoreConnectionString,
                                                                   Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                                   Action<TraceEventType, String, String> traceHandler) {
        List<SubscriptionWorker> workers = new();

        foreach (SubscriptionWorkerConfig configurationSubscriptionWorker in configuration.SubscriptionWorkers) {
            if (!configurationSubscriptionWorker.Enabled)
                continue;

            if (configurationSubscriptionWorker.IsOrdered) {
                KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Ordered");

                if (ehr.Value != null) {
                    SubscriptionWorker worker = ConfigureSubscriptionWorker(subscriptionRepository, configuration,
                        eventStoreConnectionString, traceHandler, ehr, configurationSubscriptionWorker, "ORDERED");
                    workers.Add(worker);
                }
            }
            else if (configurationSubscriptionWorker.IsDomainOnly) {
                KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Domain");

                if (ehr.Value != null)
                {
                    for (Int32 i = 0; i < configurationSubscriptionWorker.InstanceCount; i++) {
                        SubscriptionWorker worker = ConfigureSubscriptionWorker(subscriptionRepository, configuration, 
                            eventStoreConnectionString, traceHandler, ehr, configurationSubscriptionWorker, "DOMAIN");

                        workers.Add(worker);
                    }
                }
            }
            else {
                KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Main");
                if (ehr.Value != null)
                {
                    for (Int32 i = 0; i < configurationSubscriptionWorker.InstanceCount; i++)
                    {
                        SubscriptionWorker worker = ConfigureSubscriptionWorker(subscriptionRepository, configuration,
                            eventStoreConnectionString, traceHandler, ehr, configurationSubscriptionWorker, "MAIN");

                        workers.Add(worker);
                    }
                }
            }
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
        SubscriptionWorker worker = SubscriptionWorker.CreateSubscriptionWorker(eventStoreConnectionString, ehr.Value, subscriptionRepository, configurationSubscriptionWorker.InflightMessages, configuration.PersistentSubscriptionPollingInSeconds);

        worker.Trace += (_,
                         args) => traceHandler(TraceEventType.Information, type, args.Message);
        worker.Warning += (_,
                           args) => traceHandler(TraceEventType.Warning, type, args.Message);
        worker.Error += (_,
                         args) => traceHandler(TraceEventType.Error, type, args.Message);
        worker.SetIgnoreGroups(configurationSubscriptionWorker.IgnoreGroups);
        worker.SetIgnoreStreams(configurationSubscriptionWorker.IgnoreStreams);
        worker.SetIncludeGroups(configurationSubscriptionWorker.IncludeGroups);
        worker.SetIncludeStreams(configurationSubscriptionWorker.IncludeStreams);
        return worker;
    }

    #endregion
}