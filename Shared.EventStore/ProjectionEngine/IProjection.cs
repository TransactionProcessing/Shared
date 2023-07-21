namespace Shared.EventStore.ProjectionEngine;

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;

public interface IProjection<TState> where TState : State{
    #region Methods

    [Pure]
    Task<TState> Handle(TState state, IDomainEvent domainEvent, CancellationToken cancellationToken);

    Boolean ShouldIHandleEvent(IDomainEvent domainEvent);

    #endregion
}