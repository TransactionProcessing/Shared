namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EventHandling;
using global::EventStore.Client;

[ExcludeFromCodeCoverage]
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

        this.GetNewSubscriptions = (all, current)
            => SubscriptionWorkerHelper.GetNewSubscriptions(all,
                current,
                this.IncludeGroups,
                this.IgnoreGroups,
                this.IncludeStreams,
                this.IgnoreStreams);

        this.WriteTrace = message => SubscriptionWorkerHelper.SafeInvokeEvent(this.Trace, this, message);
        this.WriteWarning = message => SubscriptionWorkerHelper.SafeInvokeEvent(this.Warning, this, message);
        this.WriteError = message => SubscriptionWorkerHelper.SafeInvokeEvent(this.Error, this, message);
    }

    #endregion

    #region Properties

    public String IncludeGroups { get; internal set; }
    public String IgnoreGroups { get; internal set; }
    public String IncludeStreams { get; internal set; }
    public String IgnoreStreams { get; internal set; }
    public HttpClient HttpClient { get; internal set; }
    public String IgnoreSubscriptions { get; internal set; }
    public Int32 InflightMessages { get; internal set; }
    public Boolean InMemory { get; internal set; }
    public Boolean IsRunning { get; private set; }
    public Int32 PersistentSubscriptionPollingInSeconds { get; }
        
    #endregion

    #region Events

    public event EventHandler<TraceEventArgs> Error;
    public event EventHandler<TraceEventArgs> Trace;
    public event EventHandler<TraceEventArgs> Warning;

    #endregion

    #region Methods

    public static SubscriptionWorker CreateSubscriptionWorker(String eventStoreConnectionString,
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

    public static SubscriptionWorker CreateOrderedSubscriptionWorker(String eventStoreConnectionString,
                                                                     IDomainEventHandlerResolver domainEventHandlerResolver,
                                                                     ISubscriptionRepository subscriptionRepository,
                                                                     Int32 persistentSubscriptionPollingInSeconds = 60)
    {
        return new(eventStoreConnectionString, domainEventHandlerResolver, subscriptionRepository, persistentSubscriptionPollingInSeconds)
        {
            InflightMessages = 1
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

        var temp = this.CurrentSubscriptions.ToList();

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

                List<PersistentSubscriptionInfo> current = this.CurrentSubscriptions.Select(x => new PersistentSubscriptionInfo
                {
                    GroupName = x.PersistentSubscriptionDetails.GroupName,
                    StreamName = x.PersistentSubscriptionDetails.StreamName
                }).ToList();

                List<PersistentSubscriptionInfo> result = this.GetNewSubscriptions(all.PersistentSubscriptionInfo, current);

                if (result.Count > 0)
                {
                    this.WriteWarning($"Picked up {result.Count} subscriptions");

                    // Check we have retrieved back some configuration
                    foreach (PersistentSubscriptionInfo subscriptionDto in result)
                    {
                        this.WriteWarning(
                            $"Creating subscription [{subscriptionDto.StreamName}-{subscriptionDto.GroupName}]");

                        PersistentSubscriptionDetails persistentSubscriptionDetails =
                            new(subscriptionDto.StreamName, subscriptionDto.GroupName)
                            {
                                InflightMessages = this.InflightMessages
                            };
                        IPersistentSubscriptionsClient persistentSubscriptionsClient = this.InMemory
                            ? new InMemoryPersistentSubscriptionsClient()
                            : new EventStorePersistentSubscriptionsClient(this.EventStorePersistentSubscriptionsClient);

                        PersistentSubscription subscription =
                            PersistentSubscription.Create(persistentSubscriptionsClient,
                                persistentSubscriptionDetails,
                                this.DomainEventHandlerResolver);

                        subscription.SubscriptionHasDropped += (sender, args) =>
                            this.SubscriptionDropped((PersistentSubscription)sender, args);

                        await subscription.ConnectToSubscription(stoppingToken);

                        this.WriteWarning(
                            $"Subscription [{subscriptionDto.StreamName}-{subscriptionDto.GroupName}] connected");

                        this.CurrentSubscriptions.Add(subscription);
                    }
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