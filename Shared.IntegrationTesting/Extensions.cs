namespace Shared.IntegrationTesting;

using System;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Model.Builders;

public static class Extensions
{
    public static ContainerBuilder MountHostFolder(this ContainerBuilder containerBuilder,
                                                   DockerEnginePlatform dockerEnginePlatform,
                                                   String hostTraceFolder, 
                                                   String containerPath = null)
    {
        if (containerPath == null) {
            containerPath = dockerEnginePlatform == DockerEnginePlatform.Windows ? "C:\\home\\txnproc\\trace" : "/home/txnproc/trace";
        }

        if (!String.IsNullOrEmpty(hostTraceFolder)) {
            containerBuilder = containerBuilder.Mount(hostTraceFolder, containerPath, MountType.ReadWrite);
        }

        return containerBuilder;
    }

    public static ContainerBuilder SetDockerCredentials(this ContainerBuilder containerBuilder,
                                                        (String url,String username,String password)? dockerCredentials)
    {
        if (dockerCredentials.HasValue) 
        {
            containerBuilder = containerBuilder.WithCredential(dockerCredentials.Value.url,
                                                               dockerCredentials.Value.username,
                                                               dockerCredentials.Value.password);
        }

        return containerBuilder;
    }

    public static ContainerBuilder UseImageDetails(this ContainerBuilder containerBuilder,
                                                   (String imageName, Boolean useLatestImage) imageDetails) {
        return containerBuilder.UseImage(imageDetails.imageName, imageDetails.useLatestImage);
    }
}