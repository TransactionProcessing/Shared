using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleResults;

namespace Shared.Tests;

using System.Net;
using System.Net.Http;
using System.Threading;
using Shouldly;
using Xunit;

public sealed class ClientProxyBaseTests
{
    [Fact]
    public async Task Get_DeserialisesResponseAndReturnsData()
    {
        var responseBody = """{"id":42,"name":"alpha"}""";
        var expected = new SampleResponse(42, "alpha");
        var deserialiseCalls = 0;

        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody)
        });

        var sut = CreateWrapper(
            handler,
            deserialise: (content, type) =>
            {
                deserialiseCalls++;
                content.ShouldBe(responseBody);
                type.ShouldBe(typeof(SampleResponse));
                return expected;
            });

        var result = await sut.Get<SampleResponse>("widgets/42");

        result.IsSuccess.ShouldBeTrue();
        result.IsFailed.ShouldBeFalse();
        result.Data.ShouldBe(expected);
        deserialiseCalls.ShouldBe(1);
    }

    [Fact]
    public async Task Get_StringResponse_BypassesDeserialise()
    {
        const string responseBody = "plain-text";
        var deserialiseCalls = 0;

        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody)
        });

        var sut = CreateWrapper(
            handler,
            deserialise: (_, _) =>
            {
                deserialiseCalls++;
                return string.Empty;
            });

        var result = await sut.Get<string>("widgets/raw");

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(responseBody);
        deserialiseCalls.ShouldBe(0);
    }

    [Fact]
    public async Task Get_WithTokenAndHeaders_AddsAuthorizationAndHeaders()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":1,"name":"ok"}""")
        });

        var sut = CreateWrapper(handler, deserialise: (_, _) => new SampleResponse(1, "ok"));

        var result = await sut.Get<SampleResponse>(
            "widgets/1",
            "token-123",
            [("X-Correlation-Id", "corr-1"), ("Accept-Language", "en-GB")]);

        result.IsSuccess.ShouldBeTrue();
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Get);
        handler.LastRequest.AuthorizationScheme.ShouldBe("Bearer");
        handler.LastRequest.AuthorizationParameter.ShouldBe("token-123");
        handler.LastRequest.Headers["X-Correlation-Id"].ShouldBe("corr-1");
        handler.LastRequest.Headers["Accept-Language"].ShouldBe("en-GB");
        handler.LastRequest.Body.ShouldBeNull();
    }

    [Fact]
    public async Task Delete_WithTokenAndHeaders_SendsExpectedRequest()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = CreateWrapper(handler);

        var result = await sut.Delete(
            "widgets/1",
            "token-123",
            [("X-Correlation-Id", "corr-2")]);

        result.IsSuccess.ShouldBeTrue();
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Delete);
        handler.LastRequest.AuthorizationScheme.ShouldBe("Bearer");
        handler.LastRequest.AuthorizationParameter.ShouldBe("token-123");
        handler.LastRequest.Headers["X-Correlation-Id"].ShouldBe("corr-2");
        handler.LastRequest.Body.ShouldBeNull();
    }

    [Fact]
    public async Task Post_WithTokenAndHeaders_SerialisesBodyAndAddsContentHeaders()
    {
        var request = new SampleRequest("alpha");
        var serialiseCalls = 0;
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateWrapper(
            handler,
            serialise: value =>
            {
                serialiseCalls++;
                value.ShouldBeSameAs(request);
                return """{"name":"alpha"}""";
            });

        var result = await sut.Post(
            "widgets",
            request,
            "token-123",
            [("X-Correlation-Id", "corr-3"), ("Content-Language", "en-GB")]);

        result.IsSuccess.ShouldBeTrue();
        serialiseCalls.ShouldBe(1);
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.Body.ShouldBe("""{"name":"alpha"}""");
        handler.LastRequest.ContentType.ShouldBe("application/json; charset=utf-8");
        handler.LastRequest.AuthorizationScheme.ShouldBe("Bearer");
        handler.LastRequest.AuthorizationParameter.ShouldBe("token-123");
        handler.LastRequest.Headers["X-Correlation-Id"].ShouldBe("corr-3");
        handler.LastRequest.ContentHeaders["Content-Language"].ShouldBe("en-GB");
    }

    [Fact]
    public async Task Post_HttpContentWithTokenAndHeaders_SendsRawContentWithoutSerialising()
    {
        var serialiseCalls = 0;
        using var content = new StringContent("raw-post-body");
        content.Headers.ContentLanguage.Add("en-GB");

        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateWrapper(
            handler,
            serialise: _ =>
            {
                serialiseCalls++;
                return "{}";
            });

        var result = await sut.Post(
            "widgets/raw",
            content,
            "token-post",
            [("X-Correlation-Id", "corr-post")]);

        result.IsSuccess.ShouldBeTrue();
        serialiseCalls.ShouldBe(0);
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.Body.ShouldBe("raw-post-body");
        handler.LastRequest.AuthorizationParameter.ShouldBe("token-post");
        handler.LastRequest.Headers["X-Correlation-Id"].ShouldBe("corr-post");
        handler.LastRequest.ContentHeaders["Content-Language"].ShouldBe("en-GB");
    }

    [Fact]
    public async Task Post_MultipartWithTokenAndHeaders_SendsMultipartContent()
    {
        var fileData = new byte[] { 1, 2, 3, 4 };
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateWrapper(handler);

        var result = await sut.Post(
            "uploads",
            fileData,
            "statement.csv",
            [
                ("EstateId", "10"),
                ("MerchantId", "20"),
                ("FileProfileId", "30"),
                ("UserId", "40"),
                ("UploadDateTime", "2026-05-02 07:00:00")
            ],
            "token-456",
            [("X-Correlation-Id", "corr-multipart"), ("Content-Language", "en-GB")]);

        result.IsSuccess.ShouldBeTrue();
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.AuthorizationScheme.ShouldBe("Bearer");
        handler.LastRequest.AuthorizationParameter.ShouldBe("token-456");
        handler.LastRequest.Headers["X-Correlation-Id"].ShouldBe("corr-multipart");
        handler.LastRequest.ContentHeaders["Content-Language"].ShouldBe("en-GB");
        handler.LastRequest.ContentType.ShouldStartWith("multipart/form-data; boundary=");
        handler.LastRequest.Body.ShouldNotBeNull();
        handler.LastRequest.Body.ShouldContain("name=file; filename=statement.csv");
        handler.LastRequest.Body.ShouldContain("Content-Type: multipart/form-data");
        handler.LastRequest.Body.ShouldContain("name=EstateId");
        handler.LastRequest.Body.ShouldContain("10");
        handler.LastRequest.Body.ShouldContain("name=UploadDateTime");
        handler.LastRequest.Body.ShouldContain("2026-05-02 07:00:00");
    }

    [Fact]
    public async Task Put_WithTokenAndHeaders_SerialisesBodyAndReturnsSuccess()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateWrapper(handler, serialise: _ => """{"name":"beta"}""");

        var result = await sut.Put(
            "widgets/2",
            new SampleRequest("beta"),
            "token-234",
            [("X-Correlation-Id", "corr-4")]);

        result.IsSuccess.ShouldBeTrue();
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
        handler.LastRequest.Body.ShouldBe("""{"name":"beta"}""");
        handler.LastRequest.AuthorizationParameter.ShouldBe("token-234");
        handler.LastRequest.Headers["X-Correlation-Id"].ShouldBe("corr-4");
    }

    [Fact]
    public async Task Put_HttpContentWithTokenAndHeaders_SendsRawContentWithoutSerialising()
    {
        var serialiseCalls = 0;
        using var content = new StringContent("raw-put-body");

        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateWrapper(
            handler,
            serialise: _ =>
            {
                serialiseCalls++;
                return "{}";
            });

        var result = await sut.Put(
            "widgets/raw/2",
            content,
            "token-put",
            [("X-Correlation-Id", "corr-put")]);

        result.IsSuccess.ShouldBeTrue();
        serialiseCalls.ShouldBe(0);
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
        handler.LastRequest.Body.ShouldBe("raw-put-body");
        handler.LastRequest.AuthorizationParameter.ShouldBe("token-put");
        handler.LastRequest.Headers["X-Correlation-Id"].ShouldBe("corr-put");
    }

    [Fact]
    public async Task Patch_GenericWithTokenAndHeaders_DeserialisesResponse()
    {
        var expected = new SampleResponse(5, "patched");
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":5,"name":"patched"}""")
        });

        var sut = CreateWrapper(
            handler,
            serialise: _ => """{"name":"patched"}""",
            deserialise: (_, type) =>
            {
                type.ShouldBe(typeof(SampleResponse));
                return expected;
            });

        var result = await sut.Patch<SampleRequest, SampleResponse>(
            "widgets/5",
            new SampleRequest("patched"),
            "token-345",
            [("X-Correlation-Id", "corr-5")]);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(expected);
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Patch);
        handler.LastRequest.AuthorizationParameter.ShouldBe("token-345");
        handler.LastRequest.Headers["X-Correlation-Id"].ShouldBe("corr-5");
    }

    [Fact]
    public async Task Patch_HttpContentGenericWithTokenAndHeaders_DeserialisesResponseWithoutSerialising()
    {
        var serialiseCalls = 0;
        var expected = new SampleResponse(9, "raw-patch");
        using var content = new StringContent("raw-patch-body");

        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":9,"name":"raw-patch"}""")
        });

        var sut = CreateWrapper(
            handler,
            serialise: _ =>
            {
                serialiseCalls++;
                return "{}";
            },
            deserialise: (_, type) =>
            {
                type.ShouldBe(typeof(SampleResponse));
                return expected;
            });

        var result = await sut.Patch<SampleResponse>(
            "widgets/raw/9",
            content,
            "token-patch",
            [("X-Correlation-Id", "corr-patch")]);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(expected);
        serialiseCalls.ShouldBe(0);
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Patch);
        handler.LastRequest.Body.ShouldBe("raw-patch-body");
        handler.LastRequest.AuthorizationParameter.ShouldBe("token-patch");
        handler.LastRequest.Headers["X-Correlation-Id"].ShouldBe("corr-patch");
    }

    [Fact]
    public async Task Post_WhenSerialiseThrows_ReturnsFailureResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateWrapper(
            handler,
            serialise: _ => throw new InvalidOperationException("boom"));

        var result = await sut.Post("widgets", new SampleRequest("broken"));

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Failure);
        result.Message.ShouldContain("The request body could not be serialized: boom");
    }

    [Fact]
    public async Task Get_WhenDeserialiseThrows_ReturnsFailureResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":7}""")
        });

        var sut = CreateWrapper(
            handler,
            deserialise: (_, _) => throw new InvalidOperationException("cannot parse"));

        var result = await sut.Get<SampleResponse>("widgets/7");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Failure);
        result.Message.ShouldContain("The response body could not be deserialized: cannot parse");
    }

    [Fact]
    public async Task Get_WhenHandlerTimesOut_ReturnsCriticalError()
    {
        var handler = new RecordingHandler(_ => throw new OperationCanceledException("timed out"));
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/timeout");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.CriticalError);
        result.Message.ShouldBe("The HTTP request timed out.");
    }

    [Fact]
    public async Task Get_WhenResponseIsBadRequest_MapsErrorsToInvalidResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"title":"Validation failed","errors":{"name":["Name is required"],"age":["Age must be positive"]}}""")
        });

        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/invalid");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldBe("Validation failed");
        result.Errors.ShouldContain("Validation failed");
        result.Errors.ShouldContain("Name is required");
        result.Errors.ShouldContain("Age must be positive");
    }

    [Fact]
    public async Task Get_WhenDeserialisedTypeIsWrong_ReturnsFailureResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":8}""")
        });

        var sut = CreateWrapper(handler, deserialise: (_, _) => new DifferentResponse("wrong"));

        var result = await sut.Get<SampleResponse>("widgets/8");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Failure);
        result.Message.ShouldContain("is not assignable");
    }

    [Fact]
    public async Task Get_WithWhitespaceToken_ThrowsArgumentException()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":1}""")
        });

        var sut = CreateWrapper(handler);

        var exception = await Should.ThrowAsync<ArgumentException>(
            () => sut.Get<SampleResponse>("widgets/1", " "));

        exception.ParamName.ShouldBe("accessToken");
    }

    private static TestHttpClientWrapper CreateWrapper(
        RecordingHandler handler,
        Func<object, string>? serialise = null,
        Func<string, Type, object>? deserialise = null)
    {
        return new TestHttpClientWrapper(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://example.test/")
            },
            serialise ?? (_ => "{}"),
            deserialise ?? ((_, type) => Activator.CreateInstance(type) ?? throw new InvalidOperationException("No default instance.")));
    }

    private sealed class TestHttpClientWrapper : ClientProxyBase.ClientProxyBase
    {
        public TestHttpClientWrapper(
            HttpClient httpClient,
            Func<object, string> serialise,
            Func<string, Type, object> deserialise)
            : base(httpClient, serialise, deserialise)
        {
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> Responder;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            Responder = responder;
        }

        public CapturedRequest? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = await CapturedRequest.Create(request, cancellationToken);
            return Responder(request);
        }
    }

    private sealed record CapturedRequest(
        HttpMethod Method,
        Uri? RequestUri,
        string? AuthorizationScheme,
        string? AuthorizationParameter,
        IReadOnlyDictionary<string, string> Headers,
        IReadOnlyDictionary<string, string> ContentHeaders,
        string? ContentType,
        string? Body)
    {
        public static async Task<CapturedRequest> Create(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new CapturedRequest(
                request.Method,
                request.RequestUri,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                request.Headers.ToDictionary(
                    header => header.Key,
                    header => string.Join(",", header.Value)),
                request.Content?.Headers.ToDictionary(
                    header => header.Key,
                    header => string.Join(",", header.Value))
                ?? new Dictionary<string, string>(),
                request.Content?.Headers.ContentType?.ToString(),
                body);
        }
    }

    private sealed record SampleRequest(string Name);

    private sealed record SampleResponse(int Id, string Name);

    private sealed record DifferentResponse(string Value);
}
