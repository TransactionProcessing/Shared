﻿using SimpleResults;

namespace Shared.EventStore.EventStore
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using global::EventStore.Client;
    using Microsoft.Extensions.Diagnostics.HealthChecks;

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck" />
    public class EventStoreConnectionStringHealthCheck : IHealthCheck
    {
        private readonly EventStoreClient Client;
private readonly EventStoreProjectionManagementClient ProjectionManagementClient;

        #region Fields

        /// <summary>
        /// The event store client settings
        /// </summary>
        private readonly EventStoreClientSettings EventStoreClientSettings;

        /// <summary>
        /// The user credentials
        /// </summary>
        private readonly UserCredentials UserCredentials;

        private readonly IEventStoreContext Context;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreConnectionStringHealthCheck" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="userCredentials">The user credentials.</param>
        public EventStoreConnectionStringHealthCheck(EventStoreClientSettings settings, UserCredentials userCredentials)
        {
            this.EventStoreClientSettings = settings;
            this.UserCredentials = userCredentials;
            this.Client = new EventStoreClient(settings);
            this.ProjectionManagementClient = new(settings);
            this.Context = new EventStoreContext(this.Client, this.ProjectionManagementClient);
        }

        public EventStoreConnectionStringHealthCheck(EventStoreClientSettings settings) : this(settings, null)
        {
            
        }

        internal EventStoreConnectionStringHealthCheck(IEventStoreContext context) {
            this.Context = context;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Runs the health check, returning the status of the component being checked.
        /// </summary>
        /// <param name="context">A context object associated with the current execution.</param>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> that can be used to cancel the health check.</param>
        /// <returns>
        /// A <see cref="T:System.Threading.Tasks.Task`1" /> that completes when the health check has finished, yielding the status of the component being checked.
        /// </returns>
        /// <exception cref="Exception">$all stream not found</exception>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
                                                              CancellationToken cancellationToken)
        {
            try
            {
                Result<List<ResolvedEvent>> readResult = await this.Context.ReadLastEventsFromAll(1, cancellationToken);
                if (readResult.IsFailed) {
                    return HealthCheckResult.Unhealthy("Failed during read of $all stream");
                }
                if (readResult.Data.Any() == false)
                    return HealthCheckResult.Unhealthy("$all stream not found");
                
                return HealthCheckResult.Healthy();
            }
            catch(Exception ex)
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, exception:ex);
            }
        }

        #endregion
    }
}