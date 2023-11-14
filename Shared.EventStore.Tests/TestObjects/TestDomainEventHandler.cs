namespace Shared.EventStore.Tests.TestObjects;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;
using EventHandling;

public class TestDomainEventHandler : IDomainEventHandler
{
    public List<IDomainEvent> DomainEvents = new();

    public async Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        DomainEvents.Add(domainEvent);
    }
}