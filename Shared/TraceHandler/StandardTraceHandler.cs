namespace Shared.TraceHandler;

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

[ExcludeFromCodeCoverage]
public class StandardTraceHandler : ITraceHandler
{
    public event EventHandler<TraceEventHandlerArgs> TraceGenerated;
    public event EventHandler<ErrorEventHandlerArgs> ErrorThrown;

    public void LogTrace(string traceMessage)
    {
        if (this.TraceGenerated != null)
            this.TraceGenerated.Invoke(this, new TraceEventHandlerArgs(traceMessage, LogLevel.Information));
    }

    public void LogWarning(string traceMessage)
    {
        if (this.TraceGenerated != null)
            this.TraceGenerated.Invoke(this, new TraceEventHandlerArgs(traceMessage, LogLevel.Warning));
    }

    public void LogError(Exception exception)
    {
        if (this.ErrorThrown != null)
            this.ErrorThrown.Invoke(this, new ErrorEventHandlerArgs(exception, LogLevel.Error));
    }
}