namespace Shared.DomainDrivenDesign.CommandHandling
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface ICommandRouter
    {
        /// <summary>
        /// Routes the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        Task Route(ICommand command,CancellationToken cancellationToken);
    }
}