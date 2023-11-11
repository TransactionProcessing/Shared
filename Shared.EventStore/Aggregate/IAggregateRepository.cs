namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using CSharpFunctionalExtensions;
    using DomainDrivenDesign.EventSourcing;

    public interface IAggregateRepository<TAggregate, TDomainEvent> where TAggregate : Aggregate
                                                                    where TDomainEvent : IDomainEvent
    {
        #region Methods

        Task<Result<TAggregate>> GetLatestVersion(Guid aggregateId,
                                                  CancellationToken cancellationToken);

        Task<Result> SaveChanges(TAggregate aggregate,
                         CancellationToken cancellationToken);

        Task<Result<TAggregate>> GetLatestVersionFromLastEvent(Guid aggregateId, CancellationToken cancellationToken);

        #endregion
    }
}