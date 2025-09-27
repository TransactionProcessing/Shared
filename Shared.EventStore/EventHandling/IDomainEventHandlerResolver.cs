namespace Shared.EventStore.EventHandling;

using DomainDrivenDesign.EventSourcing;
using SimpleResults;
using System.Collections.Generic;

public interface IDomainEventHandlerResolver
{
    Result<List<IDomainEventHandler>> GetDomainEventHandlers(IDomainEvent domainEvent);
}