﻿using SimpleResults;

namespace Shared.HealthChecks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IHealthCheckClient
    {
        #region Methods

        Task<Result<String>> PerformHealthCheck(String scheme, 
                                                String uri,
                                                Int32 port,
                                                CancellationToken cancellationToken);

        #endregion
    }
}