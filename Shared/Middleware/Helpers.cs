using Microsoft.Extensions.Logging;

namespace Shared.Middleware;

using System;
using System.Text;

public static class Helpers
{
    public static Boolean IsHealthCheckRequest(String url) => url.EndsWith("/health");
    
    public static void LogMessage(String url, StringBuilder message, LogLevel logLevel)
    {
        String logMessage = Helpers.IsHealthCheckRequest(url) switch
        {
            true => $"HEALTH_CHECK | {message}",
            _ => message.ToString()
        };

        Action log = logLevel switch
        {
            LogLevel.Trace => () => Logger.Logger.LogTrace(logMessage),
            LogLevel.Debug => () => Logger.Logger.LogDebug(logMessage),
            LogLevel.Information => () => Logger.Logger.LogInformation(logMessage),
            LogLevel.Warning => () => Logger.Logger.LogWarning(logMessage),
            _ => () => Logger.Logger.LogInformation(logMessage)
        };

        log();
    }
}