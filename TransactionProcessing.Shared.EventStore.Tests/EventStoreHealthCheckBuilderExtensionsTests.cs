using KurrentDB.Client;

namespace TransactionProcessing.Shared.EventStore.Tests;

using Extensions;
using global::EventStore.Client;
using Microsoft.Extensions.DependencyInjection;
using TestObjects;
using Xunit;

public class EventStoreHealthCheckBuilderExtensionsTests
{
    [Fact]
    public void AddEventStore_HealthCheckAdded(){
        IHealthChecksBuilder builder = new TestHealthChecksBuilder();
        builder.AddEventStore(new KurrentDBClientSettings(), null, null, null);
    }
}
