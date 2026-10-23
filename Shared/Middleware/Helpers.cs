#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Shared.Middleware;

public static class Helpers
{
    public static Boolean IsHealthCheckRequest(String url) => url.EndsWith("/health", StringComparison.OrdinalIgnoreCase);

    [ExcludeFromCodeCoverage]
    public static void LogMessage(String url, StringBuilder message, LogLevel logLevel)
    {
        String eventType = IsHealthCheckRequest(url) ? "HEALTH_CHECK" : "HTTP";
        Dictionary<String, Object?> fields = new() { ["eventType"] = eventType, ["message"] = message.ToString() };
        switch (logLevel)
        {
            case LogLevel.Trace: Logger.Logger.LogTrace("{eventType} {message}", eventType, message.ToString()); break;
            case LogLevel.Debug: Logger.Logger.LogDebug("{eventType} {message}", eventType, message.ToString()); break;
            case LogLevel.Warning: Logger.Logger.LogWarning("{eventType} {message}", eventType, message.ToString()); break;
            case LogLevel.Error: Logger.Logger.LogError("{eventType} {message}", eventType, message.ToString()); break;
            case LogLevel.Critical: Logger.Logger.LogCritical("{eventType} {message}", eventType, message.ToString()); break;
            default: Logger.Logger.LogInformation("{eventType} {message}", eventType, message.ToString()); break;
        }
    }
}
