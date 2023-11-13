namespace Shared.TraceHandler;

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

[ExcludeFromCodeCoverage]
public class TraceEventHandlerArgs : EventArgs
{
    public TraceEventHandlerArgs(string traceMessage, LogLevel logLevel)
    {
        this.TraceMessage = traceMessage;
        this.LogLevel = logLevel;
    }

    public string TraceMessage { get; private set; }

    public LogLevel LogLevel
    {
        get;
        private set;
    }
}