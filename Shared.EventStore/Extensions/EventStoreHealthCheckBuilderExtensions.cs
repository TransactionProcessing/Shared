namespace Shared.EventStore.Extensions
{
    using System;
    using System.Collections.Generic;
    using EventStore;
    using global::EventStore.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    /// <summary>
    /// 
    /// </summary>
    public static class EventStoreHealthCheckBuilderExtensions
    {
        #region Methods

        /// <summary>
        /// Add a health check for EventStore services.
        /// </summary>
        /// <param name="builder">The <see cref="IHealthChecksBuilder" />.</param>
        /// <param name="eventStoreClientSettings">The event store client settings.</param>
        /// <param name="userCredentials">The user credentials.</param>
        /// <param name="name">The health check name. Optional. If <c>null</c> the type name 'eventstore' will be used for the name.</param>
        /// <param name="failureStatus">The <see cref="HealthStatus" /> that should be reported when the health check fails. Optional. If <c>null</c> then
        /// the default status of <see cref="HealthStatus.Unhealthy" /> will be reported.</param>
        /// <param name="tags">A list of tags that can be used to filter sets of health checks. Optional.</param>
        /// <returns>
        /// The <see cref="IHealthChecksBuilder" />.
        /// </returns>
        public static IHealthChecksBuilder AddEventStore(this IHealthChecksBuilder builder,
                                                         EventStoreClientSettings eventStoreClientSettings,
                                                         UserCredentials userCredentials = null,
                                                         String name = default,
                                                         HealthStatus? failureStatus = default,
                                                         IEnumerable<String> tags = default)
        {
            return builder.Add(new HealthCheckRegistration(name ?? EventStoreHealthCheckBuilderExtensions.NAME,
                                                           sp => new EventStoreConnectionStringHealthCheck(eventStoreClientSettings, userCredentials),
                                                           failureStatus,
                                                           tags));
        }

        #endregion

        #region Others

        /// <summary>
        /// The name
        /// </summary>
        private const String NAME = "eventstore";

        #endregion
    }
}