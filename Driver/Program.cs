using System;

namespace Driver
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.Client;
    using Grpc.Core;
    using Shared.DomainDrivenDesign.EventSourcing;
    using Shared.EventStore.Aggregate;
    using Shared.EventStore.EventStore;
    using Shared.IntegrationTesting;
    using Shared.Logger;

    class Program
    {
        internal static EventStoreClientSettings ConfigureEventStoreSettings()
        {
            EventStoreClientSettings settings = EventStoreClientSettings.Create("esdb://127.0.0.1:2113?tls=false");

            settings.CreateHttpMessageHandler = () => new SocketsHttpHandler
            {
                SslOptions =
                                                          {
                                                              RemoteCertificateValidationCallback = (sender,
                                                                                                     certificate,
                                                                                                     chain,
                                                                                                     errors) => true,
                                                          }
            };

            settings.ConnectivitySettings = EventStoreClientConnectivitySettings.Default;
            settings.ConnectivitySettings.Insecure = true;
            settings.ConnectivitySettings.Address = new Uri("esdb://127.0.0.1:2113?tls=false&tlsVerifyCert=false");
            settings.ConnectionName = "test";
            
            return settings;
        }

        static async Task Main(string[] args) {
            EventStoreClientSettings settings = Program.ConfigureEventStoreSettings();

            EventStoreClient client = new(settings);
            
            
            EventStoreProjectionManagementClient projectionManagementClient = new EventStoreProjectionManagementClient(settings);
            
            //var x = await projectionManagementClient.GetStatusAsync("$by_category", cancellationToken: CancellationToken.None);
            
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient);
            IAggregateRepository<TestAggregate, DomainEvent> aggregateRepository = new AggregateRepository<TestAggregate, DomainEvent>(context);

            Guid aggregateId = Guid.Parse("02398284-8284-8284-8284-165402398284");
            TestAggregate aggregate = await aggregateRepository.GetLatestVersion(aggregateId, CancellationToken.None);

            aggregate.SetAggregateName("Test Name");
            //aggregate.SetAggregateName("Test Name1");
            //aggregate.SetAggregateName("Test Name2");
            //aggregate.SetAggregateName("Test Name3");
            //aggregate.SetAggregateName("Test Name4");

            await aggregateRepository.SaveChanges(aggregate, CancellationToken.None);
            
            //TestDockerHelper t = new TestDockerHelper();
            //await t.StartContainersForScenarioRun("");

        }

        public class ConsoleLogger : ILogger
        {
            public Boolean IsInitialised { get; set; }

            public void LogCritical(Exception exception)
            {
                Console.WriteLine(exception);
            }

            public void LogDebug(String message)
            {
                Console.WriteLine(message);
            }

            public void LogError(Exception exception)
            {
                Console.WriteLine(exception);
            }

            public void LogInformation(String message)
            {
                Console.WriteLine(message);
            }

            public void LogTrace(String message)
            {
                Console.WriteLine(message);
            }

            public void LogWarning(String message)
            {
                Console.WriteLine(message);
            }
        }

        public class TestDockerHelper : DockerHelper
        {
            public TestDockerHelper()
            {
                this.Logger = new ConsoleLogger();
            }
            public override async Task StartContainersForScenarioRun(String scenarioName)
            {
                //await this.LoadEventStoreProjections(2113);

                List<(String, String, Int32)> subs = new List<(String, String, Int32)>();
                subs.Add(("TestStream", "TestGroup1",0));
                subs.Add(("TestStream", "TestGroup2",0));
                subs.Add(("TestStream1", "TestGroup3",1));
                await this.PopulateSubscriptionServiceConfiguration(2113, subs);
            }

            public override async Task StopContainersForScenarioRun()
            {
                
            }
        }
    }
}
