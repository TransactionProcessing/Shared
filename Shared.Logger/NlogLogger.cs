namespace Shared.Logger;

using System;
using System.Diagnostics.CodeAnalysis;
using NLog;

/// <summary>
/// Writes structured events through NLog.
/// </summary>
[ExcludeFromCodeCoverage]
public class NlogLogger : ILogger
{
    private String? FileName;

    private NLog.Logger? LoggerObject;

    public Boolean IsInitialised { get; set; }

    public void Initialise(NLog.Logger loggerObject,
                           String fileName)
    {
        this.LoggerObject = loggerObject ?? throw new ArgumentNullException(nameof(loggerObject));
        this.FileName = fileName;
        this.IsInitialised = true;
    }

    public void LogCritical(String message) => this.LogMessage(LogLevel.Fatal, message, null, Array.Empty<Object?>());

    public void LogCritical(Exception exception) =>
        this.LogMessage(LogLevel.Fatal, exception?.Message ?? String.Empty, exception, Array.Empty<Object?>());

    public void LogCritical(String message,
                            Exception exception) =>
        this.LogMessage(LogLevel.Fatal, message, exception, Array.Empty<Object?>());

    public void LogCritical(String messageTemplate,
                            params Object?[] args) =>
        this.LogMessage(LogLevel.Fatal, messageTemplate, null, args);

    public void LogCritical(Exception exception,
                            String messageTemplate,
                            params Object?[] args) =>
        this.LogMessage(LogLevel.Fatal, messageTemplate, exception, args);

    public void LogDebug(String message) =>
        this.LogMessage(LogLevel.Debug, message, null, Array.Empty<Object?>());

    public void LogDebug(String messageTemplate,
                         params Object?[] args) =>
        this.LogMessage(LogLevel.Debug, messageTemplate, null, args);

    public void LogError(Exception exception) =>
        this.LogMessage(LogLevel.Error, exception?.Message ?? String.Empty, exception, Array.Empty<Object?>());

    public void LogError(String message) =>
        this.LogMessage(LogLevel.Error, message, null, Array.Empty<Object?>());

    public void LogError(String message,
                         Exception exception) =>
        this.LogMessage(LogLevel.Error, message, exception, Array.Empty<Object?>());

    public void LogError(String messageTemplate,
                         params Object?[] args) =>
        this.LogMessage(LogLevel.Error, messageTemplate, null, args);

    public void LogError(Exception exception,
                         String messageTemplate,
                         params Object?[] args) =>
        this.LogMessage(LogLevel.Error, messageTemplate, exception, args);

    public void LogInformation(String message) =>
        this.LogMessage(LogLevel.Info, message, null, Array.Empty<Object?>());

    public void LogInformation(String messageTemplate,
                               params Object?[] args) =>
        this.LogMessage(LogLevel.Info, messageTemplate, null, args);

    public void LogTrace(String message) =>
        this.LogMessage(LogLevel.Trace, message, null, Array.Empty<Object?>());

    public void LogTrace(String messageTemplate,
                         params Object?[] args) =>
        this.LogMessage(LogLevel.Trace, messageTemplate, null, args);

    public void LogWarning(String message) =>
        this.LogMessage(LogLevel.Warn, message, null, Array.Empty<Object?>());

    public void LogWarning(String messageTemplate,
                           params Object?[] args) =>
        this.LogMessage(LogLevel.Warn, messageTemplate, null, args);

    private void LogMessage(LogLevel logLevel,
                            String messageTemplate,
                            Exception? exception,
                            Object?[] args)
    {
        if (this.LoggerObject == null)
        {
            throw new InvalidOperationException("LoggerObject has not been set");
        }

        var properties = LoggerEventProperties.Create(logLevel.Name);
        LogEventInfo eventInfo = new(logLevel,
                                     this.LoggerObject.Name,
                                     null,
                                     messageTemplate,
                                     args,
                                     exception);
        eventInfo.TimeStamp = ((DateTimeOffset)properties["timestampUtc"]!).UtcDateTime;
        eventInfo.Properties["FileName"] = this.FileName;
        eventInfo.Properties["loggerName"] = this.LoggerObject.Name;
        eventInfo.Properties["messageTemplate"] = messageTemplate;

        foreach (var property in properties)
        {
            eventInfo.Properties[property.Key] = property.Value;
        }

        this.LoggerObject.Log(eventInfo);
    }
}
