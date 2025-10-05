using System.Diagnostics.CodeAnalysis;

namespace Shared.EventStore.EventStore;

using Microsoft.Extensions.Logging;
using Shared.General;
using System;
using System.Collections.Generic;
using System.Threading;

public class EventStoreContextManager : IEventStoreContextManager
{
    private readonly IEventStoreContext Context;

    private readonly Func<String, IEventStoreContext> EventStoreContextFunc;

    private readonly Dictionary<String, IEventStoreContext> EventStoreContexts;

    private readonly Object padlock = new();
    
    public EventStoreContextManager(Func<String, IEventStoreContext> eventStoreContextFunc)
    {
        this.EventStoreContexts = new();
        this.EventStoreContextFunc = eventStoreContextFunc;
    }
    
    public event TraceHandler TraceGenerated;

    public IEventStoreContext GetEventStoreContext(String connectionStringIdentifier)
    {
        this.WriteTrace($"No resolved context found, about to resolve one using connectionIdentifier {connectionStringIdentifier}");

        if (this.EventStoreContexts.TryGetValue(connectionStringIdentifier, out IEventStoreContext context))
        {
            return context;
        }

        this.WriteTrace($"Creating a new EventStoreContext for connectionIdentifier {connectionStringIdentifier}");

        lock(this.padlock)
        {
            if (!this.EventStoreContexts.ContainsKey(connectionStringIdentifier))
            {
                // This will need to now look up the ES Connection string from persistence
                String connectionString = ConfigurationReader.GetValue("EventStoreSettings", connectionStringIdentifier);

                this.WriteTrace($"Connection String is {connectionString}");

                IEventStoreContext eventStoreContext = this.EventStoreContextFunc(connectionString);

                this.EventStoreContexts.Add(connectionStringIdentifier, eventStoreContext);
            }

            return this.EventStoreContexts[connectionStringIdentifier];
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
}