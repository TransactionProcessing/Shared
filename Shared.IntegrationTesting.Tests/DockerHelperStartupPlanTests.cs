using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EventStore.Client;
using KurrentDB.Client;
using NLog;
using NUnit.Framework;
using Shared.IntegrationTesting;
using Shared.IntegrationTesting.TestContainers;
using Shared.Logger;
using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.IntegrationTesting.Tests;

public class DockerHelperStartupPlanTests
{
    private sealed class InspectableDockerHelper : TestDockerHelper
    {
        public string GetConnectionStringForTesting(string databaseName)
        {
            return this.SetConnectionString(databaseName);
        }

        public static string GetScenarioDatabaseNameForTesting(
            string databaseName,
            Guid testId,
            bool sharedSqlServer)
        {
            return BaseDockerHelper.GetScenarioDatabaseName(databaseName, testId, sharedSqlServer);
        }

        public static DockerServices GetServicesForStartupForTesting(
            DockerServices requiredServices,
            bool hasSharedSqlServer)
        {
            return BaseDockerHelper.GetServicesForStartup(requiredServices, hasSharedSqlServer);
        }

        public static string GetHostTraceFolderPathForTesting(
            string scenarioName,
            DateTimeOffset runStartedAt,
            bool isCI,
            bool isWindows)
        {
            return BuildHostTraceFolderPath(scenarioName, runStartedAt, isCI, isWindows);
        }

        public IReadOnlyList<IReadOnlyList<DockerServices>> GetStartupGroupsForTesting()
        {
            return base.GetStartupGroups();
        }

        public HttpClient GetKurrentDbHealthCheckClientForTesting()
        {
            return BaseDockerHelper.KurrentDbHealthCheckClient;
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
            KurrentDBProjectionManagementClient projectionClient,
            string projectionName,
            string projection,
            CancellationToken cancellationToken)
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
            [DockerServices.KurrentDb],
            [DockerServices.SubscriptionService],
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
            (DockerServices.KurrentDb, null!)
        ];

        IReadOnlyList<(DockerServices service, IContainer container)> remaining =
            InspectableDockerHelper.GetRetainedSharedContainers(containers, DockerServices.SqlServer);

        remaining.Select(container => container.service).ShouldBe([DockerServices.SqlServer]);
    }

    [Test]
    public void Shared_SQL_server_is_not_started_by_a_scenario_helper()
    {
        DockerServices services = InspectableDockerHelper.GetServicesForStartupForTesting(
            DockerServices.SqlServer | DockerServices.KurrentDb,
            hasSharedSqlServer: true);

        services.ShouldBe(DockerServices.KurrentDb);
    }

    [Test]
    public void Scenario_helper_still_starts_SQL_server_without_a_shared_fixture()
    {
        DockerServices services = InspectableDockerHelper.GetServicesForStartupForTesting(
            DockerServices.SqlServer | DockerServices.KurrentDb,
            hasSharedSqlServer: false);

        services.ShouldBe(DockerServices.SqlServer | DockerServices.KurrentDb);
    }

    [Test]
    public void Scenario_database_names_include_the_scenario_id()
    {
        Guid firstTestId = Guid.NewGuid();
        Guid secondTestId = Guid.NewGuid();

        string firstDatabaseName = InspectableDockerHelper.GetScenarioDatabaseNameForTesting(
            "TransactionProcessorReadModel",
            firstTestId,
            sharedSqlServer: true);
        string secondDatabaseName = InspectableDockerHelper.GetScenarioDatabaseNameForTesting(
            "TransactionProcessorReadModel",
            secondTestId,
            sharedSqlServer: true);

        firstDatabaseName.ShouldBe($"TransactionProcessorReadModel-{firstTestId:N}");
        secondDatabaseName.ShouldBe($"TransactionProcessorReadModel-{secondTestId:N}");
        firstDatabaseName.ShouldNotBe(secondDatabaseName);
    }

    [Test]
    public void Existing_scenario_scoped_database_names_are_not_suffixed_twice()
    {
        Guid testId = Guid.NewGuid();

        string databaseName = InspectableDockerHelper.GetScenarioDatabaseNameForTesting(
            $"PersistedGrantStore-{testId}",
            testId,
            sharedSqlServer: true);

        databaseName.ShouldBe($"PersistedGrantStore-{testId:N}");
    }

    [Test]
    public void Master_database_name_is_not_scenario_scoped()
    {
        InspectableDockerHelper helper = new() { TestId = Guid.NewGuid() };

        helper.GetConnectionStringForTesting("master").ShouldContain("database=master;");
    }

    [Test]
    public void Host_trace_folder_contains_the_test_run_timestamp()
    {
        DateTimeOffset firstRunStartedAt = new(2026, 10, 19, 16, 37, 0, TimeSpan.Zero);
        DateTimeOffset secondRunStartedAt = new(2026, 10, 19, 16, 38, 0, TimeSpan.Zero);
        string firstRunPath = InspectableDockerHelper.GetHostTraceFolderPathForTesting(
            "MyScenario",
            firstRunStartedAt,
            isCI: false,
            isWindows: true);
        string secondRunPath = InspectableDockerHelper.GetHostTraceFolderPathForTesting(
            "MyScenario",
            secondRunStartedAt,
            isCI: false,
            isWindows: true);

        firstRunPath.ShouldEndWith(Path.Combine("MyScenario", "20261019", "1637"));
        secondRunPath.ShouldEndWith(Path.Combine("MyScenario", "20261019", "1638"));
        firstRunPath.ShouldNotBe(secondRunPath);
    }

    [Test]
    public void Local_host_trace_folder_uses_the_integration_tests_root()
    {
        string windowsLocalPath = InspectableDockerHelper.GetHostTraceFolderPathForTesting(
            "MyScenario",
            new DateTimeOffset(2026, 10, 19, 16, 37, 0, TimeSpan.Zero),
            isCI: false,
            isWindows: true);
        string linuxLocalPath = InspectableDockerHelper.GetHostTraceFolderPathForTesting(
            "MyScenario",
            new DateTimeOffset(2026, 10, 19, 16, 37, 0, TimeSpan.Zero),
            isCI: false,
            isWindows: false);
        string windowsCiPath = InspectableDockerHelper.GetHostTraceFolderPathForTesting(
            "MyScenario",
            new DateTimeOffset(2026, 10, 19, 16, 37, 0, TimeSpan.Zero),
            isCI: true,
            isWindows: true);
        string linuxCiPath = InspectableDockerHelper.GetHostTraceFolderPathForTesting(
            "MyScenario",
            new DateTimeOffset(2026, 10, 19, 16, 37, 0, TimeSpan.Zero),
            isCI: true,
            isWindows: false);

        windowsLocalPath.ShouldBe(Path.Combine(@"C:\home\integrationtests", "MyScenario", "20261019", "1637"));
        linuxLocalPath.Replace('\\', '/').ShouldBe("/home/integrationtests/MyScenario/20261019/1637");
        windowsCiPath.ShouldBe(Path.Combine(@"C:\Users\runneradmin\trace", "MyScenario", "20261019", "1637"));
        linuxCiPath.Replace('\\', '/').ShouldBe("/home/runner/trace/MyScenario/20261019/1637");
    }

    [Test]
    public void EventStore_health_checks_reuse_their_http_client()
    {
        InspectableDockerHelper helper = new();

        HttpClient firstClient = helper.GetKurrentDbHealthCheckClientForTesting();
        HttpClient secondClient = helper.GetKurrentDbHealthCheckClientForTesting();

        secondClient.ShouldBeSameAs(firstClient);
    }

    [Test]
    public void Host_folder_mount_does_not_require_docker_platform()
    {
        ContainerBuilder builder = new("hello-world");

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
