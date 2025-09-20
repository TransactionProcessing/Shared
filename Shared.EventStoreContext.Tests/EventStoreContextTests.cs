using EventStore.Client;
using Newtonsoft.Json;
using NLog;
using Shared.DomainDrivenDesign.EventSourcing;
using Shared.EventStore.Aggregate;
using Shared.EventStore.EventStore;
using Shared.EventStore.Tests;
using Shared.Logger;
using Shared.Middleware;
using Shouldly;
using SimpleResults;

namespace Shared.EventStoreContext.Tests;

public class EventStoreContextTests : IDisposable{
    #region Fields

    private readonly EventStoreDockerHelper EventStoreDockerHelper;

    public EventStoreContextTests()
    {
        NlogLogger logger = new();
        LogManager.Setup(b => {
            b.SetupLogFactory(setup => setup.AddCallSiteHiddenAssembly(typeof(NlogLogger).Assembly));
            b.SetupLogFactory(setup => setup.AddCallSiteHiddenAssembly(typeof(Shared.Logger.Logger).Assembly));
            b.SetupLogFactory(setup => setup.AddCallSiteHiddenAssembly(typeof(TenantMiddleware).Assembly));
            b.LoadConfigurationFromFile("nlog.config");
        });

        logger.Initialise(LogManager.GetLogger("Reqnroll"), "Reqnroll");

        this.EventStoreDockerHelper = new() { Logger = logger };
    }

    #endregion

    #region Methods

    public void Dispose(){
        // Just needed for the interface
    }
        
    [OneTimeTearDown]
    public async Task TearDown(){
        //await this.EventStoreDockerHelper.StopContainersForScenarioRun(sharedDockerServices);
    }

    [Test]
    [TestCase("Running", ProjectionRunningStatus.Running)]
    [TestCase("running", ProjectionRunningStatus.Running)]
    [TestCase("RUNNING", ProjectionRunningStatus.Running)]
    [TestCase("Stopped", ProjectionRunningStatus.Stopped)]
    [TestCase("stopped", ProjectionRunningStatus.Stopped)]
    [TestCase("STOPPED", ProjectionRunningStatus.Stopped)]
    [TestCase("Faulted", ProjectionRunningStatus.Faulted)]
    [TestCase("faulted", ProjectionRunningStatus.Faulted)]
    [TestCase("FAULTED", ProjectionRunningStatus.Faulted)]
    [TestCase("Faulted (Enabled)", ProjectionRunningStatus.Faulted)]
    [TestCase("faulted (Enabled)", ProjectionRunningStatus.Faulted)]
    [TestCase("FAULTED (Enabled)", ProjectionRunningStatus.Faulted)]
    [TestCase("Completed/Stopped/Writing results", ProjectionRunningStatus.Completed)]
    [TestCase("completed/stopped/writing results", ProjectionRunningStatus.Completed)]
    [TestCase("COMPLETED/STOPPED/WRITING RESULTS", ProjectionRunningStatus.Completed)]
    [TestCase("Unknown", ProjectionRunningStatus.Unknown)]
    public void EventStoreContext_GetStatusFrom_CorrectValueReturned(String status, ProjectionRunningStatus expected){
        ProjectionDetails projectionDetails = new(0,
            0,
            0,
            String.Empty,
            0,
            0,
            0,
            status,
            String.Empty,
            "TestProjection",
            String.Empty,
            "0",
            0,
            "",
            0,
            "",
            0,
            0,
            0);
        ProjectionRunningStatus result = EventStore.EventStore.EventStoreContext.GetStatusFrom(projectionDetails);
        result.ShouldBe(expected);
    }

