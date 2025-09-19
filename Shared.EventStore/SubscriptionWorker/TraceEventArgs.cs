namespace Shared.EventStore.SubscriptionWorker;

using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class TraceEventArgs : EventArgs
{
    #region Properties

    public String Message { get; set; }

    #endregion
}