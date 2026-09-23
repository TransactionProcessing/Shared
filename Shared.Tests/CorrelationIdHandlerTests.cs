namespace Shared.Tests;

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClientProxyBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Shared.Logger.TennantContext;
using Shouldly;
using Xunit;

public class CorrelationIdHandlerTests
{
    [Fact]
    public async Task SendAsync_ForwardsTheCanonicalCorrelationIdFromHttpContextItems()
    {
        Guid value = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        DefaultHttpContext httpContext = new(new FeatureCollection());
        httpContext.Items["correlationId"] = CorrelationId.From(value).ToString();
        HttpContextAccessor accessor = new() { HttpContext = httpContext };
        CapturingHandler innerHandler = new();
        CorrelationIdHandler handler = new(accessor) { InnerHandler = innerHandler };
        using HttpMessageInvoker invoker = new(handler);

        using HttpRequestMessage request = new(HttpMethod.Get, "https://example.test");

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.CorrelationId.ShouldBe("01234567-89ab-cdef-0123-456789abcdef");
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string CorrelationId { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            this.CorrelationId = request.Headers.GetValues("correlationId").Single();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
