namespace Shared.EventStore.SubscriptionWorker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using EventHandling;
    using global::EventStore.Client;

    public class SubscriptionWorker
    {
        #region Fields

        internal readonly List<PersistentSubscription> CurrentSubscriptions = new();

        public Func<List<PersistentSubscriptionInfo>, List<PersistentSubscriptionInfo>, List<PersistentSubscriptionInfo>> GetNewSubscriptions;

        private readonly List<IDomainEventHandler> EventHandlers;

        private global::EventStore.Client.EventStorePersistentSubscriptionsClient EventStorePersistentSubscriptionsClient;

        private readonly IDomainEventHandlerResolver DomainEventHandlerResolver;

        private readonly ISubscriptionRepository SubscriptionRepository;

        private Thread WorkerThread;

        private readonly Action<String> WriteError;

        private readonly Action<String> WriteTrace;

        private readonly Action<String> WriteWarning;

        #endregion

        #region Constructors

        private SubscriptionWorker(String eventStoreConnectionString,
                                   IDomainEventHandlerResolver domainEventHandlerResolver,
                                   ISubscriptionRepository subscriptionRepository,
                                   Int32 persistentSubscriptionPollingInSeconds = 60)
        {
            this.DomainEventHandlerResolver = domainEventHandlerResolver;
            this.SubscriptionRepository = subscriptionRepository;
            this.EventStorePersistentSubscriptionsClient = new(EventStoreClientSettings.Create(eventStoreConnectionString));

            this.PersistentSubscriptionPollingInSeconds = persistentSubscriptionPollingInSeconds;

            EventStoreClientSettings settings = EventStoreClientSettings.Create(eventStoreConnectionString);
            this.HttpClient = SubscriptionWorkerHelper.CreateHttpClient(settings);

            this.IgnoreSubscriptions = "local-"; //Default behaviour

            this.GetNewSubscriptions = (all, current)
                                           => SubscriptionWorkerHelper.GetNewSubscriptions(all,
                                                                                           current,
                                                                                           this.IsOrdered,
                                                                                           this.IgnoreSubscriptions,
                                                                                           this.FilterSubscriptions,
                                                                                           this.StreamNameFilter);

            this.WriteTrace = message => SubscriptionWorkerHelper.SafeInvokeEvent(this.Trace, this, message);
            this.WriteWarning = message => SubscriptionWorkerHelper.SafeInvokeEvent(this.Warning, this, message);
            this.WriteError = message => SubscriptionWorkerHelper.SafeInvokeEvent(this.Error, this, message);
        }

        private SubscriptionWorker(EventStoreClientSettings eventStoreConnectionSettings,
                                   IDomainEventHandlerResolver domainEventHandlerResolver,
                                   ISubscriptionRepository subscriptionRepository,
                                   Int32 persistentSubscriptionPollingInSeconds = 60)
        {
            this.DomainEventHandlerResolver = domainEventHandlerResolver;
            this.SubscriptionRepository = subscriptionRepository;
            this.EventStorePersistentSubscriptionsClient = new(eventStoreConnectionSettings);

            this.PersistentSubscriptionPollingInSeconds = persistentSubscriptionPollingInSeconds;

            EventStoreClientSettings settings = eventStoreConnectionSettings;
            this.HttpClient = SubscriptionWorkerHelper.CreateHttpClient(settings);

            this.IgnoreSubscriptions = "local-"; //Default behaviour

            this.GetNewSubscriptions = (all, current)
                                           => SubscriptionWorkerHelper.GetNewSubscriptions(all,
                                                                                           current,
                                                                                           this.IsOrdered,
                                                                                           this.IgnoreSubscriptions,
                                                                                           this.FilterSubscriptions,
                                                                                           this.StreamNameFilter);

            this.WriteTrace = message => SubscriptionWorkerHelper.SafeInvokeEvent(this.Trace, this, message);
            this.WriteWarning = message => SubscriptionWorkerHelper.SafeInvokeEvent(this.Warning, this, message);
            this.WriteError = message => SubscriptionWorkerHelper.SafeInvokeEvent(this.Error, this, message);
        }

        #endregion

        #region Properties

        public String FilterSubscriptions { get; internal set; }
        public HttpClient HttpClient { get; internal set; }
        public String IgnoreSubscriptions { get; internal set; }
        public Int32 InflightMessages { get; internal set; }
        public Boolean InMemory { get; internal set; }
        public Boolean IsOrdered { get; internal set; }
        public Boolean IsRunning { get; private set; }
        public Int32 PersistentSubscriptionPollingInSeconds { get; }
        public String StreamNameFilter { get; internal set; }

        #endregion

        #region Events

        public event EventHandler<TraceEventArgs> Error;
        public event EventHandler<TraceEventArgs> Trace;
        public event EventHandler<TraceEventArgs> Warning;

        #endregion

        #region Methods

        public static SubscriptionWorker CreateConcurrentSubscriptionWorker(String eventStoreConnectionString,
                                                                            IDomainEventHandlerResolver domainEventHandlerResolver,
                                                                            ISubscriptionRepository subscriptionRepository,
                                                                            Int32 inflightMessages = 200,
                                                                            Int32 persistentSubscriptionPollingInSeconds = 60)
        {
            return new(eventStoreConnectionString, domainEventHandlerResolver, subscriptionRepository, persistentSubscriptionPollingInSeconds)
                   {
                       InflightMessages = inflightMessages
                   };
        }

        public static SubscriptionWorker CreateConcurrentSubscriptionWorker(EventStoreClientSettings eventStoreConnectionSettings,
                                                                            IDomainEventHandlerResolver domainEventHandlerResolver,
                                                                            ISubscriptionRepository subscriptionRepository,
                                                                            Int32 inflightMessages = 200,
                                                                            Int32 persistentSubscriptionPollingInSeconds = 60)
        {
            return new(eventStoreConnectionSettings, domainEventHandlerResolver, subscriptionRepository, persistentSubscriptionPollingInSeconds)
                   {
                       InflightMessages = inflightMessages
                   };
        }

        public static SubscriptionWorker CreateOrderedSubscriptionWorker(EventStoreClientSettings eventStoreConnectionSettings,
                                                                         IDomainEventHandlerResolver domainEventHandlerResolver,
                                                                         ISubscriptionRepository subscriptionRepository,
                                                                         Int32 persistentSubscriptionPollingInSeconds = 60)
        {
            return new(eventStoreConnectionSettings, domainEventHandlerResolver, subscriptionRepository, persistentSubscriptionPollingInSeconds)
                   {
                       InflightMessages = 1,
                       IsOrdered = true
                   };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //TODO: What if we are already running?

            try
            {
                this.WorkerThread = new Thread(async () => await this.ExecuteAsync(cancellationToken));
                this.WorkerThread.Start();

                this.IsRunning = true;
            }
            catch (Exception e)
            {
                this.WriteError(e.Message);

                //We still want to bring the service down.
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            // Close the ES Connection
            this.WriteTrace("About to close EventStore Connection");

            await this.EventStorePersistentSubscriptionsClient.DisposeAsync();
            this.EventStorePersistentSubscriptionsClient = null;

            this.IsRunning = false;

            var temp = this.CurrentSubscriptions.Select(r => r).ToList();

            for (Int32 i = 0; i < temp.Count; i++)
            {
                this.SubscriptionDropped(temp[i], "Worker stopped");
            }
        }

        private async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    PersistentSubscriptions all = await this.SubscriptionRepository.GetSubscriptions(false, stoppingToken);

                    var current = this.CurrentSubscriptions.Select(x => new PersistentSubscriptionInfo
                                                                        {
                                                                            GroupName = x.PersistentSubscriptionDetails.GroupName,
                                                                            StreamName = x.PersistentSubscriptionDetails.StreamName
                                                                        }).ToList();

                    var result = this.GetNewSubscriptions(all.PersistentSubscriptionInfo, current);

                    this.WriteWarning($"Picked up {result.Count} subscriptions");

                    // Check we have retrieved back some configuration
                    foreach (PersistentSubscriptionInfo subscriptionDto in result)
                    {
                        this.WriteWarning($"Creating subscription [{subscriptionDto.StreamName}-{subscriptionDto.GroupName}]");

                        PersistentSubscriptionDetails persistentSubscriptionDetails = new(subscriptionDto.StreamName, subscriptionDto.GroupName)
                                                                                      {
                                                                                          InflightMessages = this.InflightMessages
                                                                                      };
                        IPersistentSubscriptionsClient persistentSubscriptionsClient;

                        if (this.InMemory)
                        {
                            persistentSubscriptionsClient = new InMemoryPersistentSubscriptionsClient();
                        }
                        else
                        {
                            persistentSubscriptionsClient = new EventStorePersistentSubscriptionsClient(this.EventStorePersistentSubscriptionsClient);
                        }

                        PersistentSubscription subscription =
                            PersistentSubscription.Create(persistentSubscriptionsClient, persistentSubscriptionDetails, this.DomainEventHandlerResolver);

                        subscription.SubscriptionHasDropped += (sender, args) => this.SubscriptionDropped((PersistentSubscription)sender, args);

                        await subscription.ConnectToSubscription(stoppingToken);

                        this.WriteWarning($"Subscription [{subscriptionDto.StreamName}-{subscriptionDto.GroupName}] connected");

                        this.CurrentSubscriptions.Add(subscription);
                    }
                }
                catch (Exception e)
                {
                    this.WriteError(e.Message);
                }

                // Delay for 60 seconds before polling the database again
                await Task.Delay(TimeSpan.FromSeconds(this.PersistentSubscriptionPollingInSeconds), stoppingToken);
            }
        }

        private async void SubscriptionDropped(PersistentSubscription subscription, String error)
        {
            this.WriteWarning($"Stopping [{subscription}] subscription because SubscriptionDropped {error}.");

            try
            {
                if (subscription.EventStorePersistentSubscription != null)
                {
                    subscription.EventStorePersistentSubscription.Dispose();
                }

                this.CurrentSubscriptions.Remove(subscription);
                var delegates = subscription.SubscriptionHasDropped?.GetInvocationList();

                if (delegates != null)
                {
                    foreach (Delegate d in delegates)
                    {
                        this.WriteWarning("Removing SubscriptionHasDropped eventHandler.");
                        subscription.SubscriptionHasDropped -= (EventHandler<String>)d;
                    }
                }

                this.WriteWarning($"Stopped [{subscription}] subscription.");

                if (this.IsRunning)
                {
                    //When we refresh the GetSubscriptions, we need to force refresh
                    this.WriteWarning("About force refresh of repository");

                    //This could time out waiting on a lock, hence the try catch
                    await this.SubscriptionRepository.GetSubscriptions(true, CancellationToken.None);
                }
            }
            catch (Exception e)
            {
                this.WriteError($"Error occurred {e.Message}.");
            }
        }

        #endregion
    }
}