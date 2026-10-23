#nullable enable
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Shared.Middleware;

public static class Helpers
{
    private const String MessageTemplate = "{eventType} {message}";

    public static Boolean IsHealthCheckRequest(String url) => url.EndsWith("/health", StringComparison.OrdinalIgnoreCase);

    [ExcludeFromCodeCoverage]
    public static void LogMessage(String url, StringBuilder message, LogLevel logLevel)
    {
        String eventType = IsHealthCheckRequest(url) ? "HEALTH_CHECK" : "HTTP";
        String messageText = message.ToString();
        switch (logLevel)
        {
            case LogLevel.Trace: Logger.Logger.LogTrace(MessageTemplate, eventType, messageText); break;
            case LogLevel.Debug: Logger.Logger.LogDebug(MessageTemplate, eventType, messageText); break;
            case LogLevel.Warning: Logger.Logger.LogWarning(MessageTemplate, eventType, messageText); break;
            case LogLevel.Error: Logger.Logger.LogError(MessageTemplate, eventType, messageText); break;
            case LogLevel.Critical: Logger.Logger.LogCritical(MessageTemplate, eventType, messageText); break;
            default: Logger.Logger.LogInformation(MessageTemplate, eventType, messageText); break;
        }
    }
}
