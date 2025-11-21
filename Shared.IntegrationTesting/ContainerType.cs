using System;

namespace Shared.IntegrationTesting;

public enum ContainerType
{
    SqlServer,
    EventStore,
    MessagingService,
    SecurityService,
    CallbackHandler,
    TestHost,
    TransactionProcessor,
    FileProcessor,
    TransactionProcessorAcl,
    NotSet
}

[Flags]
public enum DockerServices
{
    SqlServer = 1,
    EventStore = 2,
    MessagingService = 4,
    SecurityService = 8,
    CallbackHandler = 16,
    TestHost = 32,
    TransactionProcessor = 64,
    FileProcessor = 128,
    TransactionProcessorAcl = 256
}

public enum DockerEnginePlatform
{
    Unknown,
    Linux,
    Windows
}