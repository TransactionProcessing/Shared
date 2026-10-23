namespace Shared.Logger;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using NLog;
using Shared.Logger.TennantContext;

[ExcludeFromCodeCoverage]
public static class Logger
{
    private static ILogger? LoggerObject;

    public static Boolean IsInitialised { get; set; }

    public static void Initialise(NLog.Logger loggerObject,
                                  String fileName)
    {
        NlogLogger logger = new();
        logger.Initialise(loggerObject, fileName);
        Logger.Initialise(logger);
    }

    public static void Initialise(ILogger loggerObject)
    {
        Logger.LoggerObject = loggerObject ?? throw new ArgumentNullException(nameof(loggerObject));
        Logger.IsInitialised = true;
    }

    public static void LogTrace(String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        String sanitizedTemplate = Logger.SanitizeForLog(messageTemplate);
        Logger.Write((logger, sanitizedArgs) => logger.LogTrace(sanitizedTemplate, sanitizedArgs), true, args);
    }

    public static void LogDebug(String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        String sanitizedTemplate = Logger.SanitizeForLog(messageTemplate);
        Logger.Write((logger, sanitizedArgs) => logger.LogDebug(sanitizedTemplate, sanitizedArgs), true, args);
    }

    public static void LogInformation(String messageTemplate,
                                      params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        String sanitizedTemplate = Logger.SanitizeForLog(messageTemplate);
        Logger.Write((logger, sanitizedArgs) => logger.LogInformation(sanitizedTemplate, sanitizedArgs), true, args);
    }

    public static void LogWarning(String messageTemplate,
                                  params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        String sanitizedTemplate = Logger.SanitizeForLog(messageTemplate);
        Logger.Write((logger, sanitizedArgs) => logger.LogWarning(sanitizedTemplate, sanitizedArgs), true, args);
    }

    public static void LogError(String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write((logger, sanitizedArgs) => logger.LogError(messageTemplate, sanitizedArgs), true, args);
    }

    public static void LogError(Exception exception,
                                String messageTemplate,
                                params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write((logger, sanitizedArgs) => logger.LogError(exception, messageTemplate, sanitizedArgs), true, args);
    }

    public static void LogCritical(String messageTemplate,
                                   params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write((logger, sanitizedArgs) => logger.LogCritical(messageTemplate, sanitizedArgs), true, args);
    }

    public static void LogCritical(Exception exception,
                                   String messageTemplate,
                                   params Object?[] args)
    {
        Logger.ValidateLoggerObject();
        Logger.Write((logger, sanitizedArgs) => logger.LogCritical(exception, messageTemplate, sanitizedArgs), true, args);
    }

    // Compatibility overloads below remain available for existing callers and can be removed only in a breaking release.
    [Obsolete("Use LogCritical(String messageTemplate, params Object?[] args) or the exception-aware structured overload.")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogCritical(Exception exception)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogCritical(exception));
    }

    [Obsolete("Use LogCritical(String messageTemplate, params Object?[] args) or the exception-aware structured overload.")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogCritical(String message,
                                   Exception exception)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogCritical(message, exception));
    }

    [Obsolete("Use LogDebug(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogDebug(String message)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogDebug(message));
    }

    [Obsolete("Use LogError(Exception exception, String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogError(Exception exception)
    {
        Logger.ValidateLoggerObject();
        Logger.Write(logger => logger.LogError(exception));
    }

    [Obsolete("Use LogError(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogError(String message)
    {
        Logger.ValidateLoggerObject();
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogError(sanitizedMessage));
    }

    [Obsolete("Use LogError(Exception exception, String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogError(String message,
                                Exception exception)
    {
        Logger.ValidateLoggerObject();
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogError(sanitizedMessage, exception));
    }

    [Obsolete("Use LogInformation(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogInformation(String message)
    {
        Logger.ValidateLoggerObject();
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogInformation(sanitizedMessage));
    }

    [Obsolete("Use LogTrace(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogTrace(String message)
    {
        Logger.ValidateLoggerObject();
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogTrace(sanitizedMessage));
    }

    [Obsolete("Use LogWarning(String messageTemplate, params Object?[] args).")] // NOSONAR - retained for source compatibility until a breaking release.
    public static void LogWarning(String message)
    {
        Logger.ValidateLoggerObject();
        String sanitizedMessage = Logger.SanitizeForPlainTextLog(message);
        Logger.Write(logger => logger.LogWarning(sanitizedMessage));
    }

    private static String SanitizeForPlainTextLog(String value)
    {
        if (String.IsNullOrEmpty(value)) return value;

        Char[] sanitized = value.Where(character =>
            // Remove all control characters to prevent log forging and parser confusion.
            // Keep standard printable text only.
            !Char.IsControl(character)
        ).ToArray();

        return new String(sanitized);
    }

    private static Object?[] SanitizeLogArgs(Object?[] args) =>
        args.Select(Logger.SanitizeLogValue).ToArray();

    private static Object? SanitizeLogValue(Object? value)
    {
        if (value == null) return null;

        if (value is String text) return Logger.SanitizeForLog(text);

        if (value is IDictionary dictionary)
        {
            Dictionary<String, Object?> sanitized = new();
            foreach (DictionaryEntry entry in dictionary)
            {
                String key = Logger.SanitizeForLog(Convert.ToString(entry.Key) ?? String.Empty);
                sanitized[key] = Logger.SanitizeLogValue(entry.Value);
            }

            String json = JsonSerializer.Serialize(sanitized);
            return Logger.SanitizeForLog(json);
        }

        if (value is IEnumerable enumerable && value is not String)
        {
            Object?[] sanitizedItems = enumerable.Cast<Object?>().Select(Logger.SanitizeLogValue).ToArray();
            String json = JsonSerializer.Serialize(sanitizedItems);
            return Logger.SanitizeForLog(json);
        }

        String? normalized = Convert.ToString(value);
        return normalized == null ? null : Logger.SanitizeForLog(normalized);
    }

    private static String SanitizeForLog(String value) =>
        Logger.SanitizeForPlainTextLog(value);

    [MemberNotNull(nameof(LoggerObject))]
    private static void ValidateLoggerObject()
    {
        if (Logger.LoggerObject == null)
        {
            throw new InvalidOperationException("Logger has not been initialised");
        }
    }

    private static void Write(Action<ILogger, Object?[]?> write, Boolean isStructured=false, Object?[]? args=null)
    {
        Object?[]? sanitizedArgs = args == null ? null : Logger.SanitizeLogArgs(args);

        TenantContext tenantContext = TenantContext.CurrentTenant;
        if (tenantContext == null)
        {
            write(Logger.LoggerObject!, sanitizedArgs);
            return;
        }

        String correlationId = isStructured switch {
                                   true => tenantContext.CorrelationId.ToString(),
                                   _ => $"Correlation ID: {tenantContext.CorrelationId}"
                               };

        using (ScopeContext.PushProperty(TenantContext.KeyNameCorrelationId, correlationId))
        {
            write(Logger.LoggerObject!, sanitizedArgs);
            if (tenantContext.PerTenantLogsEnabled && tenantContext.EstateId != Guid.Empty)
            {
                using (ScopeContext.PushProperty("tenantId", $"_{tenantContext.EstateId}"))
                {
                    write(Logger.LoggerObject!, sanitizedArgs);
                }
            }
        }
    }
}
