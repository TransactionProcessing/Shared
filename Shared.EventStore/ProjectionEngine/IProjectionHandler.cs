namespace Shared.EventStore.ProjectionEngine;

using System;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;

public interface IProjectionHandler{
    #region Events

    event EventHandler<String> TraceGenerated;

    #endregion

    #region Methods

    Task Handle(IDomainEvent @event, CancellationToken cancellationToken);

    #endregion
}