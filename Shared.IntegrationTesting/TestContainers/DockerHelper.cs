using System.Threading;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Shared.Serialisation;
using SimpleResults;
using System.Linq;

namespace Shared.IntegrationTesting.TestContainers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

public abstract class DockerHelper : BaseDockerHelper
{
    protected DockerHelper(Boolean skipHealthChecks=false) :base(skipHealthChecks){
        StringSerialiser.Initialise(new SystemTextJsonSerializer(new System.Text.Json.JsonSerializerOptions()));
    }
    
    protected  virtual void SetHostTraceFolder(String scenarioName) {
        String ciEnvVar = Environment.GetEnvironmentVariable("CI");

        Boolean isCI = (!String.IsNullOrEmpty(ciEnvVar) && String.Compare(ciEnvVar, Boolean.TrueString, StringComparison.InvariantCultureIgnoreCase) == 0);

        this.HostTraceFolder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (isCI ? $@"C:\Users\runneradmin\trace\{scenarioName}" : $@"C:\home\txnproc\trace\{scenarioName}")
            : (isCI ? $"/home/runner/trace/{scenarioName}" : $"/home/txnproc/trace/{scenarioName}");
        
        if (Directory.Exists(this.HostTraceFolder) == false){
            this.Trace($"[{this.HostTraceFolder}] does not exist");
            Directory.CreateDirectory(this.HostTraceFolder);
            this.Trace($"[{this.HostTraceFolder}] created");
        }
        else{
            this.Trace($"[{this.HostTraceFolder}] already exists");
        }
        
        this.Trace($"HostTraceFolder is [{this.HostTraceFolder}]");
    }

    
    public override async Task StartContainersForScenarioRun(
        String scenarioName,
        DockerServices dockerServices,
        CancellationToken cancellationToken){

        cancellationToken.ThrowIfCancellationRequested();
        this.IntegrationTestCertificate = IntegrationTestCertificate.Create();
        try
        {
            this.RequiredDockerServices = dockerServices;

            this.IsSecureEventStore = Environment.GetEnvironmentVariable("IsSecureEventStore") switch
            {
                null => false,
                { Length: 0 } => false,
                "false" => false,
                _ => true
            };
            this.SetHostTraceFolder(scenarioName);

            this.TestId = Guid.NewGuid();

            this.Trace($"Test Id is {this.TestId}");

            this.SetupContainerNames();

            this.ClientDetails = ("serviceClient", "Secret1");

            INetwork testNetwork = await this.SetupTestNetwork();
            this.TestNetworks.Add(testNetwork);

            List<INetwork> networks = [
                testNetwork,
        ];

            async Task StartWithTrace(Func<DotNet.Testcontainers.Builders.ContainerBuilder> setupContainer, DockerServices service)
            {
                DateTimeOffset startedAt = DateTimeOffset.UtcNow;
                this.Trace($"Starting container [{service}]");
                await StartContainer2(setupContainer, networks, service, cancellationToken);
                TimeSpan elapsed = DateTimeOffset.UtcNow - startedAt;
                this.Trace($"Container [{service}] started in {elapsed.TotalSeconds:N1}s");
            }

            Func<DockerServices, Func<DotNet.Testcontainers.Builders.ContainerBuilder>> setupContainerForService = service => service switch
            {
                DockerServices.SqlServer => this.ConfigureSqlContainer,
                DockerServices.EventStore => this.SetupEventStoreContainer,
                DockerServices.MessagingService => this.SetupMessagingServiceContainer,
                DockerServices.SecurityService => this.SetupSecurityServiceContainer,
                DockerServices.CallbackHandler => this.SetupCallbackHandlerContainer,
                DockerServices.TestHost => this.SetupTestHostContainer,
                DockerServices.TransactionProcessor => this.SetupTransactionProcessorContainer,
                DockerServices.FileProcessor => this.SetupFileProcessorContainer,
                DockerServices.TransactionProcessorAcl => this.SetupTransactionProcessorAclContainer,
                DockerServices.ConfigurationHost => this.SetupConfigHostContainer,
                DockerServices.EstateManagementUI => this.SetupEstateManagementUiContainer,
                DockerServices.EstateReporting => this.SetupEstateReportingContainer,
                _ => throw new InvalidOperationException($"No startup plan exists for docker service [{service}]")
            };

            foreach (IReadOnlyList<DockerServices> startupGroup in this.GetStartupGroups())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.WhenAll(startupGroup.Select(service => StartWithTrace(setupContainerForService(service), service)));
            }

            await this.LoadEventStoreProjections(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            await this.CreateSubscriptions(cancellationToken);
        }
        catch
        {
            this.DisposeIntegrationTestCertificate();
            throw;
        }
    }

    public void DisposeIntegrationTestCertificate()
    {
        this.IntegrationTestCertificate?.Dispose();
        this.IntegrationTestCertificate = null;
    }

    public override async Task StopContainersForScenarioRun(
    DockerServices sharedDockerServices)
    {
        List<Exception> cleanupErrors = [];
        List<(DockerServices service, IContainer container)> remainingContainers =
            RetainSharedContainers(this.Containers, sharedDockerServices).ToList();
        List<INetwork> remainingNetworks = [];

        try
        {
            foreach ((DockerServices service, IContainer container)
                     in this.Containers.AsEnumerable().Reverse())
            {
                if ((sharedDockerServices & service) == service)
                {
                    continue;
                }

                String containerName;
                try
                {                   
                    containerName = container.Name;
                }
                catch (InvalidOperationException ex)
                {
                    this.Trace($"Skipping unavailable container [{service}] ({ex.Message})");
                    continue;
                }

                try
                {
                    this.Trace($"Stopping container [{containerName}]");

                    await container.StopAsync(CancellationToken.None);
                    await container.DisposeAsync();

                    this.Trace($"Container [{containerName}] stopped");
                }
                catch (Exception ex)
                {
                    this.Trace($"Failed to stop container [{containerName}]");
                    cleanupErrors.Add(ex);
                    remainingContainers.Add((service, container));
                }
            }

            if (remainingContainers.Count > 0)
            {
                remainingNetworks.AddRange(this.TestNetworks);
            }
            else
            {
                foreach (INetwork network in this.TestNetworks.AsEnumerable().Reverse())
                {
                    try
                    {
                        await network.DeleteAsync(CancellationToken.None);
                        await network.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        this.Trace("Failed to remove Docker network");
                        cleanupErrors.Add(ex);
                        remainingNetworks.Add(network);
                    }
                }
            }
        }
        finally
        {
            this.Containers = remainingContainers;
            this.TestNetworks = remainingNetworks;

            if ((sharedDockerServices & DockerServices.SecurityService)
                != DockerServices.SecurityService)
            {
                this.DisposeIntegrationTestCertificate();
            }
        }

        if (cleanupErrors.Count > 0)
        {
            throw new AggregateException(
                "One or more integration-test resources failed to clean up.",
                cleanupErrors);
        }
    }

    protected static IReadOnlyList<(DockerServices service, IContainer container)> RetainSharedContainers(
        IEnumerable<(DockerServices service, IContainer container)> containers,
        DockerServices sharedDockerServices)
    {
        return containers
            .Where(container => (sharedDockerServices & container.service) == container.service)
            .ToList();
    }

    public abstract Task CreateSubscriptions(CancellationToken cancellationToken);
}
