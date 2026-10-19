#nullable enable
using System;

namespace Shared.EntityFramework;

/// <summary>Controls which slow EF Core commands are logged.</summary>
public sealed class QueryTimingInterceptorOptions
{
    /// <summary>Gets the minimum duration in milliseconds required for logging. Defaults to 500.</summary>
    public int ThresholdMilliseconds { get; init; } = 500;

    /// <summary>Gets whether SQL command text is included in warning logs. Defaults to false.</summary>
    public bool IncludeCommandText { get; init; }
}
