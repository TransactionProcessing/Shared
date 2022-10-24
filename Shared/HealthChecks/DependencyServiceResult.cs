namespace Shared.HealthChecks;

using System;
using System.Collections.Generic;

public class DependencyServiceResult
{
    #region Properties

    public TimeSpan Duration { get; set; }

    public String Name { get; set; }

    public HealthCheckStatus Status { get; set; }

    public List<String> Tags { get; set; }

    #endregion
}