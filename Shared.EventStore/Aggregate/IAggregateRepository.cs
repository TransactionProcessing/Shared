namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;

    public interface IAggregateRepository<TAggregate, TDomainEvent> where TAggregate : Aggregate
                                                                    where TDomainEvent : IDomainEvent
    {
        #region Methods

        /// <summary>
        /// Gets the latest version.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task<TAggregate> GetLatestVersion(Guid aggregateId,
                                          CancellationToken cancellationToken);

        /// <summary>
        /// Saves the changes.
        /// </summary>
        /// <param name="aggregate">The aggregate.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task SaveChanges(TAggregate aggregate,
                         CancellationToken cancellationToken);

        #endregion
    }
}