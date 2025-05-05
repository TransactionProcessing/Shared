using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Shared.DomainDrivenDesign.EventSourcing;

namespace Shared.EventStore.Aggregate;

public interface IAggregateRepositoryResolver
{
    IAggregateRepository<TAggregate, TEvent> Resolve<TAggregate, TEvent>() where TAggregate : Aggregate where TEvent : DomainEvent;
}

[ExcludeFromCodeCoverage]
public class AggregateRepositoryResolver : IAggregateRepositoryResolver
{
    private readonly IServiceProvider _provider;

    public AggregateRepositoryResolver(IServiceProvider provider)
    {
        this._provider = provider;
    }

    public IAggregateRepository<TAggregate, TEvent> Resolve<TAggregate, TEvent>() where TAggregate : Aggregate where TEvent : DomainEvent
    {
        Type repoType = typeof(IAggregateRepository<,>).MakeGenericType(typeof(TAggregate), typeof(TEvent));
        return (IAggregateRepository<TAggregate, TEvent>)this._provider.GetRequiredService(repoType);
    }
}