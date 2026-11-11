using TransactionProcessing.UnionResults;

namespace TransactionProcessing.Shared.HealthChecks;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IHealthCheckClient
{

    Task<Result<String>> PerformHealthCheck(String scheme, 
                                            String uri,
                                            Int32 port,
                                            CancellationToken cancellationToken);

}