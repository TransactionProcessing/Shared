namespace Shared.Logger;

using System;

/// <summary>
/// 
/// </summary>
public interface ILogger
{
    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether this instance is initialised.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is initialised; otherwise, <c>false</c>.
    /// </value>
    Boolean IsInitialised { get; set; }

    #endregion

    #region Methods

    void LogCritical(Exception exception);

    void LogCritical(String message,
                     Exception exception);

    void LogDebug(String message);

    void LogError(Exception exception);

    void LogError(String message,
                  Exception exception);

    void LogInformation(String message);

    void LogTrace(String message);

    void LogWarning(String message);

    #endregion
}