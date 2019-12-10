using System;
using System.Text;

namespace Shared.EventStore.EventStore
{
    using System.Diagnostics;
    using DomainDrivenDesign.EventStore;

    public interface IEventStoreContextManager
    {
        IEventStoreContext GetEventStoreContext(String connectionIdentifier);
    }
}
