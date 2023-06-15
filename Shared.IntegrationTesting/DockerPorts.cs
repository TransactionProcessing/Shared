namespace Shared.IntegrationTesting;

using System;

public static class DockerPorts
{
    public const Int32 FileProcessorDockerPort = 5009;

    public const Int32 CallbackHandlerDockerPort = 5010;

    public const Int32 EstateManagementDockerPort = 5000;

    public const Int32 EventStoreHttpDockerPort = 2113;

    public const Int32 EventStoreTcpDockerPort = 1113;

    public const Int32 MessagingServiceDockerPort = 5006;

    public const Int32 SecurityServiceDockerPort = 5001;

    public const Int32 TestHostPort = 9000;

    public const Int32 TransactionProcessorAclDockerPort = 5003;

    public const Int32 TransactionProcessorDockerPort = 5002;
}