using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Shared.EventStore.EventStore;
using Shared.Repositories;
using Shouldly;
using Xunit;

namespace Shared.EventStore.Tests
{
    public class EventStoreContextManagerTests {
        private readonly EventStoreContextManager Manager1;
        private readonly EventStoreContextManager Manager2;
        private readonly Mock<IEventStoreContext> Context;
        private readonly Mock<IConnectionStringConfigurationRepository> ConnectionStringConfigurationRepository;
        public EventStoreContextManagerTests() {
            this.Context = new Mock<IEventStoreContext>();
            this.Manager1 = new EventStoreContextManager(this.Context.Object);
            this.ConnectionStringConfigurationRepository = new Mock<IConnectionStringConfigurationRepository>();
            this.Manager2 = new EventStoreContextManager((str) => this.Context.Object, this.ConnectionStringConfigurationRepository.Object);
        }

        [Fact]
        public void EventStoreContextManager_GetContext_ByConnectionIdentifier_ContextReturned() {
            var ctx = this.Manager1.GetEventStoreContext("111EDCB6-1F7C-49D5-AA6D-61CC30FF118E");
            ctx.ShouldNotBeNull();
        }

        [Fact]
        public void EventStoreContextManager_GetContextFromResolver_ByConnectionIdentifier_ContextReturned()
        {
            var ctx = this.Manager2.GetEventStoreContext("111EDCB6-1F7C-49D5-AA6D-61CC30FF118E");
            ctx.ShouldNotBeNull();
        }

        [Fact]
        public void EventStoreContextManager_GetContextFromResolver_UseCache_ByConnectionIdentifier_ContextReturned()
        {
            var ctx = this.Manager2.GetEventStoreContext("111EDCB6-1F7C-49D5-AA6D-61CC30FF118E");
            ctx.ShouldNotBeNull();
            var ctx2 = this.Manager2.GetEventStoreContext("111EDCB6-1F7C-49D5-AA6D-61CC30FF118E");
            ctx2.ShouldNotBeNull();
            ctx.GetHashCode().ShouldBe(ctx2.GetHashCode());
        }

        [Fact]
        public void EventStoreContextManager_GetContext_ByConnectionIdentifier_AndType_ContextReturned()
        {
            var ctx = this.Manager1.GetEventStoreContext("111EDCB6-1F7C-49D5-AA6D-61CC30FF118E", "EventStoreConnection");
            ctx.ShouldNotBeNull();
        }
    }
}
