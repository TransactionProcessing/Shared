namespace Shared.EventStore.Subscriptions
{
    using System;

    /// <summary>
    /// 
    /// </summary>
    [Obsolete]
    public class Subscription
    {
        #region Properties

        /// <summary>
        /// Gets or sets the end point URI.
        /// </summary>
        /// <value>
        /// The end point URI.
        /// </value>
        public String EndPointUri { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        /// <value>
        /// The name of the group.
        /// </value>
        public String GroupName { get; set; }

        /// <summary>
        /// Gets or sets the name of the stream.
        /// </summary>
        /// <value>
        /// The name of the stream.
        /// </value>
        public String StreamName { get; set; }

        /// <summary>
        /// Gets or sets the stream position to restart from.
        /// </summary>
        /// <value>
        /// The stream position to restart from.
        /// </value>
        public Int32? StreamPositionToRestartFrom { get; set; }

        /// <summary>
        /// Gets or sets the subscription identifier.
        /// </summary>
        /// <value>
        /// The subscription identifier.
        /// </value>
        public Guid SubscriptionId { get; set; }

        #endregion
    }
}