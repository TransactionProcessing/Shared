namespace Shared.HealthChecks;

using System;
using System.Collections.Generic;

public class HealthCheckResult
{
    #region Properties

    public List<DependencyServiceResult> Results { get; set; }

    public HealthCheckStatus Status { get; set; }

    public TimeSpan TotalDuration { get; set; }

    #endregion
}