namespace Shared.TraceHandler;

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

[ExcludeFromCodeCoverage]
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