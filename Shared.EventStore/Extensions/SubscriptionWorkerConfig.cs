namespace Shared.EventStore.Extensions;

using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubscriptionWorkerConfig
{
    #region Properties

    public Boolean Enabled { get; set; }

    public String IgnoreGroups { get; set; }

    public String IgnoreStreams { get; set; }

    public String IncludeGroups { get; set; }

    public String IncludeStreams { get; set; }

    public Int32 InflightMessages { get; set; }

    public Int32 InstanceCount { get; set; }

    public Boolean IsOrdered { get; set; }
    public Boolean IsDomainOnly { get; set; }

    public String WorkerName { get; set; }

    #endregion
}