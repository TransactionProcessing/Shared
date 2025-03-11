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