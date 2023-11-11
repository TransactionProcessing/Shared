using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    using Microsoft.Extensions.Logging;

    public interface ITraceHandler{
        event EventHandler<TraceEventHandlerArgs> TraceGenerated;
        event EventHandler<ErrorEventHandlerArgs> ErrorThrown;
    }

    public class StandardTraceHandler : ITraceHandler{
        public event EventHandler<TraceEventHandlerArgs> TraceGenerated;
        public event EventHandler<ErrorEventHandlerArgs> ErrorThrown;

        public void LogTrace(String traceMessage){
            if (TraceGenerated != null)
                this.TraceGenerated.Invoke(this, new TraceEventHandlerArgs(traceMessage, LogLevel.Information) );
        }

        public void LogWarning(String traceMessage)
        {
            if (TraceGenerated != null)
                this.TraceGenerated.Invoke(this, new TraceEventHandlerArgs(traceMessage, LogLevel.Warning));
        }

        public void LogError(Exception exception)
        {
            if (ErrorThrown!= null)
                this.ErrorThrown.Invoke(this, new ErrorEventHandlerArgs(exception, LogLevel.Error));
        }
    }

    public class TraceEventHandlerArgs : EventArgs{
        public TraceEventHandlerArgs(String traceMessage, LogLevel logLevel){
            this.TraceMessage = traceMessage;
            this.LogLevel = logLevel;
        }

        public String TraceMessage { get; private set; }

        public LogLevel LogLevel
        {
            get;
            private set;
        }
    }

    public class ErrorEventHandlerArgs : EventArgs
    {
        public ErrorEventHandlerArgs(Exception exception, LogLevel logLevel)
        {
            this.Exception = exception;
            this.LogLevel = logLevel;
        }

        public Exception Exception { get; private set; }
        public LogLevel LogLevel
        {
            get;
            private set;
        }
    }

}


