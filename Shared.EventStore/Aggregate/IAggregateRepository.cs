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

        Task<TAggregate> GetLatestVersion(Guid aggregateId,
                                                  CancellationToken cancellationToken);

        Task SaveChanges(TAggregate aggregate,
                         CancellationToken cancellationToken);

        Task<TAggregate> GetLatestVersionFromLastEvent(Guid aggregateId, CancellationToken cancellationToken);

        #endregion
    }
}