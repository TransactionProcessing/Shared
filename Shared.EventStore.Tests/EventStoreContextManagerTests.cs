using EventStore.Client;
using Microsoft.Extensions.Configuration;
using Moq;
using Shared.EventStore.EventStore;
using Shared.General;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Shared.EventStore.Tests;

public class EventStoreContextManagerTests {
    private readonly EventStoreContextManager Manager;
    
    private readonly Mock<IEventStoreContext> Context1;
    private readonly Mock<IEventStoreContext> Context2;
    public EventStoreContextManagerTests() {
        this.Context1 = new Mock<IEventStoreContext>();
        this.Context2 = new Mock<IEventStoreContext>();
        this.Manager = new EventStoreContextManager((s) => {
            if (s.Contains("133"))
                return this.Context1.Object;
            return this.Context2.Object;
        });
        
        IReadOnlyDictionary<String, String> defaultAppSettings = new Dictionary<String, String>
        {
            ["EventStoreSettings:ConnectionString"] = "https://192.168.1.133:2113",
            ["EventStoreSettings:ConnectionString1"] = "https://192.168.1.134:2113",
        };
        IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().AddInMemoryCollection(defaultAppSettings);
        ConfigurationReader.Initialise(configurationBuilder.Build());
    }

    [Fact]
    public void EventStoreContextManager_GetContext_ByConnectionIdentifier_ContextReturned() {
        var ctx = this.Manager.GetEventStoreContext("ConnectionString");
        ctx.ShouldNotBeNull();
    }

    [Fact]
    public void EventStoreContextManager_GetContextFromResolver_UseCache_ByConnectionIdentifier_ContextReturned()
    {
        var ctx = this.Manager.GetEventStoreContext("ConnectionString");
        ctx.ShouldNotBeNull();
        var ctx2 = this.Manager.GetEventStoreContext("ConnectionString");
        ctx2.ShouldNotBeNull();
        ctx.GetHashCode().ShouldBe(ctx2.GetHashCode());
    }

    [Fact]
    public void EventStoreContextManager_GetContextFromResolver_MultipleContexts_ContextReturned()
    {
        var ctx = this.Manager.GetEventStoreContext("ConnectionString");
        ctx.ShouldNotBeNull();

        var ctx2 = this.Manager.GetEventStoreContext("ConnectionString1");
        ctx2.ShouldNotBeNull();
        ctx.GetHashCode().ShouldNotBe(ctx2.GetHashCode());
    }
}