namespace Shared.Logger
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Shared.Logger.ILogger" />
    [ExcludeFromCodeCoverage]
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
            // This is a null logger so needs to actual implementation
        }

        public void LogCritical(String message,
                                Exception exception) {
            // This is a null logger so needs to actual implementation
        }

        public void LogDebug(String message) {
            // This is a null logger so needs to actual implementation
        }

        public void LogError(Exception exception) {
            // This is a null logger so needs to actual implementation
        }

        public void LogError(String message,
                             Exception exception) {
            // This is a null logger so needs to actual implementation
        }

        public void LogInformation(String message) {
            // This is a null logger so needs to actual implementation
        }

        public void LogTrace(String message) {
            // This is a null logger so needs to actual implementation
        }

        public void LogWarning(String message) {
            // This is a null logger so needs to actual implementation
        }

        #endregion
    }
}