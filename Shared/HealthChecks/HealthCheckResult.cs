namespace Shared.HealthChecks;

using System;
using System.Collections.Generic;

public class HealthCheckResult
{
    public HealthCheckStatus status { get; set; }
    public TimeSpan totalDuration { get; set; }
    public List<DependencyServiceResult> results { get; set; }
}