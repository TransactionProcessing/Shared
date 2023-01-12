namespace Shared.EventStore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Aggregate;
    using DomainDrivenDesign.EventSourcing;
    using Ductus.FluentDocker.Builders;
    using Ductus.FluentDocker.Commands;
    using Ductus.FluentDocker.Model.Builders;
    using Ductus.FluentDocker.Model.Containers;
    using Ductus.FluentDocker.Services;
    using Ductus.FluentDocker.Services.Extensions;
    using EventStore;
    using global::EventStore.Client;
    using Shouldly;
    using Xunit;

    public class EventStoreContextTests : IDisposable
    {
        #region Fields

        private readonly List<IContainerService> Containers = new List<IContainerService>();

        private Int32 EventStoreHttpPort;

        private readonly List<INetworkService> TestNetworks = new List<INetworkService>();

        #endregion

        #region Methods

        public void Dispose() {
            this.StopContainers();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_InsertEvents_EventsAreWritten(Boolean secureEventStore) {
            await this.StartContainers(secureEventStore);

            await Task.Delay(TimeSpan.FromSeconds(30));

            EventStoreClientSettings settings = this.CreateEventStoreClientSettings(secureEventStore);

            EventStoreClient client = new(settings);
            EventStoreProjectionManagementClient projectionManagementClient = new(settings);
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);
            Should.NotThrow(async () => { await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None); });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EventStoreContext_ReadEvents_EventsAreRead(Boolean secureEventStore) {
            await this.StartContainers(secureEventStore);

            await Task.Delay(TimeSpan.FromSeconds(30));

            EventStoreClientSettings settings = this.CreateEventStoreClientSettings(secureEventStore);
            
            EventStoreClient client = new(settings);
            EventStoreProjectionManagementClient projectionManagementClient = new(settings);
            IEventStoreContext context = new EventStoreContext(client, projectionManagementClient);

            Guid aggreggateId = Guid.NewGuid();
            String streamName = $"TestStream-{aggreggateId:N}";

            EstateCreatedEvent event1 = new(aggreggateId, "Test Estate 1");
            EstateCreatedEvent event2 = new(aggreggateId, "Test Estate 2");
            List<IDomainEvent> domainEvents = new();
            domainEvents.Add(event1);
            domainEvents.Add(event2);

            IEventDataFactory factory = new EventDataFactory();
            EventData[] events = factory.CreateEventDataList(domainEvents);
            await context.InsertEvents(streamName, -1, events.ToList(), CancellationToken.None);

            Should.NotThrow(async () => {
                                List<ResolvedEvent> resolvedEvents = await context.ReadEvents(streamName, 0, CancellationToken.None);

                                resolvedEvents.Count.ShouldBe(events.Length);
                            });
        }

        private EventStoreClientSettings CreateEventStoreClientSettings(Boolean secureEventStore) {
            String connectionString = secureEventStore switch {
                true => $"esdb://admin:changeit@127.0.0.1:{this.EventStoreHttpPort}?tls=true&tlsVerifyCert=false",
                _ => $"esdb://admin:changeit@127.0.0.1:{this.EventStoreHttpPort}?tls=false"
            };

            EventStoreClientSettings settings = EventStoreClientSettings.Create(connectionString);
            settings.ConnectivitySettings.Insecure = secureEventStore switch {
                true => false,
                _ => true
            };

            if (secureEventStore == false) {
                settings.CreateHttpMessageHandler = () => new SocketsHttpHandler {
                                                                                     SslOptions = {
                                                                                                      RemoteCertificateValidationCallback = (sender,
                                                                                                          certificate,
                                                                                                          chain,
                                                                                                          errors) => true,
                                                                                                  }
                                                                                 };
            }

            return settings;
        }

        public static DockerEnginePlatform GetDockerEnginePlatform()
        {
            IHostService docker = GetDockerHost();

            if (docker.Host.IsLinuxEngine())
            {
                return DockerEnginePlatform.Linux;
            }

            if (docker.Host.IsWindowsEngine())
            {
                return DockerEnginePlatform.Windows;
            }

            throw new Exception("Unknown Engine Type");
        }

        public enum DockerEnginePlatform
        {
            Linux,

            Windows
        }

        public static IHostService GetDockerHost()
        {
            IList<IHostService> hosts = new Hosts().Discover();
            IHostService docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");
            return docker;
        }

        public virtual INetworkService SetupTestNetwork(String networkName = null,
                                                        Boolean reuseIfExists = false)
        {

            networkName = String.IsNullOrEmpty(networkName) ? $"testnetwork{Guid.NewGuid()}" : networkName;
            DockerEnginePlatform engineType = GetDockerEnginePlatform();

            if (engineType == DockerEnginePlatform.Windows)
            {
                var docker = GetDockerHost();
                var network = docker.GetNetworks().Where(nw => nw.Name == networkName).SingleOrDefault();
                if (network == null)
                {
                    Dictionary<String, String> driverOptions = new Dictionary<String, String>();
                    driverOptions.Add("com.docker.network.windowsshim.networkname", networkName);

                    network = docker.CreateNetwork(networkName,
                                                   new NetworkCreateParams
                                                   {
                                                       Driver = "nat",
                                                       DriverOptions = driverOptions,
                                                       Attachable = true,
                                                   });
                }

                return network;
            }

            if (engineType == DockerEnginePlatform.Linux)
            {
                // Build a network
                NetworkBuilder networkService = new Builder().UseNetwork(networkName).ReuseIfExist();

                return networkService.Build();
            }

            return null;
        }

        private async Task StartContainers(Boolean isSecureEventStore) {
            INetworkService networkService = this.SetupTestNetwork($"testNetwork-{Guid.NewGuid():N}", true);
            this.TestNetworks.Add(networkService);
            DockerEnginePlatform engineType = GetDockerEnginePlatform();
            String imageName = "eventstore/eventstore:21.10.0-buster-slim";
            if (engineType == DockerEnginePlatform.Windows) {
                imageName = "stuartferguson/eventstore";
            }


            IContainerService containerService = await this.StartEventStoreContainer(imageName,
                                                                               $"eventStore-{Guid.NewGuid():N}",
                                                                               isSecureEventStore,
                                                                               networkService);
            this.Containers.Add(containerService);
            this.EventStoreHttpPort = containerService.ToHostExposedEndpoint($"{EventStoreContextTests.EventStoreHttpDockerPort}/tcp").Port;

        }

        private async Task<IContainerService> StartEventStoreContainer(String imageName,
                                                           String containerName,
                                                           Boolean isSecureContainer,
                                                           INetworkService networkService,
                                                           Boolean forceLatestImage = false) {
            List<String> environmentVariables = new() {
                                                          "EVENTSTORE_RUN_PROJECTIONS=all",
                                                          "EVENTSTORE_START_STANDARD_PROJECTIONS=true",
                                                          "EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP=true",
                                                          "EVENTSTORE_ENABLE_EXTERNAL_TCP=true"
                                                      };

            String containerPath = GetDockerEnginePlatform() switch
            {
                DockerEnginePlatform.Windows => "C:\\Logs",
                _ => "/var/log/eventstore"
            };

            ContainerBuilder eventStoreContainerBuilder = new Builder().UseContainer().UseImage(imageName,true)
                                                                       .ExposePort(EventStoreContextTests.EventStoreHttpDockerPort).ExposePort(EventStoreContextTests.EventStoreTcpDockerPort)
                                                                       .WithName(containerName);//.MountHostFolder(this.HostTraceFolder, containerPath);

            if (isSecureContainer == false)
            {
                environmentVariables.Add("EVENTSTORE_INSECURE=true");
            }
            else
            {
                // Copy these to the container
                String path = Path.Combine(Directory.GetCurrentDirectory(), "certs");

                eventStoreContainerBuilder = eventStoreContainerBuilder.Mount(path, "/etc/eventstore/certs", MountType.ReadWrite);

                // Certificates configuration
                environmentVariables.Add("EVENTSTORE_CertificateFile=/etc/eventstore/certs/node1/node.crt");
                environmentVariables.Add("EVENTSTORE_CertificatePrivateKeyFile=/etc/eventstore/certs/node1/node.key");
                environmentVariables.Add("EVENTSTORE_TrustedRootCertificatesPath=/etc/eventstore/certs/ca");
            }


            eventStoreContainerBuilder = eventStoreContainerBuilder.WithEnvironment(environmentVariables.ToArray());

            IContainerService builtContainer = eventStoreContainerBuilder.Build().Start();
            networkService.Attach(builtContainer, false);

            await Retry.For(async () => { builtContainer = builtContainer.WaitForPort($"{EventStoreContextTests.EventStoreHttpDockerPort}/tcp"); });

            return builtContainer;
        }

        private void StopContainers() {
            if (this.Containers.Any()) {
                foreach (IContainerService containerService in this.Containers) {
                    containerService.StopOnDispose = true;
                    containerService.RemoveOnDispose = true;
                    containerService.Dispose();
                }
            }

            if (this.TestNetworks.Any()) {
                foreach (INetworkService networkService in this.TestNetworks) {
                    networkService.Stop();
                    networkService.Remove(true);
                }
            }
        }

        #endregion

        #region Others

        private const Int32 EventStoreHttpDockerPort = 2113;

        private const Int32 EventStoreTcpDockerPort = 1113;

        #endregion
    }

    public static class Retry
    {
        #region Fields

        /// <summary>
        /// The default retry for
        /// </summary>
        private static readonly TimeSpan DefaultRetryFor = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The default retry interval
        /// </summary>
        private static readonly TimeSpan DefaultRetryInterval = TimeSpan.FromSeconds(5);

        #endregion

        #region Methods

        /// <summary>
        /// Fors the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="retryFor">The retry for.</param>
        /// <param name="retryInterval">The retry interval.</param>
        /// <returns></returns>
        public static async Task For(Func<Task> action,
                                     TimeSpan? retryFor = null,
                                     TimeSpan? retryInterval = null)
        {
            DateTime startTime = DateTime.Now;
            Exception lastException = null;

            if (retryFor == null)
            {
                retryFor = Retry.DefaultRetryFor;
            }

            while (DateTime.Now.Subtract(startTime).TotalMilliseconds < retryFor.Value.TotalMilliseconds)
            {
                try
                {
                    await action().ConfigureAwait(false);
                    lastException = null;
                    break;
                }
                catch (Exception e)
                {
                    lastException = e;

                    // wait before retrying
                    Thread.Sleep(retryInterval ?? Retry.DefaultRetryInterval);
                }
            }

            if (lastException != null)
            {
                throw lastException;
            }
        }

        #endregion
    }
}