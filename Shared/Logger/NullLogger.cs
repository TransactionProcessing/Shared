namespace Shared.Logger
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Shared.Logger.ILogger" />
    public class NullLogger : ILogger
    {
        #region Properties

        public static NullLogger Instance => new NullLogger();

        public Boolean IsInitialised { get; set; }

        #endregion

        #region Methods

        public void Initialise() {
            this.IsInitialised = true;
        }

        public void LogCritical(Exception exception) {
        }

        public void LogCritical(String message,
                                Exception exception) {
        }

        public void LogDebug(String message) {
        }

        public void LogError(Exception exception) {
        }

        public void LogError(String message,
                             Exception exception) {
        }

        public void LogInformation(String message) {
        }

        public void LogTrace(String message) {
        }

        public void LogWarning(String message) {
        }

        #endregion
    }
}