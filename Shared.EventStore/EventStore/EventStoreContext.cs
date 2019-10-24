namespace Shared.DomainDrivenDesign.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using EventSourcing;
    using global::EventStore.ClientAPI;
    using global::EventStore.ClientAPI.Common.Log;
    using global::EventStore.ClientAPI.Projections;
    using global::EventStore.ClientAPI.SystemData;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Shared.DomainDrivenDesign.EventSourcing;

    /// <summary>
    /// Delegate TraceHandler
    /// </summary>
    /// <param name="trace">The trace.</param>
    public delegate void TraceHandler(String trace, LogLevel logLevel);

    public class EventStoreContext : IEventStoreContext
    {
        #region Fields

        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        private IEventStoreConnection Connection;

        /// <summary>
        /// The connection resolver
        /// </summary>
        private readonly Func<EventStoreConnectionSettings, IEventStoreConnection> ConnectionResolver;

        /// <summary>
        /// The create lock object
        /// </summary>
        private readonly Object CreateLockObject = new Object();

        /// <summary>
        /// Connection details
        /// </summary>
        private readonly EventStoreConnectionSettings EventStoreConnectionSettings;

        /// <summary>
        /// The user credentials
        /// </summary>
        private readonly UserCredentials UserCredentials;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreContext" /> class.
        /// </summary>
        /// <param name="eventStoreConnectionSettings">The event store connection settings.</param>
        /// <param name="connectionResolver">The connection resolver.</param>
        public EventStoreContext(EventStoreConnectionSettings eventStoreConnectionSettings,
                                 Func<EventStoreConnectionSettings, IEventStoreConnection> connectionResolver)
        {
            this.GuardAgainstNullConnectionSettings(eventStoreConnectionSettings);

            // Cache the settings
            this.EventStoreConnectionSettings = eventStoreConnectionSettings;
            this.ConnectionResolver = connectionResolver;

            // Create a set of Cached User Credentials
            this.UserCredentials = new UserCredentials(eventStoreConnectionSettings.UserName, eventStoreConnectionSettings.Password);
        }

        private void GuardAgainstNullConnectionSettings(EventStoreConnectionSettings eventStoreConnectionSettings)
        {
            if (eventStoreConnectionSettings == null)
            {
                throw new ArgumentNullException(nameof(eventStoreConnectionSettings), "EventStoreConnectionSettings cannot be null");
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [connection destroyed].
        /// </summary>
        public event EventHandler ConnectionDestroyed;

        /// <summary>
        /// Occurs when [event appeared].
        /// </summary>
        public event EventAppearedEventHandler EventAppeared;

        /// <summary>
        /// Occurs when [live process started].
        /// </summary>
        public event LiveProcessStartedEventHandler LiveProcessStarted;

        /// <summary>
        /// Occurs when [subscription dropped].
        /// </summary>
        public event SubscriptionDroppedEventHandler SubscriptionDropped;

        #endregion

        #region Methods

        /// <summary>
        /// Connects to subscription.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="subscriptionGroupId">The subscription group identifier.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <returns></returns>
        public async Task ConnectToSubscription(String streamName,
                                                String groupName,
                                                Guid subscriptionGroupId,
                                                Int32 bufferSize)
        {
            this.GuardAgainstNullOrEmptyStreamName(streamName);
            this.GuardAgainstNullOrEmptyGroupName(groupName);
            this.GuardAgainstEmptySubscriptionGroupId(subscriptionGroupId);

            // Setup the Event Appeared delegate
            Action<EventStorePersistentSubscriptionBase, ResolvedEvent> eventAppeared = (subscription,
                                                                                         resolvedEvent) =>
                                                                                            this.PersistentSubscriptionEventAppeared(subscription,
                                                                                                                                     resolvedEvent,
                                                                                                                                     subscriptionGroupId);

            // Setup the Subscription Droppped delegate
            Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> subscriptionDropped = (subscription,
                                                                                                                   reason,
                                                                                                                   ex) => this.PersistentSubscriptionDropped(streamName,
                                                                                                                                                             groupName,
                                                                                                                                                             reason,
                                                                                                                                                             ex,
                                                                                                                                                             subscriptionGroupId);

            IEventStoreConnection connection = null;

            try
            {
                // Get the connection
                connection = await this.GetEventStoreConnection();

                // Now connect
                await this.Connection.ConnectToPersistentSubscriptionAsync(streamName, groupName, eventAppeared, subscriptionDropped, this.UserCredentials, 10, false);
            }
            catch(Exception ex)
            {
                // Check if the exception is because group doesnt exist
                if (ex.InnerException != null && ex.InnerException.Message == "Subscription not found")
                {
                    // Create the missing Group
                    await this.CreatePersistentSubscriptionFromBeginning(streamName, groupName);

                    // Retry the connection
                    await connection.ConnectToPersistentSubscriptionAsync(streamName, groupName, eventAppeared, subscriptionDropped, this.UserCredentials, 10, false);
                }
                else
                {
                    // Some other exception has happened so just throw to caller
                    throw;
                }
            }
        }
        /// <summary>
        /// Occurs when trace is generated.
        /// </summary>
        public event TraceHandler TraceGenerated;

        private void GuardAgainstEmptySubscriptionGroupId(Guid subscriptionGroupId)
        {
            if (subscriptionGroupId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(subscriptionGroupId), "Subscription Group Id cannot be an empty GUID when connecting to a Subscription");
            }
        }

        private void GuardAgainstNullOrEmptyGroupName(String groupName)
        {
            if (String.IsNullOrEmpty(groupName))
            {
                throw new ArgumentNullException(nameof(groupName), "Group Name cannot be null or empty when connecting to a Subscription");
            }
        }

        private void GuardAgainstNullOrEmptyStreamName(String streamName)
        {
            if (String.IsNullOrEmpty(streamName))
            {
                throw new ArgumentNullException(nameof(streamName), "Stream Name cannot be null or empty when connecting to a Subscription");
            }
        }

        /// <summary>
        /// Creates the new persistent subscription.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        public async Task CreateNewPersistentSubscription(String stream,
                                                          String groupName)
        {
            PersistentSubscriptionSettingsBuilder settingsBuilder =
                PersistentSubscriptionSettings.Create().ResolveLinkTos().WithMaxRetriesOf(10).WithMessageTimeoutOf(TimeSpan.FromSeconds(10));

            await this.CreatePersistentSubscription(stream, groupName, settingsBuilder.Build());
        }

        /// <summary>
        /// Creates the persistent subscription from beginning.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        public async Task CreatePersistentSubscriptionFromBeginning(String stream,
                                                                    String groupName)
        {
            PersistentSubscriptionSettingsBuilder settingsBuilder = PersistentSubscriptionSettings
                                                                    .Create().ResolveLinkTos().WithMaxRetriesOf(10).WithMessageTimeoutOf(TimeSpan.FromSeconds(10))
                                                                    .StartFromBeginning();

            await this.CreatePersistentSubscription(stream, groupName, settingsBuilder.Build());
        }

        /// <summary>
        /// Creates the persistent subscription from current.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        public async Task CreatePersistentSubscriptionFromCurrent(String stream,
                                                                  String groupName)
        {
            PersistentSubscriptionSettingsBuilder settingsBuilder = PersistentSubscriptionSettings
                                                                    .Create().ResolveLinkTos().WithMaxRetriesOf(10).WithMessageTimeoutOf(TimeSpan.FromSeconds(10))
                                                                    .StartFromCurrent();

            await this.CreatePersistentSubscription(stream, groupName, settingsBuilder.Build());
        }

        /// <summary>
        /// Creates the persistent subscription from position.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        public async Task CreatePersistentSubscriptionFromPosition(String stream,
                                                                   String groupName,
                                                                   Int32 position)
        {
            PersistentSubscriptionSettingsBuilder settingsBuilder = PersistentSubscriptionSettings
                                                                    .Create().ResolveLinkTos().WithMaxRetriesOf(10).WithMessageTimeoutOf(TimeSpan.FromSeconds(10))
                                                                    .StartFrom(position);

            await this.CreatePersistentSubscription(stream, groupName, settingsBuilder.Build());
        }

        /// <summary>
        /// Deletes the persistent subscription.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <returns></returns>
        public async Task DeletePersistentSubscription(String stream,
                                                       String groupName)
        {
            // Get the connection
            IEventStoreConnection connection = await this.GetEventStoreConnection();

            await connection.DeletePersistentSubscriptionAsync(stream, groupName, this.UserCredentials);
        }

        /// <summary>
        /// Gets the partition result from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="partitionId">The partition identifier.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<String> GetPartitionResultFromProjection(String projectionName,
                                                                   String partitionId)
        {
            ProjectionsManager projectionsManager = this.CreateProjectionsManager();

            return await projectionsManager.GetPartitionResultAsync(projectionName, partitionId, this.UserCredentials);
        }

        /// <summary>
        /// Gets the partition state from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <param name="partitionId">The partition identifier.</param>
        /// <returns></returns>
        public async Task<String> GetPartitionStateFromProjection(String projectionName,
                                                                  String partitionId)
        {
            ProjectionsManager projectionsManager = this.CreateProjectionsManager();

            return await projectionsManager.GetPartitionStateAsync(projectionName, partitionId, this.UserCredentials);
        }

        /// <summary>
        /// Gets the result from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <returns></returns>
        public async Task<String> GetResultFromProjection(String projectionName)
        {
            ProjectionsManager projectionsManager = this.CreateProjectionsManager();

            return await projectionsManager.GetResultAsync(projectionName, this.UserCredentials);
        }

        /// <summary>
        /// Gets the state from projection.
        /// </summary>
        /// <param name="projectionName">Name of the projection.</param>
        /// <returns></returns>
        public async Task<String> GetStateFromProjection(String projectionName)
        {
            ProjectionsManager projectionsManager = this.CreateProjectionsManager();

            return await projectionsManager.GetStateAsync(projectionName, this.UserCredentials);
        }

        /// <summary>
        /// Inserts the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="expectedVersion">The expected version.</param>
        /// <param name="aggregateEvents">The aggregate events.</param>
        /// <returns></returns>
        public async Task InsertEvents(String streamName,
                                       Int32 expectedVersion,
                                       List<DomainEvent> aggregateEvents)
        {
            List<EventData> eventData = new List<EventData>();
            JsonSerializerSettings s = new JsonSerializerSettings
                                       {
                                           TypeNameHandling = TypeNameHandling.All
                                       };

            IEventStoreConnection connection = await this.GetEventStoreConnection();

            aggregateEvents.ForEach(domainEvent => eventData.Add(new EventData(domainEvent.EventId,
                                                                               domainEvent.GetType().FullName,
                                                                               true,
                                                                               Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(domainEvent, Formatting.None, s)),
                                                                               null)));

            await connection.AppendToStreamAsync(streamName, expectedVersion, eventData, this.UserCredentials);
        }

        /// <summary>
        /// Reads the events.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="fromVersion">From version.</param>
        /// <returns></returns>
        public async Task<List<DomainEvent>> ReadEvents(String streamName,
                                                        Int64 fromVersion)
        {
            StreamEventsSlice response;
            List<DomainEvent> domainEvents = new List<DomainEvent>();

            IEventStoreConnection connection = await this.GetEventStoreConnection();

            do
            {
                // TODO: Max events might be configurable.
                response = await connection.ReadStreamEventsForwardAsync(streamName, fromVersion, 10, true, this.UserCredentials);

                if (response.NextEventNumber > 0)
                {
                    fromVersion = response.NextEventNumber;
                }

                //TODO: Factory to convert our native DomainEvents
                foreach (ResolvedEvent @event in response.Events)
                {
                    String serialisedData = Encoding.UTF8.GetString(@event.Event.Data);

                    JsonSerializerSettings s = new JsonSerializerSettings
                                               {
                                                   TypeNameHandling = TypeNameHandling.All
                                               };
                    DomainEvent deserialized = JsonConvert.DeserializeObject<DomainEvent>(serialisedData, s);

                    //TODO: look at refactoring this conversion
                    domainEvents.Add(deserialized);
                }
            } while (!response.IsEndOfStream);

            return domainEvents;
        }

        /// <summary>
        /// Subscribes to stream from.
        /// </summary>
        /// <param name="catchUpSubscriptionId">The catch up subscription identifier.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="lastCheckpoint">The last checkpoint.</param>
        /// <param name="endPointId">The end point identifier.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task SubscribeToStreamFrom(Guid catchUpSubscriptionId,
                                                String stream,
                                                Int32? lastCheckpoint,
                                                Guid endPointId)
        {
            // TODO: Guards

            // Setup the Event Appeared delegate
            Action<EventStoreCatchUpSubscription, ResolvedEvent> eventAppeared = (subscription,
                                                                                  resolvedEvent) =>
                                                                                     this.CatchUpSubscriptionEventAppeared(subscription,
                                                                                                                           resolvedEvent,
                                                                                                                           catchUpSubscriptionId,
                                                                                                                           endPointId);

            // Setup the Subscription Droppped delegate
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> subscriptionDropped = (subscription,
                                                                                                            reason,
                                                                                                            ex) => this.CatchUpSubscriptionDropped(subscription,
                                                                                                                                                   reason,
                                                                                                                                                   ex,
                                                                                                                                                   catchUpSubscriptionId);

            // Setup the Live Processing Started delegate
            Action<EventStoreCatchUpSubscription> liveProcessingStarted =
                subscription => this.CatchUpSubscriptionLiveProcessingStarted(catchUpSubscriptionId, subscription);

            // Create the settings object
            CatchUpSubscriptionSettings settings = CatchUpSubscriptionSettings.Default;

            // Get the connection
            IEventStoreConnection connection = await this.GetEventStoreConnection();

            // Now Subscribe to the Stream
            this.Connection.SubscribeToStreamFrom(stream, lastCheckpoint, settings, eventAppeared, liveProcessingStarted, subscriptionDropped, this.UserCredentials);
        }

        /// <summary>
        /// Catches up subscription dropped.
        /// </summary>
        /// <param name="eventStoreCatchUpSubscription">The event store catch up subscription.</param>
        /// <param name="subscriptionDropReason">The subscription drop reason.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="subscriptionGroupId">The subscription group identifier.</param>
        private void CatchUpSubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription,
                                                SubscriptionDropReason subscriptionDropReason,
                                                Exception exception,
                                                Guid subscriptionGroupId)
        {
            if (this.SubscriptionDropped != null)
            {
                this.SubscriptionDropped(eventStoreCatchUpSubscription.StreamId,
                                         string.Empty,
                                         SubscriptionType.CatchUp,
                                         subscriptionDropReason,
                                         exception,
                                         subscriptionGroupId);
            }
            else
            {
                this.LogInformation("Unable to process event as SubscriptionDropped Event handler is null");
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

        private void LogDebug(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Debug);
            }
        }

        private void LogWarning(String trace)
        {
            if (this.TraceGenerated != null)
            {
                this.TraceGenerated(trace, LogLevel.Warning);
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

        /// <summary>
        /// Catches up subscription event appeared.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <param name="endpointId">The endpoint identifier.</param>
        /// <returns></returns>
        private async Task CatchUpSubscriptionEventAppeared(EventStoreCatchUpSubscription subscription,
                                                            ResolvedEvent resolvedEvent,
                                                            Guid catchUpSubscriptionId,
                                                            Guid endpointId)
        {
            try
            {
                // Check the event data has been received
                if (this.EventAppeared != null)
                {
                    // Serialise the event data
                    String serialisedData = JsonConvert.SerializeObject(resolvedEvent);

                    SubscriptionDataTransferObject subscriptionInformation = new SubscriptionDataTransferObject
                                                                             {
                                                                                 SerialisedData = serialisedData,
                                                                                 EventId = resolvedEvent.Event.EventId,
                                                                                 SubscriptionGroupId = catchUpSubscriptionId.ToString()
                                                                             };

                    Boolean handledSuccessfully = await this.EventAppeared(subscriptionInformation);
                    if (!handledSuccessfully)
                    {
                        throw new Exception($"Failed to Process Event {resolvedEvent.Event.EventId} on catchup subscription group {catchUpSubscriptionId}");
                    }
                }
                else
                {
                    this.LogInformation("Unable to process event as EventAppeared Event handler is null");
                }
            }
            catch(Exception ex)
            {
                this.LogError(ex);
            }
        }

        /// <summary>
        /// Catches up subscription live processing started.
        /// </summary>
        /// <param name="catchUpSubscriptionId">The catch up subscription identifier.</param>
        /// <param name="eventStoreCatchUpSubscription">The event store catch up subscription.</param>
        private void CatchUpSubscriptionLiveProcessingStarted(Guid catchUpSubscriptionId,
                                                              EventStoreCatchUpSubscription eventStoreCatchUpSubscription)
        {
            if (this.LiveProcessStarted != null)
            {
                this.LiveProcessStarted(catchUpSubscriptionId);
            }
            else
            {
                this.LogInformation("Unable to process event as LiveProcessStarted Event handler is null");
            }
        }

        /// <summary>
        /// Handles the AuthenticationFailed event of the Event Store Connection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientErrorEventArgs"/> instance containing the event data.</param>
        private void Connection_AuthenticationFailed(Object sender,
                                                     ClientAuthenticationFailedEventArgs e)
        {
            this.LogInformation($"Error on connection [{e.Connection.ConnectionName}] to Event Store, Authenticaion Failed");
        }

        /// <summary>
        /// Handles the Closed event of the Event Store Connection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientClosedEventArgs"/> instance containing the event data.</param>
        private void Connection_Closed(Object sender,
                                       ClientClosedEventArgs e)
        {
            this.LogInformation($"Connection [{e.Connection.ConnectionName}] to Event Store is closed. Closure reason [{e.Reason}]");

            // Set the connection to null, this will force the next Get Connection to create a new connection
            this.Connection = null;
        }

        /// <summary>
        /// Handles the Connected event of the Connection control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientConnectionEventArgs"/> instance containing the event data.</param>
        private void Connection_Connected(Object sender,
                                          ClientConnectionEventArgs e)
        {
            this.LogInformation($"Connection [{e.Connection.ConnectionName}] to Event Store [{e.RemoteEndPoint.Address}:{e.RemoteEndPoint.Port}] is connectected");
        }

        /// <summary>
        /// Handles the Disconnected event of the Event Store Connection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientConnectionEventArgs"/> instance containing the event data.</param>
        private void Connection_Disconnected(Object sender,
                                             ClientConnectionEventArgs e)
        {
            this.LogInformation($"Connection [{e.Connection.ConnectionName}] to Event Store [{e.RemoteEndPoint.Address}:{e.RemoteEndPoint.Port}] is disconnected");
        }

        /// <summary>
        /// Handles the ErrorOccurred event of the Event Store Connection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientErrorEventArgs"/> instance containing the event data.</param>
        private void Connection_ErrorOccurred(Object sender,
                                              ClientErrorEventArgs e)
        {
            this.LogInformation($"Error on connection [{e.Connection.ConnectionName}] to Event Store");
        }

        /// <summary>
        /// Handles the Reconnecting event of the Event Store Connection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ClientReconnectingEventArgs"/> instance containing the event data.</param>
        private void Connection_Reconnecting(Object sender,
                                             ClientReconnectingEventArgs e)
        {
            this.LogInformation($"Connection [{e.Connection.ConnectionName}] to Event Store reconnecting");
        }

        /// <summary>
        /// Creates the persistent connection.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="persistentSubscriptionSettings">The persistent subscription settings.</param>
        /// <returns></returns>
        private async Task CreatePersistentSubscription(String streamName,
                                                        String groupName,
                                                        PersistentSubscriptionSettings persistentSubscriptionSettings)
        {
            this.GuardAgainstNullOrEmptyStreamName(streamName);
            this.GuardAgainstNullOrEmptyGroupName(groupName);
            this.GuardAgainstNullPersistentSubscriptionSettings(persistentSubscriptionSettings);

            // Get the connection
            IEventStoreConnection connection = await this.GetEventStoreConnection();

            // Attempt to create the subscription
            await connection.CreatePersistentSubscriptionAsync(streamName, groupName, persistentSubscriptionSettings, this.UserCredentials);
        }

        private void GuardAgainstNullPersistentSubscriptionSettings(PersistentSubscriptionSettings persistentSubscriptionSettings)
        {
            if (persistentSubscriptionSettings == null)
            {
                throw new ArgumentNullException(nameof(persistentSubscriptionSettings), "Persistent Subscription Settings cannot be null when creating a Persistent Subscription");
            }
        }

        /// <summary>
        /// Creates the projections manager.
        /// </summary>
        /// <returns></returns>
        private ProjectionsManager CreateProjectionsManager()
        {
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(this.EventStoreConnectionSettings.IpAddress), this.EventStoreConnectionSettings.HttpPort);

            return new ProjectionsManager(new ConsoleLogger(), endpoint, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Gets the event store connection.
        /// </summary>
        /// <returns></returns>
        private async Task<IEventStoreConnection> GetEventStoreConnection()
        {
            if (this.Connection == null)
            {
                lock(this.CreateLockObject)
                {
                    if (this.Connection == null)
                    {
                        this.Connection = this.ConnectionResolver(this.EventStoreConnectionSettings);
                        this.Connection.Connected += this.Connection_Connected;
                        this.Connection.Reconnecting += this.Connection_Reconnecting;
                        this.Connection.Closed += this.Connection_Closed;
                        this.Connection.ErrorOccurred += this.Connection_ErrorOccurred;
                        this.Connection.Disconnected += this.Connection_Disconnected;
                        this.Connection.AuthenticationFailed += this.Connection_AuthenticationFailed;
                        this.Connection.ConnectAsync().Wait();
                    }
                }
            }

            return this.Connection;
        }

        /// <summary>
        /// Persistents the subscription dropped.
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="subscriptionDropReason">The subscription drop reason.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="subscriptionGroupId">The subscription group identifier.</param>
        private void PersistentSubscriptionDropped(String streamName,
                                                   String groupName,
                                                   SubscriptionDropReason subscriptionDropReason,
                                                   Exception exception,
                                                   Guid subscriptionGroupId)
        {
            if (this.SubscriptionDropped != null)
            {
                this.SubscriptionDropped(streamName, groupName, SubscriptionType.Persistent, subscriptionDropReason, exception, subscriptionGroupId);
            }
            else
            {
                this.LogInformation("Unable to process Subscription Dropping message as SubscriptionDropped Event handler is null");
            }
        }

        /// <summary>
        /// Persistents the subscription event appeared.
        /// </summary>
        /// <param name="subscription">The subscription.</param>
        /// <param name="resolvedEvent">The resolved event.</param>
        /// <param name="subscriptionGroupId">The subscription group identifier.</param>
        /// <returns></returns>
        private async Task PersistentSubscriptionEventAppeared(EventStorePersistentSubscriptionBase subscription,
                                                               ResolvedEvent resolvedEvent,
                                                               Guid subscriptionGroupId)
        {
            try
            {
                // Check the event data has been received
                if (this.EventAppeared != null)
                {
                    // Get the event data from the resolved Event
                    String serialisedData = Encoding.UTF8.GetString(resolvedEvent.Event.Data);

                    SubscriptionDataTransferObject subscriptionInformation = new SubscriptionDataTransferObject
                                                                             {
                                                                                 SerialisedData = serialisedData,
                                                                                 EventId = resolvedEvent.Event.EventId,
                                                                                 SubscriptionGroupId = subscriptionGroupId.ToString()
                                                                             };

                    Boolean handledSuccessfully = await this.EventAppeared(subscriptionInformation);
                    if (!handledSuccessfully)
                    {
                        throw new Exception($"Failed to Process Event {resolvedEvent.Event.EventId} on persistent subscription group {subscriptionGroupId}");
                    }
                }
                else
                {
                    this.LogInformation("Unable to process event as EventAppeared Event handler is null");
                }

                // Acknowledge the event
                subscription.Acknowledge(resolvedEvent);
            }
            catch(Exception ex)
            {
                subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Retry, ex.Message);
            }
        }

        #endregion
    }
}