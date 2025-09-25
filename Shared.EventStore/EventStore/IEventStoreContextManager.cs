using System;
using System.Text;

namespace Shared.EventStore.EventStore;

using System.Diagnostics;

public interface IEventStoreContextManager
{
    event TraceHandler TraceGenerated;
    
    IEventStoreContext GetEventStoreContext(String connectionStringIdentifier);
}