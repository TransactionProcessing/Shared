namespace Shared.EventStore.Subscriptions
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    [Obsolete]
    public class SubscriptionInformation
    {
        #region Properties

        /// <summary>
        /// Gets or sets the event stream identifier.
        /// </summary>
        /// <value>
        /// The event stream identifier.
        /// </value>
        public String EventStreamId { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        /// <value>
        /// The name of the group.
        /// </value>
        public String GroupName { get; set; }

        #endregion
    }
}