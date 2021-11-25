namespace Shared.EventStore.SubscriptionWorker
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface ISubscriptionRepository
    {
        Task<PersistentSubscriptions> GetSubscriptions(Boolean forceRefresh, CancellationToken cancellationToken);

        Task PreWarm(CancellationToken cancellationToken);
    }
}