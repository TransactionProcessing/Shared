namespace Shared.DomainDrivenDesign.CommandHandling
{
    using System;

    public abstract class Command<T> : ICommand
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Command<T>" /> class.
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        protected Command(Guid commandId)
        {
            this.GuardAgainstNoCommandId(commandId);

            this.CommandId = commandId;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the command identifier.
        /// </summary>
        /// <value>The command identifier.</value>
        public Guid CommandId { get; }

        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        public T Response { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Guards against no event ID
        /// </summary>
        /// <param name="commandId">The command identifier.</param>
        /// <exception cref="System.ArgumentNullException">commandId;No event ID provided</exception>
        private void GuardAgainstNoCommandId(Guid commandId)
        {
            if (commandId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(commandId), "A command Id must be provided");
            }
        }

        #endregion
    }
}