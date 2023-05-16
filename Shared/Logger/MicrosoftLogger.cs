namespace Shared.Logger
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.Extensions.Logging;

    [ExcludeFromCodeCoverage]
    public class MicrosoftLogger : ILogger
    {
        #region Fields

        private Microsoft.Extensions.Logging.ILogger LoggerObject;

        #endregion

        #region Properties

        public Boolean IsInitialised { get; set; }

        #endregion

        #region Methods

        public void Initialise(Microsoft.Extensions.Logging.ILogger loggerObject) {
            this.LoggerObject = loggerObject;
            this.IsInitialised = true;
        }

        public void LogCritical(Exception exception) {
            this.LogMessage(LogLevel.Critical, exception.Message, exception);
        }

        public void LogCritical(String message,
                                Exception exception) {
            this.LogMessage(LogLevel.Critical, message, exception);
        }

        public void LogDebug(String message) {
            this.LogMessage(LogLevel.Debug, message);
        }

        public void LogError(Exception exception) {
            this.LogMessage(LogLevel.Error, exception.Message, exception);
        }

        public void LogError(String message,
                             Exception exception) {
            this.LogMessage(LogLevel.Error, message, exception);
        }

        public void LogInformation(String message) {
            this.LogMessage(LogLevel.Information, message);
        }

        public void LogTrace(String message) {
            this.LogMessage(LogLevel.Trace, message);
        }

        public void LogWarning(String message) {
            this.LogMessage(LogLevel.Warning, message);
        }

        private void LogMessage(LogLevel logLevel,
                                String message,
                                Exception exception = null) {
            if (this.LoggerObject != null) {
                this.LoggerObject.Log(logLevel,
                                      new EventId(),
                                      message,
                                      exception,
                                      (state,
                                       ex) => message);
            }
            else {
                throw new InvalidOperationException("LoggerObject has not been set");
            }
        }

        #endregion
    }
}