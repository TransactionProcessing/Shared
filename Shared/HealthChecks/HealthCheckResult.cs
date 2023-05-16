namespace Shared.HealthChecks;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class HealthCheckResult
{
    #region Properties
    public String Status { get; set; }
    #endregion
}