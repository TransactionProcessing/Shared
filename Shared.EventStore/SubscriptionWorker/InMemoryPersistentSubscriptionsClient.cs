using KurrentDB.Client;

namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using global::EventStore.Client;

[ExcludeFromCodeCoverage]
public class InMemoryPersistentSubscriptionsClient : IPersistentSubscriptionsClient
{
    private Func<KurrentDB.Client.PersistentSubscription, ResolvedEvent, Int32?, CancellationToken, Task> EventAppeared;

    private String Stream;
    private String Group;

    #region Methods

    public async Task<KurrentDB.Client.PersistentSubscription> SubscribeAsync(String stream,
                                                                                       String group,
                                                                                       Func<KurrentDB.Client.PersistentSubscription, ResolvedEvent, Int32?, CancellationToken, Task> eventAppeared,
                                                                                       Action<KurrentDB.Client.PersistentSubscription, SubscriptionDroppedReason, Exception?>? subscriptionDropped,
                                                                                       UserCredentials? userCredentials,
                                                                                       Int32 bufferSize,
                                                                                       CancellationToken cancellationToken)
    {
        this.Stream = stream;
        this.Group = group;
        this.EventAppeared = eventAppeared;

        return await Task.Factory.StartNew(() => default(KurrentDB.Client.PersistentSubscription),
            cancellationToken);
    }


    public void WriteEvent(String @event, String @type, CancellationToken cancellationToken)
    {

        ReadOnlyMemory<byte> data = new(Encoding.Default.GetBytes(@event));
        IDictionary<string, string> metadata = new Dictionary<String, String>();
        ReadOnlyMemory<byte> custommetadata = new(Encoding.Default.GetBytes(@event));

        metadata.Add("type", @type);
        metadata.Add("created", "1000");
        metadata.Add("content-type", "application/json");

        EventRecord er = new(this.Stream, Uuid.NewUuid(), StreamPosition.Start, Position.Start, metadata, data, custommetadata);
        ResolvedEvent re = new(er, null, null);

        this.EventAppeared(default, re, 0, cancellationToken);
    }

    #endregion
}