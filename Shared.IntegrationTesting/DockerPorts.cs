namespace Shared.IntegrationTesting;

using System;

public static class DockerPorts
{
    public static readonly Int32 FileProcessorDockerPort = 5009;

    public static readonly Int32 CallbackHandlerDockerPort = 5010;
    
    public static readonly Int32 EventStoreHttpDockerPort = 2113;

    public static readonly Int32 EventStoreTcpDockerPort = 1113;

    public static readonly Int32 MessagingServiceDockerPort = 5006;

    public static readonly Int32 SecurityServiceDockerPort = 5001;

    public static readonly Int32 TestHostPort = 9000;

    public static readonly Int32 TransactionProcessorAclDockerPort = 5003;

    public static readonly Int32 TransactionProcessorDockerPort = 5002;

    public static readonly Int32 KeyCloakDockerPort = 8080;
}