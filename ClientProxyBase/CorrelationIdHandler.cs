using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ClientProxyBase;

public class CorrelationIdHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _accessor;
    private const string HeaderName = "X-Correlation-ID";

    public CorrelationIdHandler(IHttpContextAccessor accessor) => this._accessor = accessor;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = this._accessor.HttpContext?.Items[HeaderName]?.ToString();

        if (!string.IsNullOrEmpty(correlationId) && !request.Headers.Contains(HeaderName))
        {
            request.Headers.Add(HeaderName, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}