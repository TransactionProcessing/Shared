namespace Shared.EventStore.ProjectionEngine{
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;

    public interface IStateDispatcher<in TState> where TState : State{
        #region Methods

        Task Dispatch(TState state, IDomainEvent @event, CancellationToken cancellationToken);

        #endregion
    }
}