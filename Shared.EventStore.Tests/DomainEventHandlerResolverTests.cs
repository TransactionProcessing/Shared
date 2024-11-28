using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.EventStore.Tests
{
    using Shared.EventStore.EventHandling;
    using Shared.EventStore.Tests.TestObjects;
    using Shouldly;
    using Xunit;

    public class DomainEventHandlerResolverTests
    {
        [Fact]
        public void DomainEventHandlerResolver_GetDomainEventHandlers_HandlerResolved(){
            Dictionary<String, String[]> eventHandlerConfiguration= new Dictionary<String, String[]>();
            Func<Type, IDomainEventHandler> createEventHandlerFunc = (t) => {
                                                                         return new TestDomainEventHandler();
                                                                     };

            List<String> handlers = new List<String>();
            handlers.Add("Shared.EventStore.Tests.TestObjects.TestDomainEventHandler, Shared.EventStore.Tests");
            eventHandlerConfiguration.Add("EstateCreatedEvent", handlers.ToArray());

            DomainEventHandlerResolver r = new DomainEventHandlerResolver(eventHandlerConfiguration, createEventHandlerFunc);
            List<IDomainEventHandler> result  = r.GetDomainEventHandlers(new EstateCreatedEvent(TestData.AggregateId, TestData.EstateName));
            result.Count.ShouldBe(1);
            result.Single().ShouldBeOfType(typeof(TestDomainEventHandler));
        }

        [Fact]
        public void DomainEventHandlerResolver_EventNotConfigured_NullReturned()
        {
            Dictionary<String, String[]> eventHandlerConfiguration = new Dictionary<String, String[]>();
            Func<Type, IDomainEventHandler> createEventHandlerFunc = (t) => {
                                                                         return new TestDomainEventHandler();
                                                                     };

            DomainEventHandlerResolver r = new DomainEventHandlerResolver(eventHandlerConfiguration, createEventHandlerFunc);
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
