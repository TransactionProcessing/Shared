using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Shared.IntegrationTesting.TestContainers;

namespace Shared.IntegrationTesting.TestContainers;

using System;

public static class Extensions
{
    public static ContainerBuilder MountHostFolder(this ContainerBuilder containerBuilder,
                                                   DockerEnginePlatform dockerEnginePlatform,
                                                   String hostTraceFolder,
                                                   String containerPath = null)
    {
        if (containerPath == null)
        {
            containerPath = dockerEnginePlatform == DockerEnginePlatform.Windows ? "C:\\home\\txnproc\\trace" : "/home/txnproc/trace";
        }

        if (!String.IsNullOrEmpty(hostTraceFolder))
        {
            containerBuilder = containerBuilder.WithBindMount(hostTraceFolder, containerPath, AccessMode.ReadWrite);
        }

        return containerBuilder;
    }
}