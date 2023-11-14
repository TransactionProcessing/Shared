namespace Shared.EventStore.Tests.TestObjects;

using System;
using DomainDrivenDesign.EventSourcing;

public record EstateCreatedEvent : DomainEvent
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EstateCreatedEvent" /> class.
    /// </summary>
    /// <param name="aggregateId">The aggregate identifier.</param>
    /// <param name="estateName">Name of the estate.</param>
    public EstateCreatedEvent(Guid aggregateId,
                              string estateName) : base(aggregateId, Guid.NewGuid())
    {
        EstateId = aggregateId;
        EstateName = estateName;
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
    public string EstateName { get; init; }

    #endregion
}