using Imposter.Abstractions;
using Microsoft.Extensions.Logging;
using Shared.DomainDrivenDesign.EventSourcing;
using Shared.EventStore.Aggregate;
using Shared.EventStore.EventHandling;
using Shared.EventStore.EventStore;
using Shared.EventStore.ProjectionEngine;
using Shared.EventStore.SubscriptionWorker;
using Shared.EventStore.Tests.TestObjects;

[assembly: GenerateImposter(typeof(IEventStoreContextManager))]
[assembly: GenerateImposter(typeof(IEventStoreContext))]
[assembly: GenerateImposter(typeof(IAggregateRepositoryResolver))]
[assembly: GenerateImposter(typeof(IDomainEventHandlerResolver))]
[assembly: GenerateImposter(typeof(ISubscriptionRepository))]
[assembly: GenerateImposter(typeof(IDomainEventHandler))]
[assembly: GenerateImposter(typeof(ILogger))]
