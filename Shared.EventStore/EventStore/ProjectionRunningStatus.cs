namespace Shared.EventStore.EventStore;

public enum ProjectionRunningStatus
{
    /// <summary>
    /// The unknown
    /// </summary>
    Unknown,

    /// <summary>
    /// The not found
    /// </summary>
    NotFound,

    /// <summary>
    /// The stopped
    /// </summary>
    Stopped,

    /// <summary>
    /// The running
    /// </summary>
    Running,

    /// <summary>
    /// The statistics not found
    /// </summary>
    StatisticsNotFound,

    /// <summary>
    /// The faulted
    /// </summary>
    Faulted,

    /// <summary>
    /// The completed
    /// </summary>
    Completed
}