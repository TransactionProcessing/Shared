namespace Shared.Logger
{
    using System;
    using NLog;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Shared.Logger.ILogger" />
    public class NlogLogger : ILogger
    {
        #region Fields

        private String FileName;

        private NLog.Logger LoggerObject;

        #endregion

        #region Properties

        public Boolean IsInitialised { get; set; }

        #endregion

        #region Methods

        public void Initialise(NLog.Logger loggerObject,
                               String fileName) {
            this.LoggerObject = loggerObject;
            this.FileName = fileName;
            this.IsInitialised = true;
        }

        public void LogCritical(Exception exception) {
            this.LogMessage(LogLevel.Fatal, exception.Message, exception);
        }

        public void LogCritical(String message,
                                Exception exception) {
            this.LogMessage(LogLevel.Fatal, message, exception);
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
            this.LogMessage(LogLevel.Info, message);
        }

        public void LogTrace(String message) {
            this.LogMessage(LogLevel.Trace, message);
        }

        public void LogWarning(String message) {
            this.LogMessage(LogLevel.Warn, message);
        }

        private void LogMessage(LogLevel logLevel,
                                String message,
                                Exception exception = null) {
            if (this.LoggerObject != null) {
                LogEventInfo eventInfo = new LogEventInfo(logLevel, "Logger", message);
                eventInfo.Exception = exception;
                eventInfo.Properties["FileName"] = this.FileName;

                this.LoggerObject.Log(eventInfo);
            }
            else {
                throw new InvalidOperationException("LoggerObject has not been set");
            }
        }

        #endregion
    }
}