namespace Shared.Middleware;

using System;
using System.Text;

public static class Helpers
{
    public static Boolean IsHealthCheckRequest(String url) => url.EndsWith("/health");

    public static void LogMessage(String url,
                                  StringBuilder message)
    {
        if (Helpers.IsHealthCheckRequest(url))
        {
            // TODO: new logger method??
            Logger.Logger.LogInformation($"HEALTH_CHECK | {message}");
        }
        else
        {
            Logger.Logger.LogInformation($"{message}");
        }
    }
}