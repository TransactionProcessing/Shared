using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SimpleResults;

namespace TransactionProcessing.Shared.Tests;

using System.Net;
using System.Net.Http;
using System.Threading;
using TransactionProcessing.Shared.Logger.TennantContext;
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
    public async Task Get_WithCorrelationIdHeaderAndCurrentTenant_PrefersCurrentTenantCorrelationId()
    {
        var tenantCorrelationId = CorrelationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var previousTenant = TenantContext.CurrentTenant;
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":1,"name":"ok"}""")
        });

        try
        {
            TenantContext.CurrentTenant = new TenantContext();
            TenantContext.CurrentTenant.SetCorrelationId(tenantCorrelationId);

            var sut = CreateWrapper(handler, deserialise: (_, _) => new SampleResponse(1, "ok"));

            var result = await sut.Get<SampleResponse>(
                "widgets/1",
                [(TenantContext.KeyNameCorrelationId, "caller-correlation-id")]);

            result.IsSuccess.ShouldBeTrue();
            handler.LastRequest.ShouldNotBeNull();
            handler.LastRequest!.Headers[TenantContext.KeyNameCorrelationId]
                .ShouldBe(tenantCorrelationId.ToString());
        }
        finally
        {
            TenantContext.CurrentTenant = previousTenant;
        }
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
            (HttpContent)content,
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
            (HttpContent)content,
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
        result.Message.ShouldBe("The request body could not be serialized: boom");
    }

    [Fact]
    public async Task Post_WhenSerialiserReturnsNull_ReturnsFailureResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateWrapper(handler, serialise: _ => null!);

        var result = await sut.Post("widgets", new SampleRequest("null"));

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Failure);
        result.Message.ShouldBe("The request body could not be serialized.");
        handler.LastRequest.ShouldBeNull();
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
    public async Task Get_WithWhitespaceToken_ReturnsInvalidResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"id":1}""")
        });

        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/1", " ");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldContain("Access token");
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public void Constructor_RejectsNullDependencies()
    {
        Should.Throw<ArgumentNullException>(() => new TestHttpClientWrapper(null!, _ => "{}", (_, _) => new SampleResponse(1, "x")))
            .ParamName.ShouldBe("httpClient");
        Should.Throw<ArgumentNullException>(() => new TestHttpClientWrapper(new HttpClient(), null!, (_, _) => new SampleResponse(1, "x")))
            .ParamName.ShouldBe("serialise");
        Should.Throw<ArgumentNullException>(() => new TestHttpClientWrapper(new HttpClient(), _ => "{}", null!))
            .ParamName.ShouldBe("deserialise");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Get_WithInvalidRequestUri_ReturnsInvalidResult(string requestUri)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>(requestUri!);

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldContain("Request URI");
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Get_WithEmptySuccessfulResponse_ReturnsFailure()
    {
        var sut = CreateWrapper(new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("  ")
        }));

        var result = await sut.Get<SampleResponse>("widgets/empty");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Failure);
        result.Message.ShouldBe("The response content was empty.");
    }

    [Fact]
    public async Task Get_WhenDeserializerReturnsNull_ReturnsFailure()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        var sut = CreateWrapper(handler, deserialise: (_, _) => null!);

        var result = await sut.Get<SampleResponse>("widgets/null");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Failure);
        result.Message.ShouldBe("The response body could not be deserialized.");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
    [InlineData(HttpStatusCode.Conflict, ResultStatus.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
    [InlineData(HttpStatusCode.BadGateway, ResultStatus.CriticalError)]
    [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
    [InlineData(HttpStatusCode.UnprocessableEntity, ResultStatus.Failure)]
    public async Task Delete_MapsFailureStatusCodes(HttpStatusCode statusCode, ResultStatus expectedStatus)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("{\"detail\":\"server message\"}")
        });
        var sut = CreateWrapper(handler);

        var result = await sut.Delete("widgets/failed");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(expectedStatus);
        result.Message.ShouldBe("server message");
        result.Errors.ShouldContain("server message");
    }

    [Fact]
    public async Task Get_WhenFailureBodyIsEmpty_UsesReasonPhrase()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("")
        });
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/failed");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.CriticalError);
        result.Message.ShouldBe("Bad Gateway");
    }

    [Fact]
    public async Task Get_WhenFailureBodyAndReasonPhraseAreEmpty_UsesStatusCodeInMessage()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage((HttpStatusCode)418)
        {
            ReasonPhrase = null,
            Content = new StringContent("")
        });
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/failed");

        result.IsFailed.ShouldBeTrue();
        result.Message.ShouldBe("Request failed with status code 418.");
    }

    [Fact]
    public async Task Get_WhenFailureBodyIsNotJson_UsesRawBodyAsError()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("plain error")
        });
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/missing");

        result.Status.ShouldBe(ResultStatus.NotFound);
        result.Message.ShouldBe("plain error");
        result.Errors.ShouldContain("plain error");
    }

    [Theory]
    [InlineData("\"top-level error\"", "top-level error")]
    [InlineData("{\"detail\":\"detail error\"}", "detail error")]
    [InlineData("{\"message\":\"message error\"}", "message error")]
    [InlineData("{\"errors\":[\"nested error\"]}", "nested error")]
    [InlineData("{\"errors\":[],\"metadata\":{\"code\":42}}", "{\"errors\":[],\"metadata\":{\"code\":42}}")]
    [InlineData("{\"title\":42,\"detail\":null,\"message\":false,\"scalar\":\"ignored\"}", "{\"title\":42,\"detail\":null,\"message\":false,\"scalar\":\"ignored\"}")]
    [InlineData("{\"errors\":[\"\",\"  \"]}", "{\"errors\":[\"\",\"  \"]}")]
    public async Task Get_ExtractsSupportedErrorBodyShapes(string body, string expectedMessage)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body)
        });
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/invalid");

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldBe(expectedMessage);
        result.Errors.ShouldContain(expectedMessage);
    }

    [Theory]
    [InlineData("{\"metadata\":{\"message\":\"nested metadata error\"}}", "nested metadata error")]
    [InlineData("[\"first array error\",{\"detail\":\"second array error\"}]", "first array error")]
    public async Task Get_ExtractsErrorsFromNestedObjectsAndArrays(string body, string expectedMessage)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body)
        });
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/invalid");

        result.IsFailed.ShouldBeTrue();
        result.Message.ShouldBe(expectedMessage);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("true")]
    public async Task Get_WhenFailureBodyIsUnsupportedJson_ReturnsFailure(string body)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body)
        });
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/invalid");

        result.IsFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_WhenJsonContainsNoErrorMessages_UsesRawJsonAsMessage()
    {
        const string body = "{\"errors\":[],\"metadata\":{\"code\":42}}";
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(body)
        });
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/invalid");

        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldBe(body);
        result.Errors.ShouldContain(body);
    }

    [Fact]
    public async Task Get_WhenResponseContentIsNull_ReturnsFailure()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = null!
        });
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/null-content");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Failure);
        result.Message.ShouldBe("The response content was empty.");
    }

    [Theory]
    [InlineData("", "value")]
    [InlineData("X-Header", " ")]
    public async Task Get_WithInvalidAdditionalHeader_ReturnsInvalidResult(string header, string value)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/1", [(header, value)]);

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Get_WhenHeaderCannotBeAdded_ReturnsInvalidResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateWrapper(handler);

        var result = await sut.Get<SampleResponse>("widgets/1", [(":invalid", "value")]);

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldContain("could not be added");
        handler.LastRequest.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostMultipart_WithInvalidFileName_ReturnsInvalidResult(string fileName)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateWrapper(handler);

        var result = await sut.Post("uploads", [1, 2], fileName!, [], "token");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldContain("file name");
        handler.LastRequest.ShouldBeNull();
    }

    [Theory]
    [InlineData("", "value")]
    [InlineData("field", " ")]
    public async Task PostMultipart_WithInvalidFormField_ReturnsInvalidResult(string field, string value)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateWrapper(handler);

        var result = await sut.Post("uploads", [1, 2], "file.txt", [(field, value)], "token");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task PostMultipart_WithNullFileData_ReturnsInvalidResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateWrapper(handler);

        var result = await sut.Post("uploads", (byte[])null!, "file.txt", [], "token");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldContain("file data");
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task PostMultipart_WithNullFormFields_ReturnsInvalidResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateWrapper(handler);

        var result = await sut.Post(
            "uploads",
            [1],
            "file.txt",
            (List<(string field, string value)>)null!,
            "token");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldContain("form fields");
        handler.LastRequest.ShouldBeNull();
    }

    [Theory]
    [InlineData(" ", "token", "X-Test", "value")]
    [InlineData("uploads", " ", "X-Test", "value")]
    [InlineData("uploads", "token", "", "value")]
    public async Task PostMultipart_WithInvalidRequestParameters_ReturnsInvalidResult(
        string requestUri,
        string accessToken,
        string header,
        string value)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateWrapper(handler);

        var result = await sut.Post(
            requestUri,
            [1],
            "file.txt",
            [],
            accessToken,
            [(header, value)]);

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        handler.LastRequest.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.Created)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.NoContent)]
    public async Task Post_MapsSuccessfulStatuses(HttpStatusCode statusCode)
    {
        var sut = CreateWrapper(new RecordingHandler(_ => new HttpResponseMessage(statusCode)));

        var result = await sut.Post("widgets", new SampleRequest("new"));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task PostGeneric_WithRequestAndResponse_ReturnsDeserializedData()
    {
        var expected = new SampleResponse(12, "created");
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("{\"id\":12,\"name\":\"created\"}")
        });
        var sut = CreateWrapper(handler, deserialise: (_, _) => expected);

        var result = await sut.Post<SampleRequest, SampleResponse>("widgets", new SampleRequest("created"));

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(expected);
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Post);
        handler.LastRequest.Body.ShouldBe("{}");
    }

    [Fact]
    public async Task PutGeneric_WithHttpContentAndResponse_ReturnsDeserializedData()
    {
        var expected = new SampleResponse(13, "updated");
        using var content = new StringContent("raw-put");
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":13,\"name\":\"updated\"}")
        });
        var sut = CreateWrapper(handler, deserialise: (_, _) => expected);

        var result = await sut.Put<SampleResponse>("widgets/13", content);

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(expected);
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Put);
        handler.LastRequest.Body.ShouldBe("raw-put");
    }

    [Theory]
    [InlineData(ContentRequestFailure.InvalidUri)]
    [InlineData(ContentRequestFailure.InvalidToken)]
    [InlineData(ContentRequestFailure.InvalidHeader)]
    [InlineData(ContentRequestFailure.NullContent)]
    public async Task GenericContentResponseOverload_ReturnsRequestValidationFailure(ContentRequestFailure failure)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        var sut = CreateWrapper(handler);
        string requestUri = failure == ContentRequestFailure.InvalidUri ? " " : "widgets/invalid";
        string accessToken = failure == ContentRequestFailure.InvalidToken ? " " : "token";
        List<(string header, string value)>? headers = failure == ContentRequestFailure.InvalidHeader
            ? [("", "value")]
            : null;
        HttpContent content = failure == ContentRequestFailure.NullContent
            ? null!
            : new StringContent("raw");

        var result = await sut.Put<SampleResponse>(requestUri, content, accessToken, headers!);

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task PatchGeneric_WithRequestAndResponseWithoutToken_ReturnsDeserializedData()
    {
        var expected = new SampleResponse(14, "patched");
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":14,\"name\":\"patched\"}")
        });
        var sut = CreateWrapper(handler, deserialise: (_, _) => expected);

        var result = await sut.Patch<SampleRequest, SampleResponse>("widgets/14", new SampleRequest("patched"));

        result.IsSuccess.ShouldBeTrue();
        result.Data.ShouldBe(expected);
        handler.LastRequest!.Method.ShouldBe(HttpMethod.Patch);
        handler.LastRequest.Body.ShouldBe("{}");
    }

    [Fact]
    public async Task PutAndPatchWithoutResponse_UseExpectedMethods()
    {
        var requests = new List<CapturedRequest>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = CreateWrapper(handler);

        var put = await sut.Put("widgets/15", new SampleRequest("put"));
        requests.Add(handler.LastRequest!);
        var patch = await sut.Patch("widgets/15", new SampleRequest("patch"));
        requests.Add(handler.LastRequest!);

        put.IsSuccess.ShouldBeTrue();
        patch.IsSuccess.ShouldBeTrue();
        requests.Select(request => request.Method).ShouldBe([HttpMethod.Put, HttpMethod.Patch]);
    }

    [Fact]
    public async Task Get_WhenHandlerThrowsHttpRequestException_ReturnsCriticalError()
    {
        var sut = CreateWrapper(new RecordingHandler(_ => throw new HttpRequestException("network unavailable")));

        var result = await sut.Get<SampleResponse>("widgets/1");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.CriticalError);
        result.Message.ShouldBe("network unavailable");
    }

    [Fact]
    public async Task Get_WhenHandlerThrowsInvalidOperationException_ReturnsFailure()
    {
        var sut = CreateWrapper(new RecordingHandler(_ => throw new InvalidOperationException("invalid request state")));

        var result = await sut.Get<SampleResponse>("widgets/1");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Failure);
        result.Message.ShouldBe("invalid request state");
    }

    [Fact]
    public async Task PostWithoutResponse_WithEmptyContent_ReturnsInvalidResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var sut = CreateWrapper(handler);

        var result = await sut.Post("widgets/raw", (HttpContent)null!);

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldContain("content cannot be null");
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task PostMultipart_WithInvalidToken_ReturnsInvalidResult()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var sut = CreateWrapper(handler);

        var result = await sut.Post("uploads", [1], "file.txt", [], " ");

        result.IsFailed.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.Message.ShouldContain("Access token");
        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Get_WhenCallerCancelsRequest_PropagatesCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new RecordingHandler(_ =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        });
        var sut = CreateWrapper(handler);

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.Get<SampleResponse>("widgets/1", cancellation.Token));
    }

    [Theory]
    [InlineData(RequestPipeline.RequestBody, HandlerFailure.HttpRequest, ResultStatus.CriticalError)]
    [InlineData(RequestPipeline.RequestBody, HandlerFailure.InvalidOperation, ResultStatus.Failure)]
    [InlineData(RequestPipeline.RequestBody, HandlerFailure.Timeout, ResultStatus.CriticalError)]
    [InlineData(RequestPipeline.RawContent, HandlerFailure.HttpRequest, ResultStatus.CriticalError)]
    [InlineData(RequestPipeline.RawContent, HandlerFailure.InvalidOperation, ResultStatus.Failure)]
    [InlineData(RequestPipeline.RawContent, HandlerFailure.Timeout, ResultStatus.CriticalError)]
    [InlineData(RequestPipeline.GenericContent, HandlerFailure.HttpRequest, ResultStatus.CriticalError)]
    [InlineData(RequestPipeline.GenericContent, HandlerFailure.InvalidOperation, ResultStatus.Failure)]
    [InlineData(RequestPipeline.GenericContent, HandlerFailure.Timeout, ResultStatus.CriticalError)]
    [InlineData(RequestPipeline.Multipart, HandlerFailure.HttpRequest, ResultStatus.CriticalError)]
    [InlineData(RequestPipeline.Multipart, HandlerFailure.InvalidOperation, ResultStatus.Failure)]
    [InlineData(RequestPipeline.Multipart, HandlerFailure.Timeout, ResultStatus.CriticalError)]
    public async Task RequestPipelines_MapHandlerExceptions(
        RequestPipeline pipeline,
        HandlerFailure failure,
        ResultStatus expectedStatus)
    {
        var handler = new RecordingHandler(_ => throw CreateHandlerException(failure));
        var sut = CreateWrapper(handler);

        ResultStatus resultStatus = await InvokePipeline(pipeline, sut, CancellationToken.None);

        resultStatus.ShouldBe(expectedStatus);
    }

    [Theory]
    [InlineData(RequestPipeline.RequestBody)]
    [InlineData(RequestPipeline.RawContent)]
    [InlineData(RequestPipeline.GenericContent)]
    [InlineData(RequestPipeline.Multipart)]
    public async Task RequestPipelines_PropagateCallerCancellation(RequestPipeline pipeline)
    {
        using var cancellation = new CancellationTokenSource();
        var handler = new RecordingHandler(_ =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        });
        var sut = CreateWrapper(handler);

        await Should.ThrowAsync<OperationCanceledException>(
            () => InvokePipeline(pipeline, sut, cancellation.Token));
    }

    [Fact]
    public async Task DeleteOverloads_AllSendSuccessfulRequests()
    {
        var sut = CreateSuccessWrapper();
        var headers = new List<(string header, string value)> { ("X-Test", "value") };
        (string Name, Func<Task<bool>> Execute)[] operations =
        [
            ("without token or headers", async () => (await sut.Delete("widgets")).IsSuccess),
            ("with headers", async () => (await sut.Delete("widgets", headers)).IsSuccess),
            ("with token", async () => (await sut.Delete("widgets", "token")).IsSuccess),
            ("with token and headers", async () => (await sut.Delete("widgets", "token", headers)).IsSuccess)
        ];

        foreach ((string name, Func<Task<bool>> execute) in operations)
        {
            (await execute()).ShouldBeTrue(name);
        }
    }

    [Fact]
    public async Task PostOverloadsWithoutResponse_AllSendSuccessfulRequests()
    {
        var sut = CreateSuccessWrapper();
        var headers = new List<(string header, string value)> { ("X-Test", "value") };
        (string Name, Func<Task<bool>> Execute)[] operations =
        [
            ("body without token or headers", async () => (await sut.Post("widgets", new SampleRequest("x"))).IsSuccess),
            ("body with headers", async () => (await sut.Post("widgets", new SampleRequest("x"), headers)).IsSuccess),
            ("body with token", async () => (await sut.Post("widgets", new SampleRequest("x"), "token")).IsSuccess),
            ("body with token and headers", async () => (await sut.Post("widgets", new SampleRequest("x"), "token", headers)).IsSuccess),
            ("content without token or headers", async () => (await sut.Post("widgets", (HttpContent)new StringContent("raw"))).IsSuccess),
            ("content with headers", async () => (await sut.Post("widgets", (HttpContent)new StringContent("raw"), headers)).IsSuccess),
            ("content with token", async () => (await sut.Post("widgets", (HttpContent)new StringContent("raw"), "token")).IsSuccess),
            ("content with token and headers", async () => (await sut.Post("widgets", (HttpContent)new StringContent("raw"), "token", headers)).IsSuccess)
        ];

        foreach ((string name, Func<Task<bool>> execute) in operations)
        {
            (await execute()).ShouldBeTrue(name);
        }
    }

    [Fact]
    public async Task PutOverloadsWithoutResponse_AllSendSuccessfulRequests()
    {
        var sut = CreateSuccessWrapper();
        var headers = new List<(string header, string value)> { ("X-Test", "value") };
        (string Name, Func<Task<bool>> Execute)[] operations =
        [
            ("body without token or headers", async () => (await sut.Put("widgets", new SampleRequest("x"))).IsSuccess),
            ("body with headers", async () => (await sut.Put("widgets", new SampleRequest("x"), headers)).IsSuccess),
            ("body with token", async () => (await sut.Put("widgets", new SampleRequest("x"), "token")).IsSuccess),
            ("body with token and headers", async () => (await sut.Put("widgets", new SampleRequest("x"), "token", headers)).IsSuccess),
            ("content without token or headers", async () => (await sut.Put("widgets", (HttpContent)new StringContent("raw"))).IsSuccess),
            ("content with headers", async () => (await sut.Put("widgets", (HttpContent)new StringContent("raw"), headers)).IsSuccess),
            ("content with token", async () => (await sut.Put("widgets", (HttpContent)new StringContent("raw"), "token")).IsSuccess),
            ("content with token and headers", async () => (await sut.Put("widgets", (HttpContent)new StringContent("raw"), "token", headers)).IsSuccess)
        ];

        foreach ((string name, Func<Task<bool>> execute) in operations)
        {
            (await execute()).ShouldBeTrue(name);
        }
    }

    [Fact]
    public async Task PatchOverloadsWithoutResponse_AllSendSuccessfulRequests()
    {
        var sut = CreateSuccessWrapper();
        var headers = new List<(string header, string value)> { ("X-Test", "value") };
        (string Name, Func<Task<bool>> Execute)[] operations =
        [
            ("body without token or headers", async () => (await sut.Patch("widgets", new SampleRequest("x"))).IsSuccess),
            ("body with headers", async () => (await sut.Patch("widgets", new SampleRequest("x"), headers)).IsSuccess),
            ("body with token", async () => (await sut.Patch("widgets", new SampleRequest("x"), "token")).IsSuccess),
            ("body with token and headers", async () => (await sut.Patch("widgets", new SampleRequest("x"), "token", headers)).IsSuccess),
            ("content without token or headers", async () => (await sut.Patch("widgets", (HttpContent)new StringContent("raw"))).IsSuccess),
            ("content with headers", async () => (await sut.Patch("widgets", (HttpContent)new StringContent("raw"), headers)).IsSuccess),
            ("content with token", async () => (await sut.Patch("widgets", (HttpContent)new StringContent("raw"), "token")).IsSuccess),
            ("content with token and headers", async () => (await sut.Patch("widgets", (HttpContent)new StringContent("raw"), "token", headers)).IsSuccess)
        ];

        foreach ((string name, Func<Task<bool>> execute) in operations)
        {
            (await execute()).ShouldBeTrue(name);
        }
    }

    [Fact]
    public async Task GenericResponseOverloads_AllSendSuccessfulRequests()
    {
        var sut = CreateSuccessWrapper();
        var headers = new List<(string header, string value)> { ("X-Test", "value") };
        SampleRequest body = new("x");
        (string Name, Func<Task<bool>> Execute)[] operations =
        [
            ("post body without token or headers", async () => (await sut.Post<SampleRequest, SampleResponse>("widgets", body)).IsSuccess),
            ("post body with headers", async () => (await sut.Post<SampleRequest, SampleResponse>("widgets", body, headers)).IsSuccess),
            ("post body with token", async () => (await sut.Post<SampleRequest, SampleResponse>("widgets", body, "token")).IsSuccess),
            ("post body with token and headers", async () => (await sut.Post<SampleRequest, SampleResponse>("widgets", body, "token", headers)).IsSuccess),
            ("put body without token or headers", async () => (await sut.Put<SampleRequest, SampleResponse>("widgets", body)).IsSuccess),
            ("put body with headers", async () => (await sut.Put<SampleRequest, SampleResponse>("widgets", body, headers)).IsSuccess),
            ("put body with token", async () => (await sut.Put<SampleRequest, SampleResponse>("widgets", body, "token")).IsSuccess),
            ("put body with token and headers", async () => (await sut.Put<SampleRequest, SampleResponse>("widgets", body, "token", headers)).IsSuccess),
            ("patch body without token or headers", async () => (await sut.Patch<SampleRequest, SampleResponse>("widgets", body)).IsSuccess),
            ("patch body with headers", async () => (await sut.Patch<SampleRequest, SampleResponse>("widgets", body, headers)).IsSuccess),
            ("patch body with token", async () => (await sut.Patch<SampleRequest, SampleResponse>("widgets", body, "token")).IsSuccess),
            ("patch body with token and headers", async () => (await sut.Patch<SampleRequest, SampleResponse>("widgets", body, "token", headers)).IsSuccess),
            ("post content without token or headers", async () => (await sut.Post<SampleResponse>("widgets", new StringContent("raw"))).IsSuccess),
            ("post content with headers", async () => (await sut.Post<SampleResponse>("widgets", new StringContent("raw"), headers)).IsSuccess),
            ("post content with token", async () => (await sut.Post<SampleResponse>("widgets", new StringContent("raw"), "token")).IsSuccess),
            ("post content with token and headers", async () => (await sut.Post<SampleResponse>("widgets", new StringContent("raw"), "token", headers)).IsSuccess),
            ("put content without token or headers", async () => (await sut.Put<SampleResponse>("widgets", new StringContent("raw"))).IsSuccess),
            ("put content with headers", async () => (await sut.Put<SampleResponse>("widgets", new StringContent("raw"), headers)).IsSuccess),
            ("put content with token", async () => (await sut.Put<SampleResponse>("widgets", new StringContent("raw"), "token")).IsSuccess),
            ("put content with token and headers", async () => (await sut.Put<SampleResponse>("widgets", new StringContent("raw"), "token", headers)).IsSuccess),
            ("patch content without token or headers", async () => (await sut.Patch<SampleResponse>("widgets", new StringContent("raw"))).IsSuccess),
            ("patch content with headers", async () => (await sut.Patch<SampleResponse>("widgets", new StringContent("raw"), headers)).IsSuccess),
            ("patch content with token", async () => (await sut.Patch<SampleResponse>("widgets", new StringContent("raw"), "token")).IsSuccess),
            ("patch content with token and headers", async () => (await sut.Patch<SampleResponse>("widgets", new StringContent("raw"), "token", headers)).IsSuccess)
        ];

        foreach ((string name, Func<Task<bool>> execute) in operations)
        {
            (await execute()).ShouldBeTrue(name);
        }
    }

    [Fact]
    public async Task GenericBodyOverloads_WithHttpContent_ForwardRawContentWithoutSerializing()
    {
        var serialiseCalls = 0;
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        var sut = CreateWrapper(
            handler,
            serialise: _ =>
            {
                serialiseCalls++;
                return "serialized";
            },
            deserialise: (_, _) => new SampleResponse(1, "ok"));

        (string Name, Func<Task<bool>> Execute)[] operations =
        [
            ("post without response", async () => (await sut.Post<StringContent>("widgets", new StringContent("raw-body"))).IsSuccess),
            ("put without response", async () => (await sut.Put<StringContent>("widgets", new StringContent("raw-body"))).IsSuccess),
            ("patch without response", async () => (await sut.Patch<StringContent>("widgets", new StringContent("raw-body"))).IsSuccess),
            ("post with response", async () => (await sut.Post<HttpContent, SampleResponse>("widgets", new StringContent("raw-body"))).IsSuccess),
            ("put with response", async () => (await sut.Put<HttpContent, SampleResponse>("widgets", new StringContent("raw-body"))).IsSuccess),
            ("patch with response", async () => (await sut.Patch<HttpContent, SampleResponse>("widgets", new StringContent("raw-body"))).IsSuccess)
        ];

        foreach ((string name, Func<Task<bool>> execute) in operations)
        {
            (await execute()).ShouldBeTrue(name);
            handler.LastRequest!.Body.ShouldBe("raw-body");
        }

        serialiseCalls.ShouldBe(0);
    }

    private static TestHttpClientWrapper CreateSuccessWrapper() =>
        CreateWrapper(
            new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            }),
            deserialise: (_, _) => new SampleResponse(1, "ok"));

    private static async Task<ResultStatus> InvokePipeline(
        RequestPipeline pipeline,
        TestHttpClientWrapper sut,
        CancellationToken cancellationToken)
    {
        switch (pipeline)
        {
            case RequestPipeline.RequestBody:
                return (await sut.Post("widgets", new SampleRequest("request"), cancellationToken)).Status;
            case RequestPipeline.RawContent:
                using (var content = new StringContent("raw"))
                {
                    return (await sut.Post("widgets/raw", (HttpContent)content, cancellationToken)).Status;
                }
            case RequestPipeline.GenericContent:
                using (var content = new StringContent("raw"))
                {
                    return (await sut.Put<SampleResponse>("widgets/1", content, cancellationToken)).Status;
                }
            case RequestPipeline.Multipart:
                return (await sut.Post("uploads", [1], "file.txt", [], "token", cancellationToken)).Status;
            default:
                throw new ArgumentOutOfRangeException(nameof(pipeline), pipeline, null);
        }
    }

    private static Exception CreateHandlerException(HandlerFailure failure) =>
        failure switch
        {
            HandlerFailure.HttpRequest => new HttpRequestException("network unavailable"),
            HandlerFailure.InvalidOperation => new InvalidOperationException("invalid request state"),
            HandlerFailure.Timeout => new OperationCanceledException("timed out"),
            _ => throw new ArgumentOutOfRangeException(nameof(failure), failure, null)
        };

    public enum RequestPipeline
    {
        RequestBody,
        RawContent,
        GenericContent,
        Multipart
    }

    public enum HandlerFailure
    {
        HttpRequest,
        InvalidOperation,
        Timeout
    }

    public enum ContentRequestFailure
    {
        InvalidUri,
        InvalidToken,
        InvalidHeader,
        NullContent
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

    private sealed class TestHttpClientWrapper : TransactionProcessing.ClientProxyBase.ClientProxyBase
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
