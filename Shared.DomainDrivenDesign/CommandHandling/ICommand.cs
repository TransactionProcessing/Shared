namespace Shared.DomainDrivenDesign.CommandHandling
{
    using System;

    public interface ICommand
    {
        /// <summary>
        /// Gets the command identifier.
        /// </summary>
        /// <value>
        /// The command identifier.
        /// </value>
        Guid CommandId { get; }
    }
}