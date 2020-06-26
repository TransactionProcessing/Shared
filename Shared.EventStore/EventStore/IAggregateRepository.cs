namespace Shared.EventStore.EventStore
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IAggregateRepository<T> where T : Aggregate, new()
    {
        /// <summary>
        /// Gets the latest version.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<T> GetLatestVersion(Guid aggregateId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the name of the by.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<T> GetByName(Guid aggregateId, String streamName, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SaveChanges(T aggregate, CancellationToken cancellationToken);
    }
}