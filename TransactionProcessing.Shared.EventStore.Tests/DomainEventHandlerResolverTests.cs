using System;
using System.Collections.Generic;
using System.Linq;
using TransactionProcessing.Shared.EventStore.EventHandling;
using TransactionProcessing.Shared.EventStore.Tests.TestObjects;
using TransactionProcessing.UnionResults;

namespace TransactionProcessing.Shared.EventStore.Tests;

using Shouldly;
using Xunit;

public class DomainEventHandlerResolverTests
{
    [Fact]
    public void DomainEventHandlerResolver_GetDomainEventHandlers_HandlerResolved(){
        Dictionary<String, String[]> eventHandlerConfiguration= new();

        IDomainEventHandler CreateEventHandlerFunc(Type t) {
            if (t.Name == nameof(TestDomainEventHandler)) return new TestDomainEventHandler();

            if (t.Name == nameof(TestDomainEventHandler2)) return new TestDomainEventHandler2();
            return null;
        }

        eventHandlerConfiguration.Add("TransactionProcessing.Shared.EventStore.Tests.TestObjects.TestDomainEventHandler, TransactionProcessing.Shared.EventStore.Tests", new String[]{"EstateCreatedEvent"});
        eventHandlerConfiguration.Add("TransactionProcessing.Shared.EventStore.Tests.TestObjects.TestDomainEventHandler2, TransactionProcessing.Shared.EventStore.Tests", new String[] { "EstateCreatedEvent" });

        DomainEventHandlerResolver r = new(eventHandlerConfiguration, CreateEventHandlerFunc);
        Result<List<IDomainEventHandler>> result  = r.GetDomainEventHandlers(new EstateCreatedEvent(TestData.AggregateId, TestData.EstateName));
        result.IsSuccess.ShouldBeTrue();
        result.Data.Count.ShouldBe(2);
        result.Data.Count(x => x.GetType() == typeof(TestDomainEventHandler)).ShouldBe(1);
        result.Data.Count(x => x.GetType() == typeof(TestDomainEventHandler2)).ShouldBe(1);
    }

    [Fact]
    public void DomainEventHandlerResolver_EventNotConfigured_NullReturned()
    {
        Dictionary<String, String[]> eventHandlerConfiguration = new();
        Func<Type, IDomainEventHandler> createEventHandlerFunc = t => new TestDomainEventHandler();

        DomainEventHandlerResolver r = new(eventHandlerConfiguration, createEventHandlerFunc);
        Result<List<IDomainEventHandler>> result = r.GetDomainEventHandlers(new EstateCreatedEvent(TestData.AggregateId, TestData.EstateName));
        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public void DomainEventHandlerResolver_GetDomainEventHandlers_NoHandlerFound_ErrorThrown()
    {
        Dictionary<String, String[]> eventHandlerConfiguration = new Dictionary<String, String[]>();
        Func<Type, IDomainEventHandler> createEventHandlerFunc = t => new TestDomainEventHandler();

        List<String> handlers = new List<String>();
        handlers.Add("TransactionProcessing.Shared.EventStore.Tests.TestDomainEventHandler1, TransactionProcessing.Shared.EventStore.Tests");
        eventHandlerConfiguration.Add("EstateCreatedEvent", handlers.ToArray());

        Should.Throw<NotSupportedException>(() => new DomainEventHandlerResolver(eventHandlerConfiguration, createEventHandlerFunc));
    }
}