using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EventStore.Client;
using NLog;
using NUnit.Framework;
using Shared.Logger;
using Shared.IntegrationTesting;
using Shared.IntegrationTesting.TestContainers;
using Shouldly;

namespace Shared.IntegrationTesting.Tests;

public class DockerHelperStartupPlanTests
{
    private sealed class InspectableDockerHelper : TestDockerHelper
    {
        public IReadOnlyList<IReadOnlyList<DockerServices>> GetStartupGroupsForTesting()
        {
            return base.GetStartupGroups();
        }

        public HttpClient GetEventStoreHealthCheckClientForTesting()
        {
            return BaseDockerHelper.EventStoreHealthCheckClient;
        }

        public Task CaptureContainerLogsForTesting(IContainer container)
        {
            return base.CaptureContainerLogs(container, CancellationToken.None);
        }

        public static IReadOnlyList<(DockerServices service, IContainer container)> GetRetainedSharedContainers(
            IEnumerable<(DockerServices service, IContainer container)> containers,
            DockerServices sharedDockerServices)
        {
            return DockerHelper.RetainSharedContainers(containers, sharedDockerServices);
        }

    }

    private sealed class FailingProjectionDockerHelper : TestDockerHelper
    {
        private readonly FileInfo projectionFile;

        public FailingProjectionDockerHelper(FileInfo projectionFile)
        {
            this.projectionFile = projectionFile;

            NlogLogger logger = new();
            logger.Initialise(LogManager.GetLogger(nameof(FailingProjectionDockerHelper)), nameof(FailingProjectionDockerHelper));
            this.Logger = logger;
        }

        public Task LoadProjectionsForTesting()
        {
            return base.LoadEventStoreProjections(CancellationToken.None);
        }

        protected override IReadOnlyList<FileInfo> GetProjectionFiles()
        {
            return [this.projectionFile];
        }

        protected override List<string> GetRequiredProjections()
        {
            return [this.projectionFile.Name];
        }

        protected override Task CreateContinuousProjection(
            EventStoreProjectionManagementClient projectionClient,
            string projectionName,
            string projection)
        {
            throw new InvalidOperationException("projection creation failed");
        }
    }

    [Test]
    public void StartupGroups_place_foundation_services_first_and_estate_management_ui_last_on_linux()
    {
        InspectableDockerHelper helper = new();

        IReadOnlyList<IReadOnlyList<DockerServices>> startupGroups = helper.GetStartupGroupsForTesting();

        startupGroups.Select(group => group.ToArray()).ShouldBe([
            [DockerServices.SqlServer],
            [DockerServices.EventStore],
            [
                DockerServices.MessagingService,
                DockerServices.SecurityService,
                DockerServices.CallbackHandler,
                DockerServices.TestHost,
                DockerServices.TransactionProcessor,
                DockerServices.FileProcessor,
                DockerServices.TransactionProcessorAcl,
                DockerServices.ConfigurationHost,
                DockerServices.EstateReporting
            ],
            [DockerServices.EstateManagementUI]
        ]);
    }

    [Test]
    public void Shared_containers_are_retained_for_later_cleanup()
    {
        List<(DockerServices service, IContainer container)> containers =
        [
            (DockerServices.SqlServer, null!),
            (DockerServices.EventStore, null!)
        ];

        IReadOnlyList<(DockerServices service, IContainer container)> remaining =
            InspectableDockerHelper.GetRetainedSharedContainers(containers, DockerServices.SqlServer);

        remaining.Select(container => container.service).ShouldBe([DockerServices.SqlServer]);
    }

    [Test]
    public void EventStore_health_checks_reuse_their_http_client()
    {
        InspectableDockerHelper helper = new();

        HttpClient firstClient = helper.GetEventStoreHealthCheckClientForTesting();
        HttpClient secondClient = helper.GetEventStoreHealthCheckClientForTesting();

        secondClient.ShouldBeSameAs(firstClient);
    }

    [Test]
    public void Host_folder_mount_does_not_require_docker_platform()
    {
        ContainerBuilder builder = new();

        ContainerBuilder mountedBuilder = builder.MountHostFolder("trace");

        mountedBuilder.ShouldNotBeNull();
    }

    [Test]
    public async Task Startup_honours_cancellation_before_creating_resources()
    {
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();
        InspectableDockerHelper helper = new();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            helper.StartContainersForScenarioRun(
                "cancelled-startup",
                DockerServices.None,
                cancellationTokenSource.Token));
    }

    [Test]
    public async Task Projection_creation_failure_is_propagated()
    {
        string path = Path.Combine(Path.GetTempPath(), $"projection-{Guid.NewGuid():N}.js");
        await File.WriteAllTextAsync(path, "fromAll().when({});");

        try
        {
            FailingProjectionDockerHelper helper = new(new FileInfo(path));

            await Should.ThrowAsync<InvalidOperationException>(
                () => helper.LoadProjectionsForTesting());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
