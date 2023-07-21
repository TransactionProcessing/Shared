namespace Shared.EventStore.ProjectionEngine;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;
using EventHandling;
using General;

[ExcludeFromCodeCoverage]
public class EventHandler : IDomainEventHandler{
    #region Fields

    public static Dictionary<String, Type> StateTypes;

    private readonly Func<String, IDomainEventHandler> Resolver;

    #endregion

    #region Constructors

    public EventHandler(Func<String, IDomainEventHandler> resolver){
        this.Resolver = resolver;
        List<Type> subclassTypes = Assembly.GetAssembly(typeof(State))?.GetTypes().Where(t => t.IsSubclassOf(typeof(State))).ToList();

        if (subclassTypes != null){
            EventHandler.StateTypes = subclassTypes.ToDictionary(x => x.Name, x => x);
        }
    }

    #endregion

    #region Methods

    public async Task Handle(IDomainEvent domainEvent,
                             CancellationToken cancellationToken){
        // Lookup the event type in the config
        String handlerType = ConfigurationReader.GetValue("AppSettings:EventStateConfig", domainEvent.GetType().Name);

        IDomainEventHandler handler = this.Resolver(handlerType);

        await handler.Handle(domainEvent, cancellationToken);
    }

    #endregion
}