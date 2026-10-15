using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Shared.IntegrationTesting.TestContainers;

namespace Shared.IntegrationTesting.TestContainers;

using System;
using System.IO;

public static class Extensions
{
    public static ContainerBuilder MountHostFolder(this ContainerBuilder containerBuilder, string? hostTraceFolder, string? containerPath = null)
    {
        containerPath ??= "/home/txnproc/trace";

        if (string.IsNullOrWhiteSpace(hostTraceFolder))
        {
            return containerBuilder;
        }

        if (!containerPath.StartsWith('/') || containerPath.Contains('\\'))
        {
            throw new ArgumentException("Container path must be an absolute Linux path.",nameof(containerPath));
        }

        var hostPath = Path.GetFullPath(hostTraceFolder);
        Directory.CreateDirectory(hostPath);

        var useWslPaths = string.Equals(
            Environment.GetEnvironmentVariable("TEST_DOCKER_WSL_PATHS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (OperatingSystem.IsWindows() && useWslPaths)
        {
            if (hostPath.Length < 3 ||
                !char.IsAsciiLetter(hostPath[0]) ||
                hostPath[1] != ':')
            {
                throw new ArgumentException(
                    "WSL conversion requires a Windows drive path, e.g. C:\\trace.",
                    nameof(hostTraceFolder));
            }

            var drive = char.ToLowerInvariant(hostPath[0]);
            var relativePath = hostPath[3..].Replace('\\', '/');

            hostPath = $"/mnt/{drive}/{relativePath}";
        }

        return containerBuilder.WithBindMount(hostPath,containerPath,AccessMode.ReadWrite);
    }
}
