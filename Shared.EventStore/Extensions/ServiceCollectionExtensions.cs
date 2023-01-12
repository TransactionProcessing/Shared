namespace Shared.EventStore.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::EventStore.Client;
    using Grpc.Core.Interceptors;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Shared.EventStore.EventHandling;
    using Shared.EventStore.SubscriptionWorker;
    using Shared.Extensions;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEventStoreProjectionManagerClient(this IServiceCollection services, Action<EventStoreClientSettings>? configureSettings = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            EventStoreClientSettings settings = new EventStoreClientSettings();
            configureSettings?.Invoke(settings);

            services.TryAddSingleton(provider => {
                                         settings.LoggerFactory ??= provider.GetService<ILoggerFactory>();
                                         settings.Interceptors ??= provider.GetServices<Interceptor>();

                                         return new EventStoreProjectionManagementClient(settings);
                                     });

            return services;
        }
    }

    public static class IApplicationBuilderExtenstions
    {
        public static async Task ConfigureSubscriptionService(this IApplicationBuilder applicationBuilder,
                                                        SubscriptionWorkersRoot workerConfig,
                                                        String eventStoreConnectionString,
                                                        EventStoreClientSettings clientSettings,
                                                        Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                        CancellationToken cancellationToken)
        {
            if (workerConfig == null)
                throw new Exception("No Worker configuration supplied");
            if (workerConfig.InternalSubscriptionService == false)
                return;

            ISubscriptionRepository subscriptionRepository = SubscriptionRepository.Create(eventStoreConnectionString, workerConfig.InternalSubscriptionServiceCacheDuration);
            // TODO: Some logging....
            //((SubscriptionRepository)subscriptionRepository).Trace += (sender,
            //                                                           s) => Extensions.log(TraceEventType.Information, "REPOSITORY", s);

            // init our SubscriptionRepository
            await subscriptionRepository.PreWarm(cancellationToken);

            //List<SubscriptionWorker> workers = ConfigureSubscriptions(subscriptionRepository, subscriptionWorkersRoot);
            //foreach (SubscriptionWorker subscriptionWorker in workers)
            //{
                //subscriptionWorker.StartAsync(CancellationToken.None).Wait();
            //}
        }

        private static List<SubscriptionWorker> ConfigureSubscriptions(ISubscriptionRepository subscriptionRepository, 
                                                                       SubscriptionWorkersRoot configuration, 
                                                                       Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers,
                                                                       EventStoreClientSettings clientSettings)
        {
            List<SubscriptionWorker> workers = new List<SubscriptionWorker>();

            foreach (SubscriptionWorkerConfig configurationSubscriptionWorker in configuration.SubscriptionWorkers)
            {
                if (configurationSubscriptionWorker.Enabled == false)
                    continue;

                if (configurationSubscriptionWorker.IsOrdered) {
                    KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Ordered");

                    if (ehr.Value != null) {
                        SubscriptionWorker worker = SubscriptionWorker.CreateOrderedSubscriptionWorker(clientSettings,
                                                                                                       ehr.Value,
                                                                                                       subscriptionRepository,
                                                                                                       configuration.PersistentSubscriptionPollingInSeconds);
                        // TODO: Logging
                        //worker.Trace += (_,
                        //                 args) => Extensions.orderedLog(TraceEventType.Information, args.Message);
                        //worker.Warning += (_,
                        //                   args) => Extensions.orderedLog(TraceEventType.Warning, args.Message);
                        //worker.Error += (_,
                        //                 args) => Extensions.orderedLog(TraceEventType.Error, args.Message);
                        worker.SetIgnoreGroups(configurationSubscriptionWorker.IgnoreGroups);
                        worker.SetIgnoreStreams(configurationSubscriptionWorker.IgnoreStreams);
                        worker.SetIncludeGroups(configurationSubscriptionWorker.IncludeGroups);
                        worker.SetIncludeStreams(configurationSubscriptionWorker.IncludeStreams);
                        workers.Add(worker);
                    }

                }
                else
                {
                    KeyValuePair<String, IDomainEventHandlerResolver> ehr = eventHandlerResolvers.SingleOrDefault(e => e.Key == "Main");
                    if (ehr.Value != null) {
                        for (Int32 i = 0; i < configurationSubscriptionWorker.InstanceCount; i++) {
                            SubscriptionWorker worker = SubscriptionWorker.CreateSubscriptionWorker(clientSettings,
                                                                                                    ehr.Value,
                                                                                                    subscriptionRepository,
                                                                                                    configurationSubscriptionWorker.InflightMessages,
                                                                                                    configuration.PersistentSubscriptionPollingInSeconds);

                            // TODO: Logging
                            //worker.Trace += (_,
                            //                 args) => Extensions.mainLog(TraceEventType.Information, args.Message);
                            //worker.Warning += (_,
                            //                   args) => Extensions.mainLog(TraceEventType.Warning, args.Message);
                            //worker.Error += (_,
                            //                 args) => Extensions.mainLog(TraceEventType.Error, args.Message);

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
    }
    
    public class SubscriptionWorkersRoot
    {
        public Boolean InternalSubscriptionService { get; set; }
        public Int32 PersistentSubscriptionPollingInSeconds { get; set; }
        public Int32 InternalSubscriptionServiceCacheDuration { get; set; }
        public List<SubscriptionWorkerConfig> SubscriptionWorkers { get; set; }
    }

    public class SubscriptionWorkerConfig
    {
        public String WorkerName { get; set; }
        public String IncludeGroups { get; set; }
        public String IgnoreGroups { get; set; }
        public String IncludeStreams { get; set; }
        public String IgnoreStreams { get; set; }
        public Boolean Enabled { get; set; }
        public Int32 InflightMessages { get; set; }
        public Int32 InstanceCount { get; set; }
        public Boolean IsOrdered { get; set; }
    }

}
