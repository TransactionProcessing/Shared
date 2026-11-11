namespace TransactionProcessing.Shared.EventStore.EventHandling;

using DomainDrivenDesign.EventSourcing;
using TransactionProcessing.UnionResults;
using System.Collections.Generic;

public interface IDomainEventHandlerResolver
{
    Result<List<IDomainEventHandler>> GetDomainEventHandlers(IDomainEvent domainEvent);
}