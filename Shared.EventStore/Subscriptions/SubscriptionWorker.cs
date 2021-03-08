namespace Shared.EventStore.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Aggregate;
    using EventHandling;
    using General;
    using global::EventStore.Client;
    using Logger;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using NLog.Extensions.Logging;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Hosting.BackgroundService" />
    public class SubscriptionWorker : BackgroundService
    {
        /// <summary>
        /// The domain event handler resolver
        /// </summary>
        private readonly IDomainEventHandlerResolver DomainEventHandlerResolver;

        /// <summary>
        /// The persistent subscriptions client
        /// </summary>
        private readonly EventStorePersistentSubscriptionsClient PersistentSubscriptionsClient;

        /// <summary>
        /// The HTTP client
        /// </summary>
        private readonly HttpClient HttpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionWorker"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="env">The env.</param>
        /// <param name="domainEventHandlerResolver">The domain event handler resolver.</param>
        /// <param name="persistentSubscriptionsClient">The persistent subscriptions client.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public SubscriptionWorker(ILoggerFactory loggerFactory,
                                  IWebHostEnvironment env,
                                  IDomainEventHandlerResolver domainEventHandlerResolver,
                                  EventStorePersistentSubscriptionsClient persistentSubscriptionsClient,
                                  HttpClient httpClient)
        {

            String nlogConfigFilename = "nlog.config";

            if (env.IsDevelopment())
            {
                nlogConfigFilename = $"nlog.{env.EnvironmentName}.config";
            }

            loggerFactory.ConfigureNLog(Path.Combine(env.ContentRootPath, nlogConfigFilename));
            loggerFactory.AddNLog();

            Microsoft.Extensions.Logging.ILogger logger = loggerFactory.CreateLogger("MessagingService");

            Logger.Initialise(logger);

            this.DomainEventHandlerResolver = domainEventHandlerResolver;
            this.PersistentSubscriptionsClient = persistentSubscriptionsClient;
            this.HttpClient = httpClient;
        }

        /// <summary>
        /// The current subscriptions
        /// </summary>
        private readonly List<PersistentSubscription> CurrentSubscriptions = new List<PersistentSubscription>();

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {

        }

        /// <summary>
        /// This method is called when the <see cref="T:Microsoft.Extensions.Hosting.IHostedService" /> starts. The implementation should return a task that represents
        /// the lifetime of the long running operation(s) being performed.
        /// </summary>
        /// <param name="stoppingToken">Triggered when <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" /> is called.</param>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task" /> that represents the long running operations.
        /// </returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            TypeProvider.LoadDomainEventsTypeDynamically();

            foreach (KeyValuePair<Type, String> type in TypeMap.Map)
            {
                Logger.LogInformation($"Type name {type.Value} mapped to {type.Key.Name}");
            }

            while (stoppingToken.IsCancellationRequested == false)
            {
                try
                {
                    String[] subscriptionFilters = null;
                    String groupFilter = ConfigurationReader.GetValue("SubscriptionFilter");
                    if (groupFilter.Contains(","))
                    {
                        // Multiple filters
                        subscriptionFilters = groupFilter.Split(",");
                    }
                    else
                    {
                        subscriptionFilters = new[] { groupFilter };
                    }
                    List<SubscriptionInformation> list = await this.GetSubscriptionsList(stoppingToken);
                    var result = list
                                 .Where(p => this.CurrentSubscriptions
                                                 .All(p2 => $"{p2.ToString()}" != $"{p.EventStreamId}-{p.GroupName}")).ToList();

                    result = result.Where(r => subscriptionFilters.Contains(r.GroupName)).ToList();

                    Logger.LogInformation($"{result.Count} subscriptions retrieved for Group Filter [{groupFilter}]");

                    foreach (var subscriptionDto in result)
                    {
                        Logger.LogInformation($"Creating subscription {subscriptionDto.EventStreamId}-{subscriptionDto.GroupName}");

                        PersistentSubscriptionDetails persistentSubscriptionDetails = new(subscriptionDto.EventStreamId, subscriptionDto.GroupName);

                        PersistentSubscription subscription = PersistentSubscription.Create(this.PersistentSubscriptionsClient,
                                                                                            persistentSubscriptionDetails,
                                                                                            this.DomainEventHandlerResolver);

                        await subscription.ConnectToSubscription();

                        Logger.LogInformation($"Created subscription {subscriptionDto.EventStreamId}-{subscriptionDto.GroupName}");

                        this.CurrentSubscriptions.Add(subscription);
                    }

                    Int32 removedCount = this.CurrentSubscriptions.RemoveAll(p => p.Connected == false);

                    if (removedCount > 0)
                    {
                        Logger.LogWarning($"Removed {removedCount} subscriptions because SubscriptionDropped.");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogCritical(e);
                }

                String persistentSubscriptionPollingInSeconds = ConfigurationReader.GetValue("EventStoreSettings", "PersistentSubscriptionPollingInSeconds");
                if (String.IsNullOrEmpty(persistentSubscriptionPollingInSeconds))
                {
                    persistentSubscriptionPollingInSeconds = "60";
                }

                // Delay for configured seconds before polling the eventstore again
                await Task.Delay(TimeSpan.FromSeconds(Int32.Parse(persistentSubscriptionPollingInSeconds)), stoppingToken);
            }
        }

        /// <summary>
        /// Gets the subscriptions list.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        private async Task<List<SubscriptionInformation>> GetSubscriptionsList(CancellationToken cancellationToken)
        {
            List<SubscriptionInformation> subscriptionList = null;
            String requestUri = $"{ConfigurationReader.GetValue("EventStoreSettings", "ConnectionString")}/subscriptions";


            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);

            HttpResponseMessage responseMessage = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

            if (responseMessage.IsSuccessStatusCode)
            {
                String responseData = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
                subscriptionList = JsonConvert.DeserializeObject<List<SubscriptionInformation>>(responseData);
            }

            return subscriptionList;
        }
    }
}