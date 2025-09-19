using System.Diagnostics.CodeAnalysis;

namespace Shared.EventStore.EventStore;

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Repositories;

/// <summary>
/// 
/// </summary>
/// <seealso cref="Shared.EventStore.EventStore.IEventStoreContextManager" />
public class EventStoreContextManager : IEventStoreContextManager
{
    #region Fields

    /// <summary>
    /// The connection string configuration repository
    /// </summary>
    private readonly IConnectionStringConfigurationRepository ConnectionStringConfigurationRepository;

    /// <summary>
    /// The context
    /// </summary>
    private readonly IEventStoreContext Context;

    /// <summary>
    /// The event store context function
    /// </summary>
    private readonly Func<String, IEventStoreContext> EventStoreContextFunc;

    /// <summary>
    /// The event store contexts
    /// </summary>
    private readonly Dictionary<String, IEventStoreContext> EventStoreContexts;

    //TODO static?
    /// <summary>
    /// The padlock
    /// </summary>
    private readonly Object padlock = new();

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreContextManager" /> class.
    /// </summary>
    /// <param name="eventStoreContextFunc">The event store context function.</param>
    /// <param name="connectionStringConfigurationRepository">The connection string configuration repository.</param>
    public EventStoreContextManager(Func<String, IEventStoreContext> eventStoreContextFunc,
                                    IConnectionStringConfigurationRepository connectionStringConfigurationRepository)
    {
        this.EventStoreContexts = new();
        this.EventStoreContextFunc = eventStoreContextFunc;
        this.ConnectionStringConfigurationRepository = connectionStringConfigurationRepository;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreContextManager" /> class.
    /// </summary>
    /// <param name="eventStoreContext">The event store context.</param>
    public EventStoreContextManager(IEventStoreContext eventStoreContext)
    {
        this.Context = eventStoreContext;
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when [trace generated].
    /// </summary>
    public event TraceHandler TraceGenerated;

    #endregion

    #region Methods

    public IEventStoreContext GetEventStoreContext(String connectionIdentifier) => this.GetEventStoreContext(connectionIdentifier, "EventStoreConnectionString");

    public IEventStoreContext GetEventStoreContext(String connectionIdentifier, String connectionStringIdentifier)
    {
        if (this.Context != null)
        {
            return this.Context;
        }

        this.WriteTrace($"No resolved context found, about to resolve one using connectionIdentifier {connectionIdentifier}");

        if (this.EventStoreContexts.TryGetValue(connectionIdentifier, out IEventStoreContext context))
        {
            return context;
        }

        this.WriteTrace($"Creating a new EventStoreContext for connectionIdentifier {connectionIdentifier}");

        lock(this.padlock)
        {
            if (!this.EventStoreContexts.ContainsKey(connectionIdentifier))
            {
                // This will need to now look up the ES Connection string from persistence
                String connectionString = this.ConnectionStringConfigurationRepository
                    .GetConnectionString(connectionIdentifier, connectionStringIdentifier, CancellationToken.None).Result;

                this.WriteTrace($"Connection String is {connectionString}");

                IEventStoreContext eventStoreContext = this.EventStoreContextFunc(connectionString);

                this.EventStoreContexts.Add(connectionIdentifier, eventStoreContext);
            }

            return this.EventStoreContexts[connectionIdentifier];
        }
    }

    [ExcludeFromCodeCoverage]
    private void WriteTrace(String trace)
    {
        if (this.TraceGenerated != null)
        {
            this.TraceGenerated(trace, LogLevel.Information);
        }
    }

    #endregion
}