using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using Shared.Results;
using Shouldly;
using SimpleResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Shared.Tests
{
    public class HttpService : ClientProxyBase.ClientProxyBase {
        public HttpService(HttpClient httpClient) : base(httpClient) {
        }

        public async Task<Result<TResponse>> SendHttpGetRequest<TResponse>(String uri,
                                                                                      String accessToken,
                                                                                      List<(String header, String value)> additionalHeaders,
                                                                                      CancellationToken cancellationToken) =>
            await base.SendHttpGetRequest<TResponse>(uri, accessToken, additionalHeaders, cancellationToken);

        public async Task<Result<TResponse>> SendHttpGetRequest<TResponse>(String uri,
                                                                           String accessToken,
                                                                           CancellationToken cancellationToken) =>
            await base.SendHttpGetRequest<TResponse>(uri, accessToken, null, cancellationToken);

        public async Task<Result<TResponse>> SendHttpGetRequest<TResponse>(String uri,
                                                                           List<(String header, String value)> additionalHeaders,
                                                                           CancellationToken cancellationToken) =>
            await base.SendHttpGetRequest<TResponse>(uri, null, additionalHeaders, cancellationToken);

        public async Task<Result<TResponse>> SendHttpGetRequest<TResponse>(String uri,
                                                                           CancellationToken cancellationToken) =>
            await base.SendHttpGetRequest<TResponse>(uri, null, null, cancellationToken);

        public async Task<Result> SendHttpDeleteRequest(String uri,
                                                        String accessToken,
                                                        List<(String header, String value)> additionalHeaders,
                                                        CancellationToken cancellationToken) =>
            await base.SendHttpDeleteRequest(uri, accessToken, additionalHeaders, cancellationToken);

        public async Task<Result> SendHttpDeleteRequest(String uri,
                                                                String accessToken,
                                                                CancellationToken cancellationToken) =>
            await base.SendHttpDeleteRequest(uri, accessToken, null, cancellationToken);

        public async Task<Result> SendHttpDeleteRequest(String uri,
                                                                List<(String header, String value)> additionalHeaders,
                                                                CancellationToken cancellationToken) =>
            await base.SendHttpDeleteRequest(uri, null, additionalHeaders, cancellationToken);

        public async Task<Result> SendHttpDeleteRequest(String uri,
                                                                CancellationToken cancellationToken) =>
            await base.SendHttpDeleteRequest(uri, null, null, cancellationToken);

        public async Task<Result<TRequest>> SendHttpPatchRequest<TRequest>(String uri,
                                                                                      TRequest request,
                                                                                      String accessToken,
                                                                                      List<(String header, String value)> additionalHeaders,
                                                                                      CancellationToken cancellationToken) =>
            await base.SendHttpPatchRequest<TRequest>(uri, request, accessToken, additionalHeaders, cancellationToken);


        public async Task<Result<TRequest>> SendHttpPatchRequest<TRequest>(String uri,
                                                                                   TRequest request,
                                                                                   String accessToken,
                                                                                   CancellationToken cancellationToken) =>
            await base.SendHttpPatchRequest(uri, request, accessToken, null, cancellationToken);

        public async Task<Result<TRequest>> SendHttpPatchRequest<TRequest>(String uri,
                                                                                   TRequest request,
                                                                                   List<(String header, String value)> additionalHeaders,
                                                                                   CancellationToken cancellationToken) =>
            await base.SendHttpPatchRequest(uri, request, null, additionalHeaders, cancellationToken);

        public async Task<Result<TRequest>> SendHttpPatchRequest<TRequest>(String uri,
                                                                                   TRequest request,
                                                                                   CancellationToken cancellationToken) =>
            await base.SendHttpPatchRequest(uri, request, null, null, cancellationToken);

        public async Task<Result<TRequest>> SendHttpPutRequest<TRequest>(String uri,
                                                                         TRequest request,
                                                                         String accessToken,
                                                                         List<(String header, String value)> additionalHeaders,
                                                                         CancellationToken cancellationToken) =>
            await base.SendHttpPutRequest<TRequest>(uri, request, accessToken, additionalHeaders, cancellationToken);
        
        public async Task<Result<TRequest>> SendHttpPutRequest<TRequest>(String uri,
                                                                                   TRequest request,
                                                                                   String accessToken,
                                                                                   CancellationToken cancellationToken) =>
            await base.SendHttpPutRequest(uri, request, accessToken, null, cancellationToken);

        public async Task<Result<TRequest>> SendHttpPutRequest<TRequest>(String uri,
                                                                                   TRequest request,
                                                                                   List<(String header, String value)> additionalHeaders,
                                                                                   CancellationToken cancellationToken) =>
            await base.SendHttpPutRequest(uri, request, null, additionalHeaders, cancellationToken);

        public async Task<Result<TRequest>> SendHttpPutRequest<TRequest>(String uri,
                                                                                   TRequest request,
                                                                                   CancellationToken cancellationToken) =>
            await base.SendHttpPutRequest(uri, request, null, null, cancellationToken);

        public async Task<Result<TResponse>> SendHttpPostRequest<TResponse>(String uri,
                                                                            String accessToken,
                                                                            List<(String header, String value)> additionalHeaders,
                                                                            CancellationToken cancellationToken) =>
            await base.SendHttpPostRequest<TResponse>(uri, accessToken, additionalHeaders, cancellationToken);

        public async Task<Result<TResponse>> SendHttpPostRequest<TResponse>(String uri, String accessToken, CancellationToken cancellationToken) =>
                    await base.SendHttpPostRequest<TResponse>(uri, accessToken, null, cancellationToken);

        public async Task<Result<TResponse>> SendHttpPostRequest<TResponse>(String uri, List<(String header, String value)> additionalHeaders, CancellationToken cancellationToken) =>
                            await base.SendHttpPostRequest<TResponse>(uri, null, additionalHeaders, cancellationToken);

        public async Task<Result<TResponse>> SendHttpPostRequest<TResponse>(String uri, CancellationToken cancellationToken) =>
                                        await base.SendHttpPostRequest<TResponse>(uri, null, null, cancellationToken);


        public async Task<Result<TResponse>> SendHttpPostRequest<TRequest, TResponse>(String uri,
                                                                                      TRequest request,
                                                                                      String accessToken,
                                                                                      List<(String header, String value)> additionalHeaders,
                                                                                      CancellationToken cancellationToken) 
        =>await base.SendHttpPostRequest<TRequest, TResponse>(uri, request, accessToken, additionalHeaders, cancellationToken);

        public async Task<Result<TResponse>> SendHttpPostRequest<TRequest, TResponse>(String uri, TRequest request, String accessToken, CancellationToken cancellationToken) =>
            await base.SendHttpPostRequest<TRequest, TResponse>(uri, request, accessToken, null, cancellationToken);

        public async Task<Result<TResponse>> SendHttpPostRequest<TRequest, TResponse>(String uri, TRequest request, List<(String header, String value)> additionalHeaders, CancellationToken cancellationToken) =>
            await base.SendHttpPostRequest<TRequest, TResponse>(uri, request, null, additionalHeaders, cancellationToken);

        public async Task<Result<TResponse>> SendHttpPostRequest<TRequest, TResponse>(String uri, TRequest request, CancellationToken cancellationToken) =>
            await base.SendHttpPostRequest<TRequest, TResponse>(uri, request, null, null, cancellationToken);

        public async Task<Result> SendHttpPostRequest(String uri,
                                                      String accessToken,
                                                      List<(String header, String value)> additionalHeaders,
                                                      CancellationToken cancellationToken) =>
            await base.SendHttpPostRequest(uri, accessToken, additionalHeaders, cancellationToken);

        public async Task<Result> SendHttpPostRequest(String uri, String accessToken, CancellationToken cancellationToken) =>
            await base.SendHttpPostRequest(uri, accessToken, null, cancellationToken);

        public async Task<Result> SendHttpPostRequest(String uri, List<(String header, String value)> additionalHeaders, CancellationToken cancellationToken) =>
            await base.SendHttpPostRequest(uri, null, additionalHeaders, cancellationToken);

        public async Task<Result> SendHttpPostRequest(String uri, CancellationToken cancellationToken) =>
            await base.SendHttpPostRequest(uri, null, null, cancellationToken);

    }
    public class SampleResponse
    {
        public string Message { get; set; }
        public int Value { get; set; }
    }

    public class SampleRequest
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class HttpServiceTests
    {
        private readonly MockHttpMessageHandler _mockHttp;
        private readonly HttpService _service;

        public HttpServiceTests()
        {
            _mockHttp = new MockHttpMessageHandler();
            var client = _mockHttp.ToHttpClient();
            _service = new HttpService(client);
        }

        [Fact]
        public async Task SendHttpGetRequest_SuccessfulResponse_ShouldReturnTypedObject()
        {
            var payload = new SampleResponse { Message = "Hello", Value = 42 };
            _mockHttp.When(HttpMethod.Get, "https://api/success")
                     .Respond("application/json", JsonConvert.SerializeObject(payload));

            var result = await _service.SendHttpGetRequest<SampleResponse>(
                "https://api/success", "token", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Data.Message.ShouldBe("Hello");
            result.Data.Value.ShouldBe(42);
        }

        [Fact]
        public async Task SendHttpGetRequest_ShouldAddBearerHeader_WhenAccessTokenPresent()
        {
            // Arrange
            const string expectedToken = "token123";
            const string expectedUri = "https://api/auth";

            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Get, expectedUri)
                     .WithHeaders("Authorization", $"Bearer {expectedToken}")
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new SampleResponse
                             {
                                 Message = "Authorized",
                                 Value = 1
                             }), System.Text.Encoding.UTF8, "application/json")
                         };
                     });

            // Act
            var result = await _service.SendHttpGetRequest<SampleResponse>(
                expectedUri, expectedToken, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            _mockHttp.VerifyNoOutstandingExpectation();

            capturedRequest.ShouldNotBeNull();
            capturedRequest.Headers.Authorization.ShouldNotBeNull();
            capturedRequest.Headers.Authorization.Scheme.ShouldBe("Bearer");
            capturedRequest.Headers.Authorization.Parameter.ShouldBe(expectedToken);
        }

        [Fact]
        public async Task SendHttpGetRequest_ShouldAddAllAdditionalHeaders_ToRequest()
        {
            const string expectedUri = "https://api/headers";
            var headers = new List<(string, string)>
        {
            ("X-Test-Header", "Value1"),
            ("X-Custom", "12345")
        };

            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Get, expectedUri)
                     .WithHeaders("X-Test-Header", "Value1")
                     .WithHeaders("X-Custom", "12345")
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new SampleResponse
                             {
                                 Message = "Header check",
                                 Value = 1
                             }), System.Text.Encoding.UTF8, "application/json")
                         };
                     });

            var result = await _service.SendHttpGetRequest<SampleResponse>(
                expectedUri, headers, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Data.Message.ShouldBe("Header check");
            _mockHttp.VerifyNoOutstandingExpectation();

            capturedRequest.ShouldNotBeNull();
            capturedRequest.Headers.Contains("X-Test-Header").ShouldBeTrue();
            capturedRequest.Headers.GetValues("X-Test-Header").ShouldContain("Value1");
            capturedRequest.Headers.Contains("X-Custom").ShouldBeTrue();
            capturedRequest.Headers.GetValues("X-Custom").ShouldContain("12345");
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
        [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
        [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
        [InlineData(HttpStatusCode.Conflict, ResultStatus.Conflict)]
        [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
        [InlineData(HttpStatusCode.GatewayTimeout, ResultStatus.Failure)]
        public async Task SendHttpGetRequest_FailureBranches_ShouldReturnFailure(HttpStatusCode code, ResultStatus expectedStatus)
        {
            _mockHttp.When(HttpMethod.Get, "https://api/fail")
                     .Respond(code, "application/json", JsonConvert.SerializeObject(new { Text = "Error" }));

            var result = await _service.SendHttpGetRequest<SampleResponse>("https://api/fail", CancellationToken.None);

            result.IsFailed.ShouldBeTrue();
            result.Status.ShouldBe(expectedStatus);
        }

        [Fact]
        public async Task SendHttpGetRequest_SuccessfulResponse_ShouldReturnSuccess()
        {
            var data = new SampleResponse { Message = "OK", Value = 10 };
            _mockHttp.When(HttpMethod.Get, "https://api/ok")
                     .Respond(HttpStatusCode.OK, "application/json", JsonConvert.SerializeObject(data));

            var result = await _service.SendHttpGetRequest<SampleResponse>(
                "https://api/ok", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Data.Message.ShouldBe("OK");
            result.Data.Value.ShouldBe(10);
        }

        [Fact]
        public async Task SendHttpGetRequest_AllOverloads_ShouldCallBaseMethod()
        {
            var data = new SampleResponse { Message = "World", Value = 1 };
            _mockHttp.When(HttpMethod.Get, "https://api/base")
                     .Respond("application/json", JsonConvert.SerializeObject(data));

            var r1 = await _service.SendHttpGetRequest<SampleResponse>("https://api/base", "token", CancellationToken.None);
            var r2 = await _service.SendHttpGetRequest<SampleResponse>("https://api/base", new List<(string, string)>(), CancellationToken.None);
            var r3 = await _service.SendHttpGetRequest<SampleResponse>("https://api/base", CancellationToken.None);

            r1.IsSuccess.ShouldBeTrue();
            r2.IsSuccess.ShouldBeTrue();
            r3.IsSuccess.ShouldBeTrue();

            r1.Data.Message.ShouldBe("World");
            r2.Data.Message.ShouldBe("World");
            r3.Data.Message.ShouldBe("World");
        }

        [Fact]
        public async Task SendHttpDeleteRequest_SuccessfulResponse_ShouldReturnSuccess()
        {
            _mockHttp.When(HttpMethod.Delete, "https://api/delete-success")
                     .Respond(HttpStatusCode.OK, "application/json", JsonConvert.SerializeObject(new { message = "deleted" }));

            var result = await _service.SendHttpDeleteRequest("https://api/delete-success", CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task SendHttpDeleteRequest_ShouldAddBearerHeader_WhenAccessTokenPresent()
        {
            const string expectedToken = "delete-token-123";
            const string expectedUri = "https://api/delete-auth";

            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Delete, expectedUri)
                     .WithHeaders("Authorization", $"Bearer {expectedToken}")
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { deleted = true }), System.Text.Encoding.UTF8, "application/json")
                         };
                     });

            var result = await _service.SendHttpDeleteRequest(expectedUri, expectedToken, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            capturedRequest.ShouldNotBeNull();
            capturedRequest.Headers.Authorization.ShouldNotBeNull();
            capturedRequest.Headers.Authorization.Scheme.ShouldBe("Bearer");
            capturedRequest.Headers.Authorization.Parameter.ShouldBe(expectedToken);
        }

        [Fact]
        public async Task SendHttpDeleteRequest_ShouldAddAllAdditionalHeaders_ToRequest()
        {
            const string expectedUri = "https://api/delete-headers";
            var headers = new List<(string, string)>
        {
            ("X-Test-Delete", "Del1"),
            ("X-Delete-Custom", "XYZ")
        };

            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Delete, expectedUri)
                     .WithHeaders("X-Test-Delete", "Del1")
                     .WithHeaders("X-Delete-Custom", "XYZ")
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { deleted = true }), System.Text.Encoding.UTF8, "application/json")
                         };
                     });

            var result = await _service.SendHttpDeleteRequest(expectedUri, headers, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            _mockHttp.VerifyNoOutstandingExpectation();

            capturedRequest.ShouldNotBeNull();
            capturedRequest.Headers.Contains("X-Test-Delete").ShouldBeTrue();
            capturedRequest.Headers.GetValues("X-Test-Delete").ShouldContain("Del1");
            capturedRequest.Headers.Contains("X-Delete-Custom").ShouldBeTrue();
            capturedRequest.Headers.GetValues("X-Delete-Custom").ShouldContain("XYZ");
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
        [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
        [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
        [InlineData(HttpStatusCode.Conflict, ResultStatus.Conflict)]
        [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
        [InlineData(HttpStatusCode.GatewayTimeout, ResultStatus.Failure)]
        public async Task SendHttpDeleteRequest_FailureBranches_ShouldReturnFailure(HttpStatusCode code, ResultStatus expectedStatus)
        {
            _mockHttp.When(HttpMethod.Delete, "https://api/delete-fail")
                     .Respond(code, "application/json", JsonConvert.SerializeObject(new { Text = "Error" }));

            var result = await _service.SendHttpDeleteRequest("https://api/delete-fail", CancellationToken.None);

            result.IsFailed.ShouldBeTrue();
            result.Status.ShouldBe(expectedStatus);
        }

        [Fact]
        public async Task SendHttpDeleteRequest_AllDeleteOverloads_ShouldCallBaseMethod()
        {
            const string uri = "https://api/delete-overload";
            _mockHttp.When(HttpMethod.Delete, uri)
                     .Respond(HttpStatusCode.OK, "application/json", JsonConvert.SerializeObject(new { ok = true }));

            var r1 = await _service.SendHttpDeleteRequest(uri, "token", CancellationToken.None);
            var r2 = await _service.SendHttpDeleteRequest(uri, new List<(string, string)>(), CancellationToken.None);
            var r3 = await _service.SendHttpDeleteRequest(uri, CancellationToken.None);

            r1.IsSuccess.ShouldBeTrue();
            r2.IsSuccess.ShouldBeTrue();
            r3.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task SendHttpPutRequest_ShouldSendJsonBody_WhenRequestIsObject()
        {
            var uri = "https://api/put-json";
            var requestObject = new SampleRequest { Name = "Test", Id = 123 };

            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Put, uri)
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { message = "ok" }),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPutRequest(uri, requestObject, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();

            capturedRequest.ShouldNotBeNull();
            capturedRequest.Content.ShouldNotBeNull();
            var body = await capturedRequest.Content.ReadAsStringAsync();
            body.ShouldContain("\"Name\":\"Test\"");
            body.ShouldContain("\"Id\":123");
            capturedRequest.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
        }

        [Fact]
        public async Task SendHttpPutRequest_ShouldUseFormUrlEncodedContent_WhenRequestIsFormContent()
        {
            var uri = "https://api/put-form";
            var formContent = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("username", "admin"),
            new KeyValuePair<string, string>("password", "1234")
        });

            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Put, uri)
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { ok = true }),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPutRequest(uri, formContent, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            capturedRequest.ShouldNotBeNull();
            capturedRequest.Content.ShouldBeOfType<FormUrlEncodedContent>();
        }

        [Fact]
        public async Task SendHttpPutRequest_ShouldAddBearerHeader_WhenAccessTokenPresent()
        {
            const string expectedToken = "put-token";
            const string uri = "https://api/put-auth";
            var payload = new SampleRequest { Name = "Authorized", Id = 1 };
            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Put, uri)
                     .WithHeaders("Authorization", $"Bearer {expectedToken}")
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { ok = true }),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPutRequest(uri, payload, expectedToken, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            capturedRequest.Headers.Authorization.ShouldNotBeNull();
            capturedRequest.Headers.Authorization.Scheme.ShouldBe("Bearer");
            capturedRequest.Headers.Authorization.Parameter.ShouldBe(expectedToken);
        }

        [Fact]
        public async Task SendHttpPutRequest_ShouldAddAllAdditionalHeaders_ToRequest()
        {
            const string uri = "https://api/put-headers";
            var headers = new List<(string, string)>
        {
            ("X-Put-Test", "1"),
            ("X-Custom-Header", "ABC")
        };

            var payload = new SampleRequest { Name = "HeaderCheck", Id = 55 };
            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Put, uri)
                     .WithHeaders("X-Put-Test", "1")
                     .WithHeaders("X-Custom-Header", "ABC")
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { ok = true }),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPutRequest(uri, payload, headers, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            _mockHttp.VerifyNoOutstandingExpectation();

            capturedRequest.Headers.Contains("X-Put-Test").ShouldBeTrue();
            capturedRequest.Headers.GetValues("X-Custom-Header").ShouldContain("ABC");
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
        [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
        [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
        [InlineData(HttpStatusCode.Conflict, ResultStatus.Conflict)]
        [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
        [InlineData(HttpStatusCode.GatewayTimeout, ResultStatus.Failure)]
        public async Task SendHttpPutRequest_FailureBranches_ShouldReturnFailure(HttpStatusCode code, ResultStatus expectedStatus)
        {
            const string uri = "https://api/put-fail";
            var payload = new SampleRequest { Name = "Fail", Id = 99 };

            _mockHttp.When(HttpMethod.Put, uri)
                     .Respond(code, "application/json", JsonConvert.SerializeObject(new { Text = "Error" }));

            var result = await _service.SendHttpPutRequest(uri, payload, CancellationToken.None);

            result.IsFailed.ShouldBeTrue();
            result.Status.ShouldBe(expectedStatus);
        }

        [Fact]
        public async Task SendHttpPutRequest_AllOverloads_ShouldCallBaseMethod()
        {
            const string uri = "https://api/put-overload";
            var payload = new SampleRequest { Name = "Overload", Id = 1 };

            _mockHttp.When(HttpMethod.Put, uri)
                     .Respond(HttpStatusCode.OK, "application/json", JsonConvert.SerializeObject(new { ok = true }));

            var r1 = await _service.SendHttpPutRequest(uri, payload, "token", CancellationToken.None);
            var r2 = await _service.SendHttpPutRequest(uri, payload, new List<(string, string)>(), CancellationToken.None);
            var r3 = await _service.SendHttpPutRequest(uri, payload, CancellationToken.None);

            r1.IsSuccess.ShouldBeTrue();
            r2.IsSuccess.ShouldBeTrue();
            r3.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public async Task SendHttpPatchRequest_ShouldSendJsonBody_WhenRequestIsObject()
        {
            var uri = "https://api/patch-json";
            var requestObject = new SampleRequest { Name = "PatchTest", Id = 101 };

            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Patch, uri)
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { message = "ok" }),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPatchRequest(uri, requestObject, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();

            capturedRequest.ShouldNotBeNull();
            capturedRequest.Content.ShouldNotBeNull();
            var body = await capturedRequest.Content.ReadAsStringAsync();
            body.ShouldContain("\"Name\":\"PatchTest\"");
            body.ShouldContain("\"Id\":101");
            capturedRequest.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
        }

        [Fact]
        public async Task SendHttpPatchRequest_ShouldUseFormUrlEncodedContent_WhenRequestIsFormContent()
        {
            var uri = "https://api/patch-form";
            var formContent = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("username", "patcher"),
            new KeyValuePair<string, string>("password", "secret")
        });

            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Patch, uri)
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { ok = true }),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPatchRequest(uri, formContent, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            capturedRequest.ShouldNotBeNull();
            capturedRequest.Content.ShouldBeOfType<FormUrlEncodedContent>();
        }

        [Fact]
        public async Task SendHttpPatchRequest_ShouldAddBearerHeader_WhenAccessTokenPresent()
        {
            const string expectedToken = "patch-token";
            const string uri = "https://api/patch-auth";
            var payload = new SampleRequest { Name = "AuthorizedPatch", Id = 999 };
            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Patch, uri)
                     .WithHeaders("Authorization", $"Bearer {expectedToken}")
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { ok = true }),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPatchRequest(uri, payload, expectedToken, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            capturedRequest.Headers.Authorization.ShouldNotBeNull();
            capturedRequest.Headers.Authorization.Scheme.ShouldBe("Bearer");
            capturedRequest.Headers.Authorization.Parameter.ShouldBe(expectedToken);
        }

        [Fact]
        public async Task SendHttpPatchRequest_ShouldAddAllAdditionalHeaders_ToRequest()
        {
            const string uri = "https://api/patch-headers";
            var headers = new List<(string, string)>
        {
            ("X-Patch-Test", "PATCH1"),
            ("X-Custom-Header", "XYZ")
        };

            var payload = new SampleRequest { Name = "HeaderPatch", Id = 45 };
            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Patch, uri)
                     .WithHeaders("X-Patch-Test", "PATCH1")
                     .WithHeaders("X-Custom-Header", "XYZ")
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(new { ok = true }),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPatchRequest(uri, payload, headers, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            _mockHttp.VerifyNoOutstandingExpectation();

            capturedRequest.Headers.Contains("X-Patch-Test").ShouldBeTrue();
            capturedRequest.Headers.GetValues("X-Custom-Header").ShouldContain("XYZ");
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
        [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
        [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
        [InlineData(HttpStatusCode.Conflict, ResultStatus.Conflict)]
        [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
        [InlineData(HttpStatusCode.GatewayTimeout, ResultStatus.Failure)]
        public async Task SendHttpPatchRequest_FailureBranches_ShouldReturnFailure(HttpStatusCode code, ResultStatus expectedStatus)
        {
            const string uri = "https://api/patch-fail";
            var payload = new SampleRequest { Name = "FailPatch", Id = 88 };

            _mockHttp.When(HttpMethod.Patch, uri)
                     .Respond(code, "application/json", JsonConvert.SerializeObject(new { Text = "Error" }));

            var result = await _service.SendHttpPatchRequest(uri, payload, CancellationToken.None);

            result.IsFailed.ShouldBeTrue();
            result.Status.ShouldBe(expectedStatus);
        }

        [Fact]
        public async Task SendHttpPatchRequest_AllOverloads_ShouldCallBaseMethod()
        {
            const string uri = "https://api/patch-overload";
            var payload = new SampleRequest { Name = "OverloadPatch", Id = 1 };

            _mockHttp.When(HttpMethod.Patch, uri)
                     .Respond(HttpStatusCode.OK, "application/json", JsonConvert.SerializeObject(new { ok = true }));

            var r1 = await _service.SendHttpPatchRequest(uri, payload, "token", CancellationToken.None);
            var r2 = await _service.SendHttpPatchRequest(uri, payload, new List<(string, string)>(), CancellationToken.None);
            var r3 = await _service.SendHttpPatchRequest(uri, payload, CancellationToken.None);

            r1.IsSuccess.ShouldBeTrue();
            r2.IsSuccess.ShouldBeTrue();
            r3.IsSuccess.ShouldBeTrue();
        }

        // -----------------------------
        // POST<TResponse> tests
        // -----------------------------
        [Fact]
        public async Task SendHttpPostRequest_TResponse_ShouldReturnDeserializedResponse()
        {
            const string uri = "https://api/post-generic-response";

            var expected = new SampleResponse { Message = "Success", Value = 200 };

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(HttpStatusCode.OK,
                              "application/json",
                              JsonConvert.SerializeObject(expected));

            var result = await _service.SendHttpPostRequest<SampleResponse>(uri, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Data.Message.ShouldBe("Success");
            result.Data.Value.ShouldBe(200);
        }

        [Fact]
        public async Task SendHttpPostRequest_TResponse_ShouldIncludeBearerHeader_WhenAccessTokenProvided()
        {
            const string uri = "https://api/post-bearer";
            const string token = "post-token";
            HttpRequestMessage capturedRequest = null;

            var expected = new SampleResponse { Message = "OK", Value = 123 };

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(expected),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPostRequest<SampleResponse>(uri, token, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            capturedRequest.Headers.Authorization.ShouldNotBeNull();
            capturedRequest.Headers.Authorization.Scheme.ShouldBe("Bearer");
            capturedRequest.Headers.Authorization.Parameter.ShouldBe(token);
        }

        [Fact]
        public async Task SendHttpPostRequest_TResponse_ShouldIncludeAdditionalHeaders()
        {
            const string uri = "https://api/post-headers";
            var headers = new List<(string, string)> { ("X-Extra", "123"), ("X-Test", "ABC") };

            var expected = new SampleResponse { Message = "OK", Value = 123 };
            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(expected),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPostRequest<SampleResponse>(uri, headers, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            capturedRequest.Headers.Contains("X-Extra").ShouldBeTrue();
            capturedRequest.Headers.Contains("X-Test").ShouldBeTrue();
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
        [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
        [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
        [InlineData(HttpStatusCode.Conflict, ResultStatus.Conflict)]
        [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
        [InlineData(HttpStatusCode.GatewayTimeout, ResultStatus.Failure)]
        public async Task SendHttpPostRequest_TResponse_FailureBranches_ShouldReturnFailure(HttpStatusCode code, ResultStatus expectedStatus)
        {
            const string uri = "https://api/post-fail";

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(code, "application/json", JsonConvert.SerializeObject(new { Text = "Error" }));

            var result = await _service.SendHttpPostRequest<SampleResponse>(uri, CancellationToken.None);

            result.IsFailed.ShouldBeTrue();
            result.Status.ShouldBe(expectedStatus);
        }

        // -----------------------------
        // POST<TRequest, TResponse> tests
        // -----------------------------
        [Fact]
        public async Task SendHttpPostRequest_TRequestTResponse_ShouldSendJsonBody_AndReturnResponse()
        {
            const string uri = "https://api/post-json-body";
            var request = new SampleRequest { Name = "John", Id = 42 };
            var expected = new SampleResponse { Message = "Created", Value = 201 };
            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(expected),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPostRequest<SampleRequest, SampleResponse>(uri, request, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            result.Data.Message.ShouldBe("Created");
            result.Data.Value.ShouldBe(201);

            var body = await capturedRequest.Content.ReadAsStringAsync();
            body.ShouldContain("\"Name\":\"John\"");
            body.ShouldContain("\"Id\":42");
            capturedRequest.Content.Headers.ContentType.MediaType.ShouldBe("application/json");
        }

        [Fact]
        public async Task SendHttpPostRequest_TRequestTResponse_ShouldUseFormUrlEncodedContent()
        {
            const string uri = "https://api/post-form-body";
            var formContent = new FormUrlEncodedContent(new[]
            {
            new KeyValuePair<string, string>("field1", "value1"),
            new KeyValuePair<string, string>("field2", "value2")
        });

            var expected = new SampleResponse { Message = "Accepted", Value = 202 };
            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(expected),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPostRequest<FormUrlEncodedContent, SampleResponse>(uri, formContent, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            capturedRequest.Content.ShouldBeOfType<FormUrlEncodedContent>();
        }

        [Fact]
        public async Task SendHttpPostRequest_TRequestTResponse_ShouldIncludeBearerHeader_WhenAccessTokenProvided()
        {
            const string uri = "https://api/post-body-auth";
            const string token = "auth-token";
            var request = new SampleRequest { Name = "Auth", Id = 7 };
            var expected = new SampleResponse { Message = "OK", Value = 200 };
            HttpRequestMessage capturedRequest = null;

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(req =>
                     {
                         capturedRequest = req;
                         return new HttpResponseMessage(HttpStatusCode.OK)
                         {
                             Content = new StringContent(JsonConvert.SerializeObject(expected),
                                                         Encoding.UTF8,
                                                         "application/json")
                         };
                     });

            var result = await _service.SendHttpPostRequest<SampleRequest, SampleResponse>(uri, request, token, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
            capturedRequest.Headers.Authorization.ShouldNotBeNull();
            capturedRequest.Headers.Authorization.Parameter.ShouldBe(token);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
        [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
        [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
        [InlineData(HttpStatusCode.Conflict, ResultStatus.Conflict)]
        [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
        [InlineData(HttpStatusCode.GatewayTimeout, ResultStatus.Failure)]
        public async Task SendHttpPostRequest_TRequestTResponse_FailureBranches_ShouldReturnFailure(HttpStatusCode code, ResultStatus expectedStatus)
        {
            const string uri = "https://api/post-body-fail";
            var request = new SampleRequest { Name = "Fail", Id = 123 };

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(code, "application/json", JsonConvert.SerializeObject(new { Text = "Error" }));

            var result = await _service.SendHttpPostRequest<SampleRequest, SampleResponse>(uri, request, CancellationToken.None);

            result.IsFailed.ShouldBeTrue();
            result.Status.ShouldBe(expectedStatus);
        }

        // -----------------------------
        // POST (no generic) tests
        // -----------------------------
        [Fact]
        public async Task SendHttpPostRequest_NoGenerics_ShouldReturnSuccess()
        {
            const string uri = "https://api/post-void";
            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(HttpStatusCode.OK, "application/json", "\"Done\"");

            var result = await _service.SendHttpPostRequest(uri, CancellationToken.None);

            result.IsSuccess.ShouldBeTrue();
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
        [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
        [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
        [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
        [InlineData(HttpStatusCode.Conflict, ResultStatus.Conflict)]
        [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
        [InlineData(HttpStatusCode.GatewayTimeout, ResultStatus.Failure)]
        public async Task SendHttpPostRequest_NoGenerics_FailureBranches_ShouldReturnFailure(HttpStatusCode code, ResultStatus expectedStatus)
        {
            const string uri = "https://api/post-void-fail";

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(code, "application/json", JsonConvert.SerializeObject(new { Text = "Error" }));

            var result = await _service.SendHttpPostRequest(uri, CancellationToken.None);

            result.IsFailed.ShouldBeTrue();
            result.Status.ShouldBe(expectedStatus);
        }

        // -----------------------------
        // Overload verification
        // -----------------------------
        [Fact]
        public async Task SendHttpPostRequest_AllOverloads_ShouldCallBaseMethodsSuccessfully()
        {
            const string uri = "https://api/post-overload";
            var request = new SampleRequest { Name = "Multi", Id = 9 };
            var expected = new SampleResponse { Message = "OK", Value = 200 };

            _mockHttp.When(HttpMethod.Post, uri)
                     .Respond(HttpStatusCode.OK, "application/json", JsonConvert.SerializeObject(expected));

            var r1 = await _service.SendHttpPostRequest<SampleResponse>(uri, "token", CancellationToken.None);
            var r2 = await _service.SendHttpPostRequest<SampleResponse>(uri, new List<(string, string)>(), CancellationToken.None);
            var r3 = await _service.SendHttpPostRequest<SampleResponse>(uri, CancellationToken.None);
            var r4 = await _service.SendHttpPostRequest<SampleRequest, SampleResponse>(uri, request, "token", CancellationToken.None);
            var r5 = await _service.SendHttpPostRequest<SampleRequest, SampleResponse>(uri, request, new List<(string, string)>(), CancellationToken.None);
            var r6 = await _service.SendHttpPostRequest<SampleRequest, SampleResponse>(uri, request, CancellationToken.None);
            var r7 = await _service.SendHttpPostRequest(uri, "token", CancellationToken.None);
            var r8 = await _service.SendHttpPostRequest(uri, new List<(string, string)>(), CancellationToken.None);
            var r9 = await _service.SendHttpPostRequest(uri, CancellationToken.None);

            r1.IsSuccess.ShouldBeTrue();
            r2.IsSuccess.ShouldBeTrue();
            r3.IsSuccess.ShouldBeTrue();
            r4.IsSuccess.ShouldBeTrue();
            r5.IsSuccess.ShouldBeTrue();
            r6.IsSuccess.ShouldBeTrue();
            r7.IsSuccess.ShouldBeTrue();
            r8.IsSuccess.ShouldBeTrue();
            r9.IsSuccess.ShouldBeTrue();
        }
    }
}
