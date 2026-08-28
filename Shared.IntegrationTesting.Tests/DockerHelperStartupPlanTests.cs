using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shared.IntegrationTesting;
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
    }

    [Test]
    public void StartupGroups_place_foundation_services_first_and_estate_management_ui_last()
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
}
