namespace Shared.HealthChecks;

using System;
using System.Collections.Generic;

public class DependencyServiceResult
{
    public string name { get; set; }
    public HealthCheckStatus status { get; set; }
    public TimeSpan duration { get; set; }
    public List<string> tags { get; set; }
}