namespace Shared.EventStore.ProjectionEngine;

using System;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;

public interface IProjectionHandler{

    event EventHandler<String> TraceGenerated;



    Task Handle(IDomainEvent @event, CancellationToken cancellationToken);

}