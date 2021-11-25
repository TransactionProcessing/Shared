namespace Shared.EventStore.SubscriptionWorker
{
    using System;

    public record PersistentSubscriptionDetails
    {
        #region Constructors

        public PersistentSubscriptionDetails(String streamName,
                                             String groupName)
        {
            this.StreamName = streamName;
            this.GroupName = groupName;
        }

        #endregion

        #region Properties

        public String GroupName { get; init; }

        public Int32 InflightMessages { get; init; }

        public String StreamName { get; init; }

        #endregion

        #region Methods

        public override String ToString() => $"{this.StreamName}-{this.GroupName}";

        #endregion
    }
}