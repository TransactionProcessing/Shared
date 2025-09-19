using System;
using System.Collections.Generic;
using System.Linq;
using Shared.EventStore.EventHandling;
using Shared.EventStore.Tests.TestObjects;

namespace Shared.EventStore.Tests
{
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

            eventHandlerConfiguration.Add("Shared.EventStore.Tests.TestObjects.TestDomainEventHandler, Shared.EventStore.Tests", new String[]{"EstateCreatedEvent"});
            eventHandlerConfiguration.Add("Shared.EventStore.Tests.TestObjects.TestDomainEventHandler2, Shared.EventStore.Tests", new String[] { "EstateCreatedEvent" });

            DomainEventHandlerResolver r = new(eventHandlerConfiguration, CreateEventHandlerFunc);
            List<IDomainEventHandler> result  = r.GetDomainEventHandlers(new EstateCreatedEvent(TestData.AggregateId, TestData.EstateName));
            result.Count.ShouldBe(2);
            result.Count(x => x.GetType() == typeof(TestDomainEventHandler)).ShouldBe(1);
            result.Count(x => x.GetType() == typeof(TestDomainEventHandler2)).ShouldBe(1);
        }

        [Fact]
        public void DomainEventHandlerResolver_EventNotConfigured_NullReturned()
        {
            Dictionary<String, String[]> eventHandlerConfiguration = new();
            Func<Type, IDomainEventHandler> createEventHandlerFunc = (t) => {
                                                                         return new TestDomainEventHandler();
                                                                     };

            DomainEventHandlerResolver r = new(eventHandlerConfiguration, createEventHandlerFunc);
            List<IDomainEventHandler> result = r.GetDomainEventHandlers(new EstateCreatedEvent(TestData.AggregateId, TestData.EstateName));
            result.ShouldBeNull();
        }

        [Fact]
        public void DomainEventHandlerResolver_GetDomainEventHandlers_NoHandlerFound_ErrorThrown()
        {
            Dictionary<String, String[]> eventHandlerConfiguration = new Dictionary<String, String[]>();
            Func<Type, IDomainEventHandler> createEventHandlerFunc = (t) => {
                                                                         return new TestDomainEventHandler();
                                                                     };

            List<String> handlers = new List<String>();
            handlers.Add("Shared.EventStore.Tests.TestDomainEventHandler1, Shared.EventStore.Tests");
            eventHandlerConfiguration.Add("EstateCreatedEvent", handlers.ToArray());

            Should.Throw<NotSupportedException>(() => {
                                                    new DomainEventHandlerResolver(eventHandlerConfiguration, createEventHandlerFunc);
                                                });
        }
    }
}
