using Imposter.Abstractions;
using Microsoft.Extensions.Logging;
using TransactionProcessing.Shared.DomainDrivenDesign.EventSourcing;
using TransactionProcessing.Shared.EventStore.Aggregate;
using TransactionProcessing.Shared.EventStore.EventHandling;
using TransactionProcessing.Shared.EventStore.EventStore;
using TransactionProcessing.Shared.EventStore.ProjectionEngine;
using TransactionProcessing.Shared.EventStore.SubscriptionWorker;
using TransactionProcessing.Shared.EventStore.Tests.TestObjects;

[assembly: GenerateImposter(typeof(IEventStoreContextManager))]
[assembly: GenerateImposter(typeof(IEventStoreContext))]
[assembly: GenerateImposter(typeof(IAggregateRepositoryResolver))]
[assembly: GenerateImposter(typeof(IDomainEventHandlerResolver))]
[assembly: GenerateImposter(typeof(ISubscriptionRepository))]
[assembly: GenerateImposter(typeof(IDomainEventHandler))]
[assembly: GenerateImposter(typeof(ILogger))]
