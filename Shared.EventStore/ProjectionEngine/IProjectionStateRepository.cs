namespace Shared.EventStore.ProjectionEngine;

using System;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;

public interface IProjectionStateRepository<TState> where TState : State{
    #region Methods

    Task<TState> Load(IDomainEvent @event, CancellationToken cancellationToken);

    Task<TState> Load(Guid estateId, Guid stateId, CancellationToken cancellationToken);

    Task<TState> Save(TState state, IDomainEvent @event, CancellationToken cancellationToken);

    #endregion
}