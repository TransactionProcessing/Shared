using System;
using System.Text;

namespace Shared.EventStore.EventStore
{
    using System.Diagnostics;

    public interface IEventStoreContextManager
    {
        IEventStoreContext GetEventStoreContext(String connectionIdentifier, String connectionStringIdentifier);

        IEventStoreContext GetEventStoreContext(String connectionIdentifier);
    }
}
