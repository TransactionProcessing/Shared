//using Microsoft.Extensions.Logging;
using ESPersistentSubscription = EventStore.Client.PersistentSubscription;

namespace Shared.EventStore.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Aggregate;
    using DomainDrivenDesign.EventSourcing;
    using EventHandling;
    using global::EventStore.Client;
    using Logger = Logger.Logger;

    public class PersistentSubscription
    {
        #region Fields

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override String ToString()
        {
            return $"{this.PersistentSubscriptionDetails.StreamName}-{this.PersistentSubscriptionDetails.GroupName}";
        }

        /// <summary>
        /// The persistent subscriptions client
        /// </summary>
        private readonly EventStorePersistentSubscriptionsClient PersistentSubscriptionsClient;

        /// <summary>
        /// The domain event handlers
        /// </summary>
        private readonly IDomainEventHandlerResolver DomainEventHandlerResolver;

        public PersistentSubscriptionDetails PersistentSubscriptionDetails { get; }

        /// <summary>
        /// The user credentials
        /// </summary>
        private readonly UserCredentials UserCredentials;

        /// <summary>
        /// Gets the event store persistent subscription.
        /// </summary>
        /// <value>
        /// The event store persistent subscription.
        /// </value>
        public ESPersistentSubscription EventStorePersistentSubscription { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="PersistentSubscription"/> is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if connected; otherwise, <c>false</c>.
        /// </value>
        public Boolean Connected { get; private set; }

        #endregion

        #region Constructors
        
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistentSubscription"/> class.
        /// </summary>
        /// <param name="persistentSubscriptionsClient">The persistent subscriptions client.</param>
        /// <param name="persistentSubscriptionDetails">The persistent subscription details.</param>
        /// <param name="domainEventHandlerResolver">The domain event handler resolver.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        private PersistentSubscription(EventStorePersistentSubscriptionsClient persistentSubscriptionsClient,
                                       PersistentSubscriptionDetails persistentSubscriptionDetails,
                                       IDomainEventHandlerResolver domainEventHandlerResolver,
                                       String username,
                                       String password)
        {
            this.PersistentSubscriptionDetails = persistentSubscriptionDetails;
            this.PersistentSubscriptionsClient = persistentSubscriptionsClient;
            this.DomainEventHandlerResolver = domainEventHandlerResolver;
            this.UserCredentials = new UserCredentials(username, password);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Connects to subscription.
        /// </summary>
        public async Task ConnectToSubscription()
        {
            try
            {
                this.EventStorePersistentSubscription = await this.PersistentSubscriptionsClient.SubscribeAsync(this.PersistentSubscriptionDetails.StreamName,
                                                                                                                this.PersistentSubscriptionDetails.GroupName,
                                                                                                                this.EventAppeared,
                                                                                                                this.SubscriptionDropped,
                                                                                                                this.UserCredentials,
                                                                                                                this.PersistentSubscriptionDetails.InflightCount == 0 ? 200 : this.PersistentSubscriptionDetails.InflightCount,
                                                                                                                false);



                this.Connected = true;
            }
            catch (Exception e)
            {
                //TODO: Should we kill the process?
                Logger.LogError(e);
            }
        }

        /// <summary>
        /// Creates the specified persistent subscriptions client.
        /// </summary>
        /// <param name="persistentSubscriptionsClient">The persistent subscriptions client.</param>
        /// <param name="persistentSubscriptionDetails">The persistent subscription details.</param>
        /// <param name="domainEventHandlerResolver">The domain event handler resolver.</param>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public static PersistentSubscription Create(EventStorePersistentSubscriptionsClient persistentSubscriptionsClient,
                                                            PersistentSubscriptionDetails persistentSubscriptionDetails,
                                                            IDomainEventHandlerResolver domainEventHandlerResolver,
                                                            String username = "admin",
                                                            String password = "changeit") =>

                     new(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver, username, password);


        /// <summary>
        /// Events the appeared.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="resolvedEvent">The resolved event.</param>
        private async Task EventAppeared(ESPersistentSubscription subscription,
                                         ResolvedEvent resolvedEvent,
                                         Int32? x,
                                         CancellationToken cancellationToken)
        {
            try
            {
                if (resolvedEvent.Event == null)
                {
                    // This indicates we have a gimpy event so just ignore it as nothing can be done :|
                    await subscription.Ack(resolvedEvent);
                    return;
                }

                if (resolvedEvent.Event.EventType.StartsWith("$"))
                {
                    await subscription.Ack(resolvedEvent);
                    return;
                }

                Console.WriteLine($"EventAppearedFromPersistentSubscription with Event Id {resolvedEvent.Event.EventId}");
                Logger.LogInformation($"EventAppearedFromPersistentSubscription with Event Id {resolvedEvent.Event.EventId}");

                IDomainEvent domainEvent = TypeMapConvertor.Convertor(Guid.Empty, resolvedEvent);

                List<Task> tasks = new();
                List<IDomainEventHandler> domainEventHandlers = this.DomainEventHandlerResolver.GetDomainEventHandlers(domainEvent);

                if (domainEventHandlers == null || domainEventHandlers.Any() == false)
                {
                    // Log a warning out 
                    Logger.LogWarning($"No event handlers configured for Event Type [{domainEvent.GetType().Name}]");
                    await subscription.Ack(resolvedEvent);
                    return;
                }

                foreach (IDomainEventHandler domainEventHandler in domainEventHandlers)
                {
                    tasks.Add(domainEventHandler.Handle(domainEvent, CancellationToken.None));
                }

                var t = Task.WhenAll(tasks.ToArray());

                try
                {
                    await t;
                }
                catch
                {
                    Logger.LogError(t.Exception);
                    throw t.Exception;
                }

                await subscription.Ack(resolvedEvent);
            }
            catch (Exception e)
            {
                Exception ex = new Exception($"Failed to process the event {resolvedEvent.GetResolvedEventDataAsString()}", e);

                Logger.LogError(ex);
            }
        }

        /// <summary>
        /// Subscriptions the dropped.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="droppedReason">The dropped reason.</param>
        /// <param name="exception">The exception.</param>
        private void SubscriptionDropped(ESPersistentSubscription subscription,
                                         SubscriptionDroppedReason droppedReason,
                                         Exception? exception)
        {
            this.Connected = false;
            Logger.LogError(new Exception($"Subscription dropped {droppedReason.ToString()}", exception));
        }

        #endregion
    }
}
