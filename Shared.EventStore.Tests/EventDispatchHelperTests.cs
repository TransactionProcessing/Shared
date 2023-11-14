namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;
using EventHandling;
using Moq;
using Shared.EventStore.Tests.TestObjects;
using Shouldly;
using SubscriptionWorker;
using Xunit;

public class EventDispatchHelperTests{
    [Fact]
    public async Task EventDispatchHelper_DispatchToHandlers_AllSuccessful(){
        AggregateNameSetEvent @event = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        List<IDomainEventHandler> handlers = new List<IDomainEventHandler>();
        handlers.Add(new TestDomainEventHandler());
        Should.NotThrow(async () => {
                            await @event.DispatchToHandlers(handlers, CancellationToken.None);
                        });
    }

    [Fact]
    public async Task EventDispatchHelper_DispatchToHandlers_HandlerThrowsException_ErrorThrown()
    {
        AggregateNameSetEvent @event = new AggregateNameSetEvent(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        List<IDomainEventHandler> handlers = new List<IDomainEventHandler>();
        Mock<IDomainEventHandler> domainEventHandler = new Mock<IDomainEventHandler>();
        domainEventHandler.Setup(s => s.Handle(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>())).Throws<Exception>();
        handlers.Add(domainEventHandler.Object);
        Should.Throw<Exception>(async () => {
                                    await @event.DispatchToHandlers(handlers, CancellationToken.None);
                                });
    }
}