using SimpleResults;

namespace Shared.EventStore.Tests.TestObjects;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;
using EventHandling;

public class TestDomainEventHandler : IDomainEventHandler
{
    public List<IDomainEvent> DomainEvents = new();

    public async Task<Result> Handle(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        DomainEvents.Add(domainEvent);
        return Result.Success();
    }
}