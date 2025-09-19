namespace Shared.EventStore.ProjectionEngine;

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DomainDrivenDesign.EventSourcing;

public class ProjectionHandler<TState> : IProjectionHandler where TState : State{
    #region Fields

    private readonly IProjection<TState> Projection;

    private readonly IProjectionStateRepository<TState> ProjectionStateRepository;

    private readonly IStateDispatcher<TState> StateDispatcher;

    #endregion

    #region Constructors

    public ProjectionHandler(IProjectionStateRepository<TState> projectionStateRepository,
                             IProjection<TState> projection,
                             IStateDispatcher<TState> stateDispatcher){
        this.ProjectionStateRepository = projectionStateRepository;
        this.Projection = projection;
        this.StateDispatcher = stateDispatcher;
    }

    #endregion

    #region Events

    public event EventHandler<String> TraceGenerated;

    #endregion

    #region Methods

    public async Task Handle(IDomainEvent @event, CancellationToken cancellationToken){
        if (@event == null) return;

        if (!this.Projection.ShouldIHandleEvent(@event)){
            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        //Load the state from persistence
        TState state = await this.ProjectionStateRepository.Load(@event, cancellationToken);

        if (state == null){
            return;
        }

        this.Trace($"{stopwatch.ElapsedMilliseconds}ms After Load");

        this.Trace($"{stopwatch.ElapsedMilliseconds}ms Handling {@event.EventType} for state {state.GetType().Name}");

        TState newState = await this.Projection.Handle(state, @event, cancellationToken);

        this.Trace($"{stopwatch.ElapsedMilliseconds}ms After Handle");

        if (newState != state){
            newState = newState with{
                                        ChangesApplied = true
                                    };

            // save state
            newState = await this.ProjectionStateRepository.Save(newState, @event, cancellationToken);

            //Repo might have detected a duplicate event
            this.Trace($"{stopwatch.ElapsedMilliseconds}ms After Save");

            if (this.StateDispatcher != null){
                //Send to anyone else interested
                await this.StateDispatcher.Dispatch(newState, @event, cancellationToken);

                this.Trace($"{stopwatch.ElapsedMilliseconds}ms After Dispatch");
            }
        }
        else{
            this.Trace($"{stopwatch.ElapsedMilliseconds}ms No Save required");
        }

        stopwatch.Stop();

        this.Trace($"Total time: {stopwatch.ElapsedMilliseconds}ms");
    }

    private void Trace(String traceMessage){
        if (this.TraceGenerated != null)
            this.TraceGenerated(this, traceMessage );
    }

    #endregion
}