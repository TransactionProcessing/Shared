namespace Shared.EventStore.SubscriptionWorker
{
    using System;

    public class TraceEventArgs : EventArgs
    {
        #region Properties

        public String Message { get; set; }

        #endregion
    }
}