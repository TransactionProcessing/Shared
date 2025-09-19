namespace Shared.EventStore.Aggregate;

using System;
using DomainDrivenDesign.EventSourcing;

public interface IAggregateRepositoryManager
{
    #region Methods

    /// <summary>
    /// Gets the aggregate repository.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="identifier">The identifier.</param>
    /// <returns></returns>
    IAggregateRepository<TAggregate, TDomainEvent> GetAggregateRepository<TAggregate, TDomainEvent>(Guid identifier) where TAggregate : Aggregate, new()
        where TDomainEvent : IDomainEvent;


    #endregion
}