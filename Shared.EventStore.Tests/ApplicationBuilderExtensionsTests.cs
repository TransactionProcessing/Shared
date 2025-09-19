using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Moq;
using Shared.EventStore.EventHandling;
using Shared.EventStore.Extensions;
using Shared.EventStore.SubscriptionWorker;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Shared.EventStore.Tests
{
    public class ApplicationBuilderExtensionsTests
    {
        private readonly ITestOutputHelper TestOutputHelper;

        public ApplicationBuilderExtensionsTests(ITestOutputHelper testOutputHelper) {
            this.TestOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ApplicationBuilderExtensions_ConfigureSubscriptions_WorkerListReturned()
        {
            Mock<ISubscriptionRepository> subscriptionRepository = new();

            SubscriptionWorkersRoot config = new() { 
                SubscriptionWorkers = new List<SubscriptionWorkerConfig>()
                {
                    new SubscriptionWorkerConfig
                    {
                        Enabled = true,
                        IsOrdered = true,
                    },
                    new SubscriptionWorkerConfig
                    {
                        Enabled = true,
                        IsOrdered = false,
                        InstanceCount = 1,
                    },new SubscriptionWorkerConfig
                    {
                        Enabled = true,
                        IsDomainOnly = true,
                        InstanceCount = 1,
                    }

                }
            };
            Mock<IDomainEventHandlerResolver> deh = new();
            String eventStoreConnectionString = "esdb://192.168.0.133:2113?tls=true&tlsVerifyCert=false";
            Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers = new();
            eventHandlerResolvers.Add("Ordered", deh.Object);
            eventHandlerResolvers.Add("Main", deh.Object);
            eventHandlerResolvers.Add("Domain", deh.Object);
            Action<TraceEventType, String, String> traceHandler = (et, type, msg)=> { TestOutputHelper.WriteLine(msg); };
            var result = IApplicationBuilderExtenstions.ConfigureSubscriptions(subscriptionRepository.Object, config,
                eventStoreConnectionString, eventHandlerResolvers, traceHandler);
            result.Count.ShouldBe(3);
        }

        [Fact]
        public void ApplicationBuilderExtensions_ConfigureSubscriptions_NoneEnabled_WorkerListReturned()
        {
            Mock<ISubscriptionRepository> subscriptionRepository = new();

            SubscriptionWorkersRoot config = new()
            {
                SubscriptionWorkers = new List<SubscriptionWorkerConfig>()
                {
                    new SubscriptionWorkerConfig
                    {
                        Enabled = false,
                        IsOrdered = true,
                    },
                    new SubscriptionWorkerConfig
                    {
                        Enabled = false,
                        IsOrdered = false,
                        InstanceCount = 1,
                    }
                }
            };
            Mock<IDomainEventHandlerResolver> deh = new();
            String eventStoreConnectionString = "esdb://192.168.0.133:2113?tls=true&tlsVerifyCert=false";
            Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers = new();
            eventHandlerResolvers.Add("Ordered", deh.Object);
            eventHandlerResolvers.Add("Main", deh.Object);
            Action<TraceEventType, String, String> traceHandler = (et, type, msg) => TestOutputHelper.WriteLine(msg);
            var result = IApplicationBuilderExtenstions.ConfigureSubscriptions(subscriptionRepository.Object, config,
                eventStoreConnectionString, eventHandlerResolvers, traceHandler);
            result.Count.ShouldBe(0);
        }

        [Fact]
        public void ApplicationBuilderExtensions_ConfigureSubscriptions_NoWorkers_WorkerListReturned()
        {
            Mock<ISubscriptionRepository> subscriptionRepository = new();

            SubscriptionWorkersRoot config = new()
            {
                SubscriptionWorkers = new List<SubscriptionWorkerConfig>()
            };
            String eventStoreConnectionString = "esdb://192.168.0.133:2113?tls=true&tlsVerifyCert=false";
            Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers = new();
            Action<TraceEventType, String, String> traceHandler = null;
            var result = IApplicationBuilderExtenstions.ConfigureSubscriptions(subscriptionRepository.Object, config,
                eventStoreConnectionString, eventHandlerResolvers, traceHandler);
            result.Count.ShouldBe(0);
        }

        [Fact]
        public void ApplicationBuilderExtensions_ConfigureSubscriptions_OrderedOnlyWorkers_WorkerListReturned()
        {
            Mock<ISubscriptionRepository> subscriptionRepository = new();

            SubscriptionWorkersRoot config = new()
            {
                SubscriptionWorkers = new List<SubscriptionWorkerConfig>
                {
                    new SubscriptionWorkerConfig
                    {
                        Enabled = true,
                        IsOrdered = true,
                    },
                }
            };
            Mock<IDomainEventHandlerResolver> deh = new();
            String eventStoreConnectionString = "esdb://192.168.0.133:2113?tls=true&tlsVerifyCert=false";
            Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers = new();
            eventHandlerResolvers.Add("Ordered", deh.Object);
            Action<TraceEventType, String, String> traceHandler = (et, type, msg) => TestOutputHelper.WriteLine(msg);
            var result = IApplicationBuilderExtenstions.ConfigureSubscriptions(subscriptionRepository.Object, config,
                eventStoreConnectionString, eventHandlerResolvers, traceHandler);
            result.Count.ShouldBe(1);
            result.Single().InflightMessages.ShouldBe(1);
        }

        [Fact]
        public void ApplicationBuilderExtensions_ConfigureSubscriptions_OrderedOnlyWorkers_NoHandlers_WorkerListReturned()
        {
            Mock<ISubscriptionRepository> subscriptionRepository = new();

            SubscriptionWorkersRoot config = new()
            {
                SubscriptionWorkers = new List<SubscriptionWorkerConfig>
                {
                    new SubscriptionWorkerConfig
                    {
                        Enabled = true,
                        IsOrdered = true,
                    },
                }
            };
            String eventStoreConnectionString = "esdb://192.168.0.133:2113?tls=true&tlsVerifyCert=false";
            Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers = new();
            Action<TraceEventType, String, String> traceHandler = (et, type, msg) => TestOutputHelper.WriteLine(msg);
            var result = IApplicationBuilderExtenstions.ConfigureSubscriptions(subscriptionRepository.Object, config,
                eventStoreConnectionString, eventHandlerResolvers, traceHandler);
            result.Count.ShouldBe(0);
        }

        [Fact]
        public void ApplicationBuilderExtensions_ConfigureSubscriptions_MainOnlyWorkers_WorkerListReturned()
        {
            Mock<ISubscriptionRepository> subscriptionRepository = new();

            SubscriptionWorkersRoot config = new()
            {
                SubscriptionWorkers = new List<SubscriptionWorkerConfig>
                {
                    new SubscriptionWorkerConfig
                    {
                        Enabled = true,
                        IsOrdered = false,
                        InstanceCount = 1,
                        InflightMessages= 500
                    }
                }
            };
            Mock<IDomainEventHandlerResolver> deh = new();
            String eventStoreConnectionString = "esdb://192.168.0.133:2113?tls=true&tlsVerifyCert=false";
            Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers = new();
            eventHandlerResolvers.Add("Main", deh.Object);
            Action<TraceEventType, String, String> traceHandler = (et, type, msg) => { TestOutputHelper.WriteLine(msg); };
            var result = IApplicationBuilderExtenstions.ConfigureSubscriptions(subscriptionRepository.Object, config,
                eventStoreConnectionString, eventHandlerResolvers, traceHandler);
            result.Count.ShouldBe(1);
            result.Single().InflightMessages.ShouldBe(500);
        }

        [Fact]
        public void ApplicationBuilderExtensions_ConfigureSubscriptions_MainOnlyWorkers_NoHandlers_WorkerListReturned()
        {
            Mock<ISubscriptionRepository> subscriptionRepository = new();

            SubscriptionWorkersRoot config = new()
            {
                SubscriptionWorkers = new List<SubscriptionWorkerConfig>
                {
                    new SubscriptionWorkerConfig
                    {
                        Enabled = true,
                        IsOrdered = false,
                        InstanceCount = 1,
                        InflightMessages= 500
                    }
                }
            };
            String eventStoreConnectionString = "esdb://192.168.0.133:2113?tls=true&tlsVerifyCert=false";
            Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers = new();
            Action<TraceEventType, String, String> traceHandler = (et, type, msg) => TestOutputHelper.WriteLine(msg);
            var result = IApplicationBuilderExtenstions.ConfigureSubscriptions(subscriptionRepository.Object, config,
                eventStoreConnectionString, eventHandlerResolvers, traceHandler);
            result.Count.ShouldBe(0);            
        }

        [Fact]
        public void ApplicationBuilderExtensions_ConfigureSubscriptions_MainOnlyWorkers_InstanceCount2_WorkerListReturned()
        {
            Mock<ISubscriptionRepository> subscriptionRepository = new();

            SubscriptionWorkersRoot config = new()
            {
                SubscriptionWorkers = new List<SubscriptionWorkerConfig>
                {
                    new SubscriptionWorkerConfig
                    {
                        Enabled = true,
                        IsOrdered = false,
                        InstanceCount = 2,
                        InflightMessages= 500
                    }
                }
            };
            Mock<IDomainEventHandlerResolver> deh = new();
            String eventStoreConnectionString = "esdb://192.168.0.133:2113?tls=true&tlsVerifyCert=false";
            Dictionary<String, IDomainEventHandlerResolver> eventHandlerResolvers = new();
            eventHandlerResolvers.Add("Main", deh.Object);
            Action<TraceEventType, String, String> traceHandler = (et, type, msg) => { TestOutputHelper.WriteLine(msg); };
            Func<String, Int32, ISubscriptionRepository> subscriptionRepositoryResolver = (s, i) => subscriptionRepository.Object;
            var result = IApplicationBuilderExtenstions.ConfigureSubscriptions(subscriptionRepository.Object, config,
                eventStoreConnectionString, eventHandlerResolvers, traceHandler);
            result.Count.ShouldBe(2);            
        }
    }
}
