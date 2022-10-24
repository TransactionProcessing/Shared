using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.HealthChecks
{
    public interface IHealthCheckClient
    {
        Task<HealthCheckResult> PerformHealthCheck(String uri, Int32 port, CancellationToken cancellationToken);
    }
}
