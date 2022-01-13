using System;

namespace Driver
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using EventStore.Client;
    using EventStore.ClientAPI.Common.Log;
    using Shared.EventStore.EventStore;
    using Shared.IntegrationTesting;
    using Shared.Logger;

    class Program
    {
        static async Task Main(string[] args)
        {
            /*
            EventStoreClientSettings settings = new EventStoreClientSettings();
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
            settings.ConnectionName = "Test Connection";
            settings.ConnectivitySettings = new EventStoreClientConnectivitySettings
            {
                Address = new Uri("https://localhost:2113"),
            };
            settings.DefaultCredentials = new UserCredentials("admin","changeit");

            EventStoreClient client = new EventStoreClient(settings);
            EventStoreProjectionManagementClient projectionManagementClient = new EventStoreProjectionManagementClient(settings);
            
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient);
            //IAggregateRepository<TestAggregate> aggregateRepository = new AggregateRepository<TestAggregate>(context);

            //Guid aggregateId = Guid.NewGuid();
            //TestAggregate aggregate = await aggregateRepository.GetLatestVersion(aggregateId, CancellationToken.None);

            //aggregate.SetAggregateName("Test Name");
            //aggregate.SetAggregateName("Test Name1");
            //aggregate.SetAggregateName("Test Name2");
            //aggregate.SetAggregateName("Test Name3");
            //aggregate.SetAggregateName("Test Name4");

            //await aggregateRepository.SaveChanges(aggregate, CancellationToken.None);

            //TestAggregate existingAggregate = await aggregateRepository.GetLatestVersion(aggregateId, CancellationToken.None);
            Guid merchantId = Guid.Parse("df33bc12-ec69-4393-899d-e68305794d5d");
            var x = await context.GetPartitionStateFromProjection("MerchantBalanceCalculator", $"MerchantBalanceHistory-{merchantId:N}", CancellationToken.None);

            */

            TestDockerHelper t = new TestDockerHelper();
            await t.StartContainersForScenarioRun("");

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

                List<(String, String)> subs = new List<(String, String)>();
                subs.Add(("TestStream", "TestGroup1"));
                subs.Add(("TestStream", "TestGroup2"));
                subs.Add(("TestStream1", "TestGroup3"));
                await this.PopulateSubscriptionServiceConfiguration(2113, subs);
            }

            public override async Task StopContainersForScenarioRun()
            {
                
            }
        }
    }
}
