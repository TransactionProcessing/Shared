namespace Shared.EventStore.Subscriptions
{
    using System;

    public record PersistentSubscriptionDetails
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="" /> .
        /// </summary>
        /// <param name="streamName">Name of the stream.</param>
        /// <param name="groupName">Name of the group.</param>
        public PersistentSubscriptionDetails(String streamName,
                                             String groupName, 
                                             Int32 inflightCount)
        {
            this.StreamName = streamName;
            this.GroupName = groupName;
            this.InflightCount = inflightCount;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of the group.
        /// </summary>
        /// <value>
        /// The name of the group.
        /// </value>
        public String GroupName { get; init; }

        /// <summary>
        /// Gets the name of the stream.
        /// </summary>
        /// <value>
        /// The name of the stream.
        /// </value>
        public String StreamName { get; init; }

        public Int32 InflightCount { get; init; }

        #endregion

        #region Methods

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override String ToString()
        {
            return $"{this.StreamName}-{this.GroupName}";
        }

        #endregion
    }
}