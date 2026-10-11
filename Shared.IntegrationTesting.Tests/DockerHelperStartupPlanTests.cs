using System.Collections.Generic;
using System.Linq;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using Shared.IntegrationTesting;
using Shared.IntegrationTesting.TestContainers;
using Shouldly;

namespace Shared.IntegrationTesting.Tests;

public class DockerHelperStartupPlanTests
{
    private sealed class InspectableDockerHelper : TestDockerHelper
    {
        public void UseLinuxPlatform()
        {
            this.DockerPlatform = DockerEnginePlatform.Linux;
        }

        public IReadOnlyList<IReadOnlyList<DockerServices>> GetStartupGroupsForTesting()
        {
            return base.GetStartupGroups();
        }

        public static IReadOnlyList<(DockerServices service, IContainer container)> GetRetainedSharedContainers(
            IEnumerable<(DockerServices service, IContainer container)> containers,
            DockerServices sharedDockerServices)
        {
            return DockerHelper.RetainSharedContainers(containers, sharedDockerServices);
        }
    }

    [Test]
    public void StartupGroups_place_foundation_services_first_and_estate_management_ui_last_on_linux()
    {
        InspectableDockerHelper helper = new();
        helper.UseLinuxPlatform();

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
}
