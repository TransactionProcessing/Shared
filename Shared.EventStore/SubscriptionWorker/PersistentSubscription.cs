using SimpleResults;

namespace Shared.EventStore.SubscriptionWorker
{
    using Aggregate;
    using DomainDrivenDesign.EventSourcing;
    using EventHandling;
    using global::EventStore.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Shared.General;
    using Shared.TennantContext;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [ExcludeFromCodeCoverage]
    public class PersistentSubscription
    {
        #region Fields

        public EventHandler<String> SubscriptionHasDropped;

        private readonly Func<CancellationToken, Task<global::EventStore.Client.PersistentSubscription>> Subscribe;

        #endregion

        #region Constructors

        private void SubscriptionDropped(global::EventStore.Client.PersistentSubscription arg1,
                                         SubscriptionDroppedReason arg2,
                                         Exception? arg3) =>
            this.SubscriptionDropped(arg2.ToString());

        public void SubscriptionDropped(String reason)
        {
            this.Connected = false;
            Logger.Logger.LogWarning($"Subscription dropped - {reason}");

            if (this.SubscriptionHasDropped != null)
            {
                //Broadcast to owner
                this.SubscriptionHasDropped(this, reason);
            }
        }

        private PersistentSubscription(IPersistentSubscriptionsClient persistentSubscriptionsClient,
                                       PersistentSubscriptionDetails persistentSubscriptionDetails,
                                       IDomainEventHandlerResolver domainEventHandlerResolver,
                                       String username,
                                       String password)
        {
            this.PersistentSubscriptionsClient = persistentSubscriptionsClient;
            this.PersistentSubscriptionDetails = persistentSubscriptionDetails;
            UserCredentials userCredentials = new(username, password);

            Func<global::EventStore.Client.PersistentSubscription, ResolvedEvent, Int32?, CancellationToken, Task> eventAppeared = (ps, re, retryCount, ct) => PersistentSubscription.EventAppeared(ps, re, retryCount, domainEventHandlerResolver, ct);

            this.Subscribe = ct => persistentSubscriptionsClient.SubscribeAsync(this.PersistentSubscriptionDetails.StreamName,
                                                                                this.PersistentSubscriptionDetails.GroupName,
                                                                                eventAppeared,
                                                                                this.SubscriptionDropped,
                                                                                userCredentials,
                                                                                persistentSubscriptionDetails.InflightMessages,
                                                                                ct);
        }

        #endregion

        #region Properties

        public Boolean Connected { get; private set; }
        public global::EventStore.Client.PersistentSubscription EventStorePersistentSubscription { get; private set; }
        public IPersistentSubscriptionsClient PersistentSubscriptionsClient { get; }
        public PersistentSubscriptionDetails PersistentSubscriptionDetails { get; }

        #endregion

        #region Methods

        public async Task ConnectToSubscription(CancellationToken cancellationToken)
        {
            try
            {
                this.EventStorePersistentSubscription = await this.Subscribe(cancellationToken);

                this.Connected = true;
            }
            catch (Exception e)
            {
                //TODO: Should we kill the process?
                Logger.Logger.LogError(e);
            }
        }

        public static PersistentSubscription Create(IPersistentSubscriptionsClient persistentSubscriptionsClient,
                                                    PersistentSubscriptionDetails persistentSubscriptionDetails,
                                                    IDomainEventHandlerResolver domainEventHandlerResolver,
                                                    String username = "admin",
                                                    String password = "changeit") => new(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver, username, password);


        public override String ToString() => $"{this.PersistentSubscriptionDetails.StreamName}-{this.PersistentSubscriptionDetails.GroupName}";

        private static TenantIdentifiers GetTenantIdentifiersFromDomainEvent(IDomainEvent domainEvent)
        {
            String domainEventAsString = JsonConvert.SerializeObject(domainEvent);

            try
            {
                JToken rootToken = JToken.Parse(domainEventAsString);

                JToken estateIdIdToken = rootToken.SelectTokens("..estateId").FirstOrDefault() ??
                                         rootToken.SelectTokens("..EstateId").FirstOrDefault();

                JToken merchantIdToken = rootToken.SelectTokens("..merchantId").FirstOrDefault() ??
                                         rootToken.SelectTokens("..MerchantId").FirstOrDefault();

                Guid.TryParse(estateIdIdToken?.Value<String>(), out Guid estateId);
                Guid.TryParse(merchantIdToken?.Value<String>(), out Guid merchantId);

                return estateId == Guid.Empty ? TenantIdentifiers.Default() : new TenantIdentifiers(estateId, Guid.Empty);
            }
            catch (Exception)
            {
                Logger.Logger.LogWarning($"Unable to get estateId from request body [{domainEventAsString}]");
                return TenantIdentifiers.Default();
            }
        }

        internal static async Task EventAppeared(global::EventStore.Client.PersistentSubscription persistentSubscription,
                                                 ResolvedEvent resolvedEvent,
                                                 Int32? retryCount,
                                                 IDomainEventHandlerResolver domainEventHandlerResolver,
                                                 CancellationToken cancellationToken)
        {
            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    if (resolvedEvent.SilentlyHandleEvent())
                    {
                        await PersistentSubscriptionsHelper.AckEvent(persistentSubscription, resolvedEvent);
                        return;
                    }
                    IDomainEvent domainEvent = TypeMapConvertor.Convertor(resolvedEvent);
                    TenantIdentifiers tenantIdentifiers = PersistentSubscription.GetTenantIdentifiersFromDomainEvent(domainEvent);
                    Boolean.TryParse(ConfigurationReader.GetValueOrDefault("AppSettings","LogsPerTenantEnabled", "false"), out Boolean logPerTenantEnabled);

                    TenantContext tenantContext = new();
                    tenantContext.Initialise(tenantIdentifiers, logPerTenantEnabled);
                    TenantContext.CurrentTenant = tenantContext;

                    Logger.Logger.LogInformation(
                        $"EventAppearedFromPersistentSubscription with Event Id {resolvedEvent.Event.EventId} event type {resolvedEvent.Event.EventType}");
                    
                    List<IDomainEventHandler> domainEventHandlers =
                        domainEventHandlerResolver.GetDomainEventHandlers(domainEvent);

                    if (domainEventHandlers == null || domainEventHandlers.Any() == false)
                    {
                        // Log a warning out 
                        Logger.Logger.LogWarning(
                            $"No event handlers configured for Event Type [{domainEvent.GetType().Name}]");
                        await PersistentSubscriptionsHelper.AckEvent(persistentSubscription, resolvedEvent);
                        return;
                    }

                    Result result = await domainEvent.DispatchToHandlers(domainEventHandlers, cts.Token);
                    if (result.IsSuccess) {
                        await PersistentSubscriptionsHelper.AckEvent(persistentSubscription, resolvedEvent);
                    }
                    else {
                        Exception ex = new($"Failed to process the event type {resolvedEvent.Event.EventType} {resolvedEvent.GetResolvedEventDataAsString()} Result was {result.Message}");
                        Logger.Logger.LogError(ex);
                    }
                }
            }
            catch (Exception e)
            {
                Exception ex = new($"Failed to process the event type {resolvedEvent.Event.EventType} {resolvedEvent.GetResolvedEventDataAsString()}", e);
                
                Logger.Logger.LogError(ex);
            }
        }



        #endregion
    }
}