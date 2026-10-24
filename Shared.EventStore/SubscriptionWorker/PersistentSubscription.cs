using KurrentDB.Client;
using Shared.Serialisation;
using SimpleResults;

namespace Shared.EventStore.SubscriptionWorker;

using Aggregate;
using DomainDrivenDesign.EventSourcing;
using EventHandling;
using Shared.General;
using Shared.Logger.TennantContext;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shared.EventStore;

[ExcludeFromCodeCoverage]
public class PersistentSubscription
{
    public EventHandler<String> SubscriptionHasDropped;

    public EventHandler<String> EventHandlerIsMissing;

    private readonly Func<CancellationToken, Task<KurrentDB.Client.PersistentSubscription>> Subscribe;

    private void SubscriptionDropped(KurrentDB.Client.PersistentSubscription arg1,
                                     SubscriptionDroppedReason arg2,
                                     Exception? arg3) =>
        this.SubscriptionDropped(arg2.ToString());

    public void SubscriptionDropped(String reason)
    {
        this.Connected = false;
        Logger.Logger.LogWarning("Subscription dropped - {reason}", reason);

        if (this.SubscriptionHasDropped != null) {
            //Broadcast to owner
            this.SubscriptionHasDropped(this, reason);
        }
    }

    public void EventHandlerMissing(String reason)
    {
        this.Connected = false;
        Logger.Logger.LogWarning("Event handler missing - {reason}", reason);

        if (this.EventHandlerIsMissing != null)
        {
            //Broadcast to owner
            this.EventHandlerIsMissing(this, reason);
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

        Func< KurrentDB.Client.PersistentSubscription, ResolvedEvent, Int32?, CancellationToken, Task> eventAppeared = (ps, re, retryCount, ct) => this.EventAppeared(ps, re, retryCount, domainEventHandlerResolver, ct);

        this.Subscribe = ct => persistentSubscriptionsClient.SubscribeAsync(this.PersistentSubscriptionDetails.StreamName,
            this.PersistentSubscriptionDetails.GroupName,
            eventAppeared,
            this.SubscriptionDropped,
            userCredentials,
            persistentSubscriptionDetails.InflightMessages,
            ct);
    }

    public Boolean Connected { get; private set; }
    public KurrentDB.Client.PersistentSubscription KurrentDbPersistentSubscription { get; private set; }
    public IPersistentSubscriptionsClient PersistentSubscriptionsClient { get; }
    public PersistentSubscriptionDetails PersistentSubscriptionDetails { get; }
    
    public async Task ConnectToSubscription(CancellationToken cancellationToken)
    {
        this.KurrentDbPersistentSubscription = await EventStoreGrpcRetryPolicy.ExecuteAsync(
            () => this.Subscribe(cancellationToken),
            nameof(this.ConnectToSubscription),
            this.ToString(),
            (level, message) =>
            {
                if (level == LogLevel.Warning) {
                    Logger.Logger.LogWarning(message);
                }
                else {
                    Logger.Logger.LogError(message);
                }
            });

        this.Connected = true;
    }

    public static PersistentSubscription Create(IPersistentSubscriptionsClient persistentSubscriptionsClient,
                                                PersistentSubscriptionDetails persistentSubscriptionDetails,
                                                IDomainEventHandlerResolver domainEventHandlerResolver,
                                                String username = "admin",
                                                String password = "changeit") => new(persistentSubscriptionsClient, persistentSubscriptionDetails, domainEventHandlerResolver, username, password);


    public override String ToString() => $"{this.PersistentSubscriptionDetails.StreamName}-{this.PersistentSubscriptionDetails.GroupName}";

    private static (String messageTemplate, Object[]? args) BuildFailureMessage(ResolvedEvent resolvedEvent,
                                             Result result)
    {
        String errors = result.Errors?.Any() == true
            ? String.Join(Environment.NewLine, result.Errors.Where(error => !String.IsNullOrWhiteSpace(error)))
            : String.Empty;

        String resultDetails = String.IsNullOrWhiteSpace(result.Message)
            ? $"Result status [{result.Status}]. Errors [{errors}]"
            : $"Result was {result.Message}";

        Object[] objectArray = new[] { resolvedEvent.Event.EventType, resolvedEvent.GetResolvedEventDataAsString(), resultDetails };

        return ("Failed to process the event type {resolvedEventType} {resolvedEventString()} {resultDetails}", objectArray);
    }

    private static TenantIdentifiers GetTenantIdentifiersFromDomainEvent(IDomainEvent domainEvent)
    {
        String domainEventAsString = StringSerialiser.Serialise(domainEvent);

        try
        {
            Guid estateId = StringSerialiser.GetValue<Guid>(domainEventAsString, "estateId");

            return estateId == Guid.Empty ? TenantIdentifiers.Default() : new TenantIdentifiers(estateId, Guid.Empty);
        }
        catch (Exception)
        {
            Logger.Logger.LogWarning("Unable to get estateId from request body [{domainEventAsString}]", domainEventAsString);
            return TenantIdentifiers.Default();
        }
    }

    private static CorrelationId GetCorrelationIdFromEventMetadata(ResolvedEvent resolvedEvent)
    {
        try
        {
            using JsonDocument metadata = JsonDocument.Parse(resolvedEvent.Event.Metadata);
            if (metadata.RootElement.ValueKind == JsonValueKind.Object &&
                metadata.RootElement.TryGetProperty(TenantContext.KeyNameCorrelationId, out JsonElement correlationIdElement) &&
                correlationIdElement.ValueKind == JsonValueKind.String &&
                Guid.TryParse(correlationIdElement.GetString(), out Guid correlationId) &&
                correlationId != Guid.Empty)
            {
                return CorrelationId.From(correlationId);
            }
        }
        catch (JsonException)
        {
            // Older events or events with unrelated metadata can still be processed.
        }

        return null;
    }

    internal async Task EventAppeared(KurrentDB.Client.PersistentSubscription persistentSubscription,
                                             ResolvedEvent resolvedEvent,
                                             Int32? retryCount,
                                             IDomainEventHandlerResolver domainEventHandlerResolver,
                                             CancellationToken cancellationToken)
    {
        try {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (resolvedEvent.SilentlyHandleEvent()) {
                await PersistentSubscriptionsHelper.AckEvent(persistentSubscription, resolvedEvent);
                return;
            }

            IDomainEvent domainEvent = TypeMapConvertor.Convertor(resolvedEvent);
            TenantIdentifiers tenantIdentifiers = PersistentSubscription.GetTenantIdentifiersFromDomainEvent(domainEvent);
            Boolean.TryParse(ConfigurationReader.GetValueOrDefault("AppSettings", "LogsPerTenantEnabled", "false"), out Boolean logPerTenantEnabled);

            TenantContext tenantContext = new();
            tenantContext.Initialise(tenantIdentifiers, logPerTenantEnabled);
            CorrelationId correlationId = PersistentSubscription.GetCorrelationIdFromEventMetadata(resolvedEvent);
            if (correlationId != null)
            {
                tenantContext.SetCorrelationId(correlationId);
            }
            TenantContext.CurrentTenant = tenantContext;

            Logger.Logger.LogInformation("EventAppearedFromPersistentSubscription with Event Id {resolvedEventId} event type {resolvedEventType}", resolvedEvent.Event.EventId, resolvedEvent.Event.EventType);

            Result<List<IDomainEventHandler>> domainEventHandlersResult = domainEventHandlerResolver.GetDomainEventHandlers(domainEvent);

            if (domainEventHandlersResult.IsFailed) {
                // Log a line of trace out 
                this.EventHandlerMissing($"No event handlers configured for Event Type [{domainEvent.GetType().Name}]");

                await PersistentSubscriptionsHelper.AckEvent(persistentSubscription, resolvedEvent);
                return;
            }

            List<IDomainEventHandler> domainEventHandlers = domainEventHandlersResult.Data;

            Result result = await domainEvent.DispatchToHandlers(domainEventHandlers, cts.Token);
            if (result.IsSuccess) {
                await PersistentSubscriptionsHelper.AckEvent(persistentSubscription, resolvedEvent);
            }
            else {
                (String messageTemplate, Object[] args) failureDetails = BuildFailureMessage(resolvedEvent, result);
                Logger.Logger.LogError(failureDetails.messageTemplate, failureDetails.args);
            }
        }
        catch (Exception e) {
            Logger.Logger.LogError(e,"Failed to process the event type {eventType} {resolvedEvent}", resolvedEvent.Event.EventType, resolvedEvent.GetResolvedEventDataAsString());
        }
    }
}
