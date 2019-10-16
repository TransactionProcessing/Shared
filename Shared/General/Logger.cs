using Microsoft.Extensions.Logging;
using System;

namespace Shared.General
{
    public static class Logger
    {
        #region Private Properties        
        /// <summary>
        /// The logger object
        /// </summary>
        private static ILogger LoggerObject;
        #endregion

        #region Public Properties        
        /// <summary>
        /// Gets or sets a value indicating whether this instance is initialised.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is initialised; otherwise, <c>false</c>.
        /// </value>
        public static Boolean IsInitialised { get; set; }
        #endregion

        #region Public Methods

        #region public static void Initialise(ILogger loggerObject)        
        /// <summary>
        /// Initialises the specified logger object.
        /// </summary>
        /// <param name="loggerObject">The logger object.</param>
        public static void Initialise(ILogger loggerObject)
        {
            LoggerObject = loggerObject ?? throw new ArgumentNullException(nameof(loggerObject));

            Logger.IsInitialised = true;
        }
        #endregion

        #region public static void LogTrace(String message)        
        /// <summary>
        /// Logs the trace.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogTrace(String message)
        {
            ValidateLoggerObject();

            LoggerObject.LogTrace(new EventId(), message);            
        }
        #endregion

        #region public static void LogDebug(String message)        
        /// <summary>
        /// Logs the debug.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogDebug(String message)
        {
            ValidateLoggerObject();

            LoggerObject.LogDebug(new EventId(), message);
        }
        #endregion

        #region public static void LogInformation(String message)        
        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogInformation(String message)
        {
            ValidateLoggerObject();

            LoggerObject.LogInformation(new EventId(), message);
        }
        #endregion

        #region public static void LogWarning(String message)        
        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogWarning(String message)
        {
            ValidateLoggerObject();

            LoggerObject.LogWarning(new EventId(), message);
        }
        #endregion

        #region public static void LogError(Exception exception)        
        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void LogError(Exception exception)
        {
            ValidateLoggerObject();

            LoggerObject.LogError(new EventId(), exception, exception.Message);
        }
        #endregion

        #region public static void LogCritical(Exception exception)        
        /// <summary>
        /// Logs the critical.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void LogCritical(Exception exception)
        {
            ValidateLoggerObject();

            LoggerObject.LogCritical(new EventId(), exception, exception.Message);
        }
        #endregion

        #endregion

        #region Private Methods

        #region private static void ValidateLoggerObject()        
        /// <summary>
        /// Validates the logger object.
        /// </summary>
        /// <exception cref="InvalidOperationException">Logger has not been initialised</exception>
        private static void ValidateLoggerObject()
        {
            if (LoggerObject == null)
            {
                throw new InvalidOperationException("Logger has not been initialised");
            }
        }
        #endregion

        #endregion

    }

}
