using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TransactionProcessing.ClientProxyBase;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace TransactionProcessing.Shared.Tests;

public sealed class ClientProxyBaseInfrastructureTests
{
    [Fact]
    public async Task CorrelationIdHandler_WithoutHttpContext_ForwardsRequestUnchanged()
    {
        var handler = CreateCorrelationHandler(accessor: new HttpContextAccessor());
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        using var response = await Send(handler, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        request.Headers.Contains("correlationId").ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CorrelationIdHandler_WithMissingOrEmptyItem_DoesNotAddHeader(string? correlationId)
    {
        var context = new DefaultHttpContext();
        if (correlationId is not null)
        {
            context.Items["correlationId"] = correlationId;
        }
        var handler = CreateCorrelationHandler(new HttpContextAccessor { HttpContext = context });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        using var response = await Send(handler, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        request.Headers.Contains("correlationId").ShouldBeFalse();
    }

    [Fact]
    public async Task CorrelationIdHandler_DoesNotReplaceExistingHeader()
    {
        var context = new DefaultHttpContext();
        context.Items["correlationId"] = "context-id";
        var handler = CreateCorrelationHandler(new HttpContextAccessor { HttpContext = context });
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");
        request.Headers.Add("correlationId", "request-id");

        using var response = await Send(handler, request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        request.Headers.GetValues("correlationId").ShouldBe(["request-id"]);
    }

    [Fact]
    public async Task CorrelationIdHandler_AddsContextItemAndForwardsRequest()
    {
        var context = new DefaultHttpContext();
        context.Items["correlationId"] = 12345;
        var innerHandler = new CapturingHandler();
        var handler = new CorrelationIdHandler(new HttpContextAccessor { HttpContext = context })
        {
            InnerHandler = innerHandler
        };
        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test");

        using var response = await invoker.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        innerHandler.CorrelationId.ShouldBe("12345");
    }

    [Fact]
    public void GetSocketsHttpHandler_ConfiguresCertificateValidationCallback()
    {
        using var handler = GetSocketsHttpHandler.GetHandler().ShouldBeOfType<SocketsHttpHandler>();
        RemoteCertificateValidationCallback callback = handler.SslOptions.RemoteCertificateValidationCallback!;

        callback(null!, null, null, SslPolicyErrors.RemoteCertificateNameMismatch).ShouldBeTrue();
    }

    [Fact]
    public void RegisterHttpClient_RegistersTypedClientAndTransientCorrelationHandler()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();

        var builder = services.RegisterHttpClient<ITestHttpClient, TestHttpClient>();

        builder.ShouldNotBeNull();
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ITestHttpClient>().ShouldBeOfType<TestHttpClient>();
        var firstHandler = provider.GetRequiredService<CorrelationIdHandler>();
        var secondHandler = provider.GetRequiredService<CorrelationIdHandler>();
        firstHandler.ShouldNotBeSameAs(secondHandler);
    }

    private static CorrelationIdHandler CreateCorrelationHandler(IHttpContextAccessor accessor) =>
        new(accessor) { InnerHandler = new CapturingHandler() };

    private static Task<HttpResponseMessage> Send(CorrelationIdHandler handler, HttpRequestMessage request)
    {
        var invoker = new HttpMessageInvoker(handler);
        return SendAndDisposeInvoker(invoker, request);
    }

    private static async Task<HttpResponseMessage> SendAndDisposeInvoker(HttpMessageInvoker invoker, HttpRequestMessage request)
    {
        using (invoker)
        {
            return await invoker.SendAsync(request, TestContext.Current.CancellationToken);
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public string? CorrelationId { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CorrelationId = request.Headers.TryGetValues("correlationId", out var values) ? values.Single() : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private interface ITestHttpClient;

    private sealed class TestHttpClient(HttpClient httpClient) : ITestHttpClient
    {
        public HttpClient HttpClient { get; } = httpClient;
    }
}
