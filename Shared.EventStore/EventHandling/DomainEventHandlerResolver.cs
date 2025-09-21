﻿namespace Shared.EventStore.EventHandling;

using System;
using System.Collections.Generic;
using System.Linq;
using DomainDrivenDesign.EventSourcing;

public class DomainEventHandlerResolver : IDomainEventHandlerResolver
{
    #region Fields

    /// <summary>
    /// The domain event handlers
    /// </summary>
    private readonly Dictionary<String, IDomainEventHandler> DomainEventHandlers;

    /// <summary>
    /// The event handler configuration
    /// </summary>
    private readonly Dictionary<String, String[]> EventHandlerConfiguration;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventHandlerResolver" /> class.
    /// </summary>
    /// <param name="eventHandlerConfiguration">The event handler configuration.</param>
    public DomainEventHandlerResolver(Dictionary<String, String[]> eventHandlerConfiguration, Func<Type, IDomainEventHandler> createEventHandlerResolver)
    {
        this.EventHandlerConfiguration = eventHandlerConfiguration;

        this.DomainEventHandlers = new Dictionary<String, IDomainEventHandler>();
            
        IEnumerable<String> distinctHandlers = eventHandlerConfiguration.Keys;

        foreach (String handlerTypeString in distinctHandlers)
        {
            Type handlerType = Type.GetType(handlerTypeString);

            if (handlerType == null)
            {
                throw new NotSupportedException("Event handler configuration is not for a valid type");
            }

            IDomainEventHandler eventHandler = createEventHandlerResolver(handlerType);
            this.DomainEventHandlers.Add(handlerTypeString, eventHandler);
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the domain event handlers.
    /// </summary>
    /// <param name="domainEvent">The domain event.</param>
    /// <returns></returns>
    public List<IDomainEventHandler> GetDomainEventHandlers(IDomainEvent domainEvent)
    {
        // Get the type of the event passed in
        String typeString = domainEvent.GetType().Name;

        // Lookup the list
        var eventIsConfigured = this.EventHandlerConfiguration.Any(kv => kv.Value.Contains(typeString));
        if (!eventIsConfigured)
        {
            // No handlers setup, return null and let the caller decide what to do next
            return null;
        }

        List<String> handlers = this.EventHandlerConfiguration
            .Where(kv => kv.Value.Contains(typeString))
            .Select(kv => kv.Key)
            .ToList();

        List<IDomainEventHandler> handlersToReturn = new List<IDomainEventHandler>();

        foreach (String handler in handlers)
        {
            List<KeyValuePair<String, IDomainEventHandler>> foundHandlers = this.DomainEventHandlers.Where(h => h.Key == handler).ToList();

            handlersToReturn.AddRange(foundHandlers.Select(x => x.Value));
        }

        return handlersToReturn;
    }

    #endregion
}