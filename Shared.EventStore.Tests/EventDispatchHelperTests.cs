using Shared.EventStore.Tests.TestObjects;
using SimpleResults;

namespace Shared.EventStore.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;
using EventHandling;
using Imposter.Abstractions;
using Shouldly;
using SubscriptionWorker;
using Xunit;

public class EventDispatchHelperTests{
    [Fact]
    public async Task EventDispatchHelper_DispatchToHandlers_AllSuccessful(){
        AggregateNameSetEvent @event = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        List<IDomainEventHandler> handlers = new();
        handlers.Add(new TestDomainEventHandler());
        Result result = await @event.DispatchToHandlers(handlers, CancellationToken.None);
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task EventDispatchHelper_DispatchToHandlers_HandlerThrowsException_ErrorThrown()
    {
        AggregateNameSetEvent @event = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        List<IDomainEventHandler> handlers = new();
        IDomainEventHandlerImposter domainEventHandler = new();
        domainEventHandler.Handle(Arg<IDomainEvent>.Any(), Arg<CancellationToken>.Any())
            .ReturnsAsync(Result.Failure());
        IDomainEventHandlerImposter domainEventHandler2 = new();
        domainEventHandler2.Handle(Arg<IDomainEvent>.Any(), Arg<CancellationToken>.Any())
            .ReturnsAsync(Result.Failure());
        handlers.Add(domainEventHandler.Instance());
        handlers.Add(domainEventHandler2.Instance());
        Result dispatchResult = await @event.DispatchToHandlers(handlers, CancellationToken.None);
        dispatchResult.IsFailed.ShouldBeTrue();
        
    }

    [Fact]
    public async Task EventDispatchHelper_DispatchToHandlers_HandlersFail_ErrorThrown()
    {
        AggregateNameSetEvent @event = new(TestData.AggregateId, TestData.EventId, TestData.EstateName);
        List<IDomainEventHandler> handlers = new();
        IDomainEventHandlerImposter domainEventHandler = new();
        domainEventHandler.Handle(Arg<IDomainEvent>.Any(), Arg<CancellationToken>.Any())
            .ReturnsAsync(Result.Failure("Error Message 1"));
        IDomainEventHandlerImposter domainEventHandler2 = new();
        domainEventHandler2.Handle(Arg<IDomainEvent>.Any(), Arg<CancellationToken>.Any())
            .ReturnsAsync(Result.Failure(new List<String>() {
                "Error Message 2",
                "Error Message 3"
            }));
        handlers.Add(domainEventHandler.Instance());
        handlers.Add(domainEventHandler2.Instance());
        Result dispatchResult = await @event.DispatchToHandlers(handlers, CancellationToken.None);
        dispatchResult.IsFailed.ShouldBeTrue();
        dispatchResult.Message.ShouldBe($"One or more event handlers have failed. Error Messages [Error Message 1{Environment.NewLine}Error Message 2{Environment.NewLine}Error Message 3]");

    }
}
