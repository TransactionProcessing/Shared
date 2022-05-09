namespace Shared.EventStore.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using DomainDrivenDesign.EventSourcing;
    using EventHandling;

    public record EstateCreatedEvent : DomainEvent
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EstateCreatedEvent" /> class.
        /// </summary>
        /// <param name="aggregateId">The aggregate identifier.</param>
        /// <param name="estateName">Name of the estate.</param>
        public EstateCreatedEvent(Guid aggregateId,
                                  String estateName) : base(aggregateId, Guid.NewGuid())
        {
            this.EstateId = aggregateId;
            this.EstateName = estateName;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the estate identifier.
        /// </summary>
        /// <value>
        /// The estate identifier.
        /// </value>
        public Guid EstateId { get; init; }

        /// <summary>
        /// Gets the name of the estate.
        /// </summary>
        /// <value>
        /// The name of the estate.
        /// </value>
        public String EstateName { get; init; }

        #endregion
    }

    public class TestDomainEventHandler : IDomainEventHandler
    {
        public List<IDomainEvent> DomainEvents = new();

        public async Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            DomainEvents.Add(domainEvent);
        }
    }
}
