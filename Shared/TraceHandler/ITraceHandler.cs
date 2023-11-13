using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.TraceHandler
{
    public interface ITraceHandler
    {
        event EventHandler<TraceEventHandlerArgs> TraceGenerated;
        event EventHandler<ErrorEventHandlerArgs> ErrorThrown;
    }
}


