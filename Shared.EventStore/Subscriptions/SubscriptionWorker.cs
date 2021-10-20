namespace Shared.EventStore.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Aggregate;
    using EventHandling;
    using EventStore;
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
        /// Occurs when trace is generated.
        /// </summary>
        public event TraceHandler TraceGenerated;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionWorker"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="env">The env.</param>
        /// <param name="domainEventHandlerResolver">The domain event handler resolver.</param>
        /// <param name="persistentSubscriptionsClient">The persistent subscriptions client.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public SubscriptionWorker(IDomainEventHandlerResolver domainEventHandlerResolver,
                                  EventStorePersistentSubscriptionsClient persistentSubscriptionsClient,
                                  HttpClient httpClient)
        {
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
            Boolean useInternalSubscriptionService = Boolean.Parse(ConfigurationReader.GetValue("UseInternalSubscriptionService"));

            if (useInternalSubscriptionService == false)
                return;

            TypeProvider.LoadDomainEventsTypeDynamically();

            foreach (KeyValuePair<Type, String> type in TypeMap.Map)
            {
                this.LogInformation($"Type name {type.Value} mapped to {type.Key.Name}");
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

                    this.LogInformation($"{result.Count} subscriptions retrieved for Group Filter [{groupFilter}]");

                    foreach (var subscriptionDto in result)
                    {
                        this.LogInformation($"Creating subscription {subscriptionDto.EventStreamId}-{subscriptionDto.GroupName}");

                        PersistentSubscriptionDetails persistentSubscriptionDetails = new(subscriptionDto.EventStreamId, subscriptionDto.GroupName);

                        PersistentSubscription subscription = PersistentSubscription.Create(this.PersistentSubscriptionsClient,
                                                                                            persistentSubscriptionDetails,
                                                                                            this.DomainEventHandlerResolver);

                        await subscription.ConnectToSubscription();

                        this.LogInformation($"Created subscription {subscriptionDto.EventStreamId}-{subscriptionDto.GroupName}");

                        this.CurrentSubscriptions.Add(subscription);
                    }

                    Int32 removedCount = this.CurrentSubscriptions.RemoveAll(p => p.Connected == false);

                    if (removedCount > 0)
                    {
                        this.LogWarning($"Removed {removedCount} subscriptions because SubscriptionDropped.");
                        continue;
                    }
                }
                catch (Exception e)
                {
                    this.LogCritical(e);
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
            List<SubscriptionInformation> subscriptionList = new List<SubscriptionInformation>();
            String requestUri = $"{ConfigurationReader.GetValue("EventStoreSettings", "ConnectionString")}/subscriptions";

            String username = ConfigurationReader.GetValue("EventStoreSettings", "UserName");
            String password = ConfigurationReader.GetValue("EventStoreSettings", "Password");
            String credentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));

            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            HttpResponseMessage responseMessage = await this.HttpClient.SendAsync(requestMessage, cancellationToken);
            String responseData = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            if (responseMessage.IsSuccessStatusCode)
            {
                subscriptionList = JsonConvert.DeserializeObject<List<SubscriptionInformation>>(responseData);
            }
            else
            {
                this.LogWarning($"Error getting subscription list from [{requestUri}] Http Status Code [{responseMessage.StatusCode}] Content [{responseData}]");
            }

            return subscriptionList;
        }

        private void LogDebug(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Debug);
            }
        }

        /// <summary>
        /// Traces the specified exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        private void LogError(Exception exception)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(exception.Message, LogLevel.Error);
                if (exception.InnerException != null)
                {
                    this.LogError(exception.InnerException);
                }
            }
        }

        private void LogCritical(Exception exception)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(exception.Message, LogLevel.Critical);
                if (exception.InnerException != null)
                {
                    this.LogCritical(exception.InnerException);
                }
            }
        }

        /// <summary>
        /// Traces the specified trace.
        /// </summary>
        /// <param name="trace">The trace.</param>
        private void LogInformation(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Information);
            }
        }

        private void LogWarning(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Warning);
            }
        }
    }
}