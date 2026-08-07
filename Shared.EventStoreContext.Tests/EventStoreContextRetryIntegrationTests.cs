using System.Text.Json;
using Microsoft.Extensions.Configuration;
using KurrentDB.Client;
using NLog;
using Shared.DomainDrivenDesign.EventSourcing;
using Shared.EventStore.Aggregate;
using Shared.EventStore.EventStore;
using Shared.General;
using Shared.Logger;
using Shared.Serialisation;
using Shared.IntegrationTesting;
using NUnit.Framework;
using System.Linq;
using Shouldly;
using SimpleResults;

namespace Shared.EventStoreContext.Tests;

[NonParallelizable]
public class EventStoreContextRetryIntegrationTests : IDisposable
{
    private readonly EventStoreDockerHelper EventStoreDockerHelper;

    public EventStoreContextRetryIntegrationTests()
    {
        NlogLogger logger = new();
        LogManager.Setup(b =>
        {
            b.SetupLogFactory(setup => setup.AddCallSiteHiddenAssembly(typeof(NlogLogger).Assembly));
            b.SetupLogFactory(setup => setup.AddCallSiteHiddenAssembly(typeof(Shared.Logger.Logger).Assembly));
            b.LoadConfigurationFromFile("nlog.config");
        });

        logger.Initialise(LogManager.GetLogger("Reqnroll"), "Reqnroll");

        this.EventStoreDockerHelper = new() { Logger = logger };
        StringSerialiser.Initialise(new SystemTextJsonSerializer(new JsonSerializerOptions()));
        TypeMap.AddType<EstateCreatedEvent>("EstateCreatedEvent");
    }

    [Test]
    public async Task EventStoreContext_InsertEvents_RetriesAfterDockerOutage()
    {
        this.InitialiseRetryConfiguration();
        await this.EventStoreDockerHelper.StartContainers(false, nameof(this.EventStoreContext_InsertEvents_RetriesAfterDockerOutage));

        TimeSpan deadline = TimeSpan.FromSeconds(5);
        IEventStoreContext context = this.CreateContext(false, deadline);
        context.TraceGenerated += (trace, logLevel) => TestContext.WriteLine($"[{logLevel}] {trace}");

        Guid aggregateId = Guid.NewGuid();
        String streamName = $"RetryStream-{aggregateId:N}";

        EstateCreatedEvent event1 = new(aggregateId, "Retry Estate");
        List<IDomainEvent> domainEvents = new() { event1 };

        IEventDataFactory factory = new EventDataFactory();
        List<EventData> events = factory.CreateEventDataList(domainEvents).ToList();

        Task restartTask = Task.Run(async () =>
        {
            await Task.Delay(2000);
            await this.EventStoreDockerHelper.UnpauseEventStoreContainer();
        });

        await this.EventStoreDockerHelper.PauseEventStoreContainer();

        try
        {
            Result insertEventResult = await context.InsertEvents(streamName, -1, events, CancellationToken.None);

            TestContext.WriteLine($"Insert success: {insertEventResult.IsSuccess}; status: {insertEventResult.Status}; message: {insertEventResult.Message}; errors: {String.Join(" | ", insertEventResult.Errors ?? new List<String>())}");
            await restartTask;

            insertEventResult.IsSuccess.ShouldBeTrue();

            Result<List<ResolvedEvent>>? readEventsResult = null;
            await Retry.For(async () =>
            {
                readEventsResult = await context.ReadEvents(streamName, 0, CancellationToken.None);
                readEventsResult.IsSuccess.ShouldBeTrue();
                readEventsResult.Data.Count.ShouldBe(1);
            }, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(2));
        }
        finally
        {
            await restartTask;
        }
    }

    private IEventStoreContext CreateContext(Boolean secureEventStore, TimeSpan? deadline = null)
    {
        KurrentDBClientSettings settings = this.EventStoreDockerHelper.CreateEventStoreClientSettings(secureEventStore, deadline);
        return new EventStore.EventStore.EventStoreContext(settings, deadline);
    }

    private void InitialiseRetryConfiguration()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<String, String?>
        {
            ["AppSettings:GrpcRetryMaxAttempts"] = "10",
            ["AppSettings:GrpcRetryBaseDelayMilliseconds"] = "1000",
            ["AppSettings:GrpcRetryMaxDelayMilliseconds"] = "5000",
            ["AppSettings:GrpcRetryUseJitter"] = "false",
        });

        ConfigurationReader.Initialise(builder.Build());
    }

    public void Dispose()
    {
        ConfigurationReader.Initialise(new ConfigurationRoot(new List<IConfigurationProvider>()));
    }
}