    [Test]
    public void EventStoreContext_GetStatusFrom_ProjectionDetailsIsNull_CorrectValueReturned(){
        ProjectionDetails projectionDetails = null;
        ProjectionRunningStatus result = EventStore.EventStore.EventStoreContext.GetStatusFrom(projectionDetails);
        result.ShouldBe(ProjectionRunningStatus.StatisticsNotFound);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task EventStoreContext_InsertEvents_EventsAreWritten(Boolean secureEventStore){

        await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_InsertEvents_EventsAreWritten_{secureEventStore}");

        TimeSpan deadline = TimeSpan.FromMinutes(2);
            
        IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

        Guid aggreggateId = Guid.NewGuid();
        String streamName = $"TestStream-{aggreggateId:N}";

        EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
        List<IDomainEvent> domainEvents = new(){
            event1
        };

        IEventDataFactory factory = new EventDataFactory();
        EventData[] events = factory.CreateEventDataList(domainEvents);

        Result insertEventResult = await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None);
        insertEventResult.IsSuccess.ShouldBeTrue();
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task EventStoreContext_ReadEvents_EventsAreRead(Boolean secureEventStore){
        TimeSpan deadline = TimeSpan.FromMinutes(2);

        await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEvents_EventsAreRead{secureEventStore}");
            
        IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

        Guid aggreggateId = Guid.NewGuid();
        String streamName = $"TestStream-{aggreggateId:N}";

        EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
        EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
        List<IDomainEvent> domainEvents = new(){
            event1,
            event2
        };

        IEventDataFactory factory = new EventDataFactory();
        EventData[] events = factory.CreateEventDataList(domainEvents);

        Result insertEventResult = await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None);
        insertEventResult.IsSuccess.ShouldBeTrue();

        Result<List<ResolvedEvent>>? resolvedEventsResult = await context.ReadEvents(streamName, 0, CancellationToken.None);

        resolvedEventsResult.IsSuccess.ShouldBeTrue();
        resolvedEventsResult.Data.Count.ShouldBe(events.Length);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task EventStoreContext_ReadEventsBackwards_EventsAreRead(Boolean secureEventStore){
        TimeSpan deadline = TimeSpan.FromMinutes(2);

        await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEventsBackwards_EventsAreRead{secureEventStore}");

        IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

        Guid aggreggateId = Guid.NewGuid();
        String streamName = $"TestStream-{aggreggateId:N}";

        EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
        EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
        List<IDomainEvent> domainEvents = new();
        domainEvents.Add(event1);
        domainEvents.Add(event2);

        IEventDataFactory factory = new EventDataFactory();
        EventData[] events = factory.CreateEventDataList(domainEvents);

        Result insertEventResult = await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None);
        insertEventResult.IsSuccess.ShouldBeTrue();

        Result<List<ResolvedEvent>>? resolvedEventsResult = await context.GetEventsBackward(streamName, events.Length, CancellationToken.None);

        resolvedEventsResult.IsSuccess.ShouldBeTrue();
        resolvedEventsResult.Data.Count.ShouldBe(events.Length);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task EventStoreContext_ReadEventsBackwards_StreamNotFound_EventsAreRead(Boolean secureEventStore){
        TimeSpan deadline = TimeSpan.FromMinutes(2);
            
        await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_ReadEventsBackwards_StreamNotFound_EventsAreRead{secureEventStore}");

        IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

        Guid aggreggateId = Guid.NewGuid();
        String streamName = $"TestStream1-{aggreggateId:N}";
            
        Result<List<ResolvedEvent>>? resolvedEventsResult = await context.GetEventsBackward(streamName, 1, CancellationToken.None);

        resolvedEventsResult.Status.ShouldBe(ResultStatus.NotFound);

    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task EventStoreContext_RunTransientQuery_Faulted_ErrorThrown(Boolean secureEventStore){
        TimeSpan deadline = TimeSpan.FromMinutes(2);

        await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_Faulted_ErrorThrown{secureEventStore}");
            
        IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

        Guid aggreggateId = Guid.NewGuid();
        String streamName = $"TestStream-{aggreggateId:N}";

        EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
        EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
        List<IDomainEvent> domainEvents = new(){
            event1,
            event2
        };

        IEventDataFactory factory = new EventDataFactory();
        EventData[] events = factory.CreateEventDataList(domainEvents);

        //await this.InsertEvents(context, streamName, events, deadline, retryTimeout);
        Result insertEventResult = await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None);
        insertEventResult.IsSuccess.ShouldBeTrue();

        Result<List<ResolvedEvent>>? resolvedEventsResult = await context.ReadEvents(streamName, 0, CancellationToken.None);

        resolvedEventsResult.IsSuccess.ShouldBeTrue();
        resolvedEventsResult.Data.Count.ShouldBe(events.Length);

        String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s,e){\r\n          s.estate.push(e.data.estateName);\r\n        }\r\n  });";

        Result<String>? queryResult = await context.RunTransientQuery(query, CancellationToken.None);
        queryResult.IsFailed.ShouldBeTrue();

        String errors = String.Join("|", queryResult.Errors);
        this.EventStoreDockerHelper.Trace(errors);
        if (errors.Any())
        {
            errors.Contains("Faulted").ShouldBeTrue();
        }
        else
        {
            queryResult.Message.Contains("Faulted").ShouldBeTrue();
        }
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task EventStoreContext_RunTransientQuery_QueryIsRun(Boolean secureEventStore){
        TimeSpan deadline = TimeSpan.FromMinutes(2);

        await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_QueryIsRun{secureEventStore}");

        IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

        Guid aggreggateId = Guid.NewGuid();
        String streamName = $"TestStream-{aggreggateId:N}";

        EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
        EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
        List<IDomainEvent> domainEvents = new(){
            event1,
            event2,
        };
            
        IEventDataFactory factory = new EventDataFactory();
        EventData[] events = factory.CreateEventDataList(domainEvents);

        Result insertEventResult = await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None);
        insertEventResult.IsSuccess.ShouldBeTrue();

        Result<List<ResolvedEvent>>? resolvedEventsResult = await context.ReadEvents(streamName, 0, CancellationToken.None);

        resolvedEventsResult.IsSuccess.ShouldBeTrue();
        resolvedEventsResult.Data.Count.ShouldBe(events.Length);

        String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                estates:[]\r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s,e){\r\n          s.estates.push(e.data.estateName);\r\n        }\r\n  });";

        Result<String>? queryResult = await context.RunTransientQuery(query, CancellationToken.None);
        queryResult.IsSuccess.ShouldBeTrue();
        queryResult.Data.ShouldNotBeNullOrEmpty();

        var definition = new{
            estates = new List<String>()
        };
        var result = JsonConvert.DeserializeAnonymousType(queryResult.Data, definition);

        result.estates.Contains(event1.EstateName).ShouldBeTrue();
        result.estates.Contains(event2.EstateName).ShouldBeTrue();
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown(Boolean secureEventStore){
        TimeSpan deadline = TimeSpan.FromMinutes(2);

        await this.EventStoreDockerHelper.StartContainers(secureEventStore, $"EventStoreContext_RunTransientQuery_ResultIsEmpty_ErrorThrown{secureEventStore}");

        IEventStoreContext context = this.CreateContext(secureEventStore, deadline);

        Guid aggreggateId = Guid.NewGuid();
        String streamName = $"TestStream-{aggreggateId:N}";

        EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
        EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
        List<IDomainEvent> domainEvents = new(){
            event1,
            event2
        };

        IEventDataFactory factory = new EventDataFactory();
        EventData[] events = factory.CreateEventDataList(domainEvents);

        Result insertEventResult = await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None);
        insertEventResult.IsSuccess.ShouldBeTrue();

        Result<List<ResolvedEvent>>? resolvedEventsResult = await context.ReadEvents(streamName, 0, CancellationToken.None);

        resolvedEventsResult.IsSuccess.ShouldBeTrue();
        resolvedEventsResult.Data.Count.ShouldBe(events.Length);

        String query = "fromStream('$et-EstateCreatedEvent')\r\n  .when({\r\n      $init: function (s, e)\r\n        {\r\n            return {\r\n                \r\n            };\r\n        },\r\n        \"EstateCreatedEvent\": function(s, e){\r\n          }\r\n  });";

        Result<String>? queryResult = await context.RunTransientQuery(query, CancellationToken.None);
        queryResult.IsSuccess.ShouldBeTrue();
        queryResult.Data.ShouldBeEmpty();
    }

    private IEventStoreContext CreateContext(Boolean secureEventStore, TimeSpan? deadline = null){
        EventStoreClientSettings settings = this.EventStoreDockerHelper.CreateEventStoreClientSettings(secureEventStore, deadline);

        EventStoreClient client = new(settings);
        EventStoreProjectionManagementClient projectionManagementClient = new(settings);
        IEventStoreContext context = new EventStore.EventStore.EventStoreContext(client, projectionManagementClient, deadline);
        return context;
    }

    #endregion
        
}