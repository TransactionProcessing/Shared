using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientProxyBase;
using SimpleResults;

namespace Shared.Tests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using Microsoft.VisualStudio.TestPlatform.TestHost;
    using Shouldly;
    using Xunit;

    public partial class SharedTests {
        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Created)]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.NonAuthoritativeInformation)]
        [InlineData(HttpStatusCode.NoContent)]
        [InlineData(HttpStatusCode.ResetContent)]
        [InlineData(HttpStatusCode.PartialContent)]
        [InlineData(HttpStatusCode.MultiStatus)]
        [InlineData(HttpStatusCode.AlreadyReported)]
        [InlineData(HttpStatusCode.IMUsed)]

        public async Task ClientProxyBase_HandleResponseX_SuccessStatus(HttpStatusCode statusCode) {
            String responseContent = $"Content - {statusCode}";
            HttpResponseMessage response = new(statusCode);
            response.Content = new StringContent(responseContent);
            TestClient proxybase = new(new HttpClient());
            Result<String> result = await proxybase.Test_HandleResponseX(response, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.ShouldBe(responseContent);
        }

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Created)]
        [InlineData(HttpStatusCode.Accepted)]
        [InlineData(HttpStatusCode.NonAuthoritativeInformation)]
        [InlineData(HttpStatusCode.NoContent)]
        [InlineData(HttpStatusCode.ResetContent)]
        [InlineData(HttpStatusCode.PartialContent)]
        [InlineData(HttpStatusCode.MultiStatus)]
        [InlineData(HttpStatusCode.AlreadyReported)]
        [InlineData(HttpStatusCode.IMUsed)]

        public async Task ClientProxyBase_HandleResponse_SuccessStatus(HttpStatusCode statusCode) {
            String responseContent = $"Content - {statusCode}";
            HttpResponseMessage response = new(statusCode);
            response.Content = new StringContent(responseContent);
            TestClient proxybase = new(new HttpClient());
            Should.NotThrow(async () => { await proxybase.Test_HandleResponse(response, CancellationToken.None); });
        }

        [Theory]
        [InlineData(HttpStatusCode.Continue, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.SwitchingProtocols, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Processing, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.EarlyHints, ResultStatus.Failure)]
        public async Task ClientProxyBase_HandleResponseX_1xx_ErrorStatus(HttpStatusCode statusCode,
                                                                          ResultStatus resultStatus) {
            await TestMethod_HandleResponseX(statusCode, resultStatus);
        }

        [Theory]
        [InlineData(HttpStatusCode.Continue, typeof(Exception))]
        [InlineData(HttpStatusCode.SwitchingProtocols, typeof(Exception))]
        [InlineData(HttpStatusCode.Processing, typeof(Exception))]
        [InlineData(HttpStatusCode.EarlyHints, typeof(Exception))]
        public async Task ClientProxyBase_HandleResponse_1xx_ErrorStatus(HttpStatusCode statusCode,
                                                                         Type expectedException) {
            await TestMethod_HandleResponse(statusCode, expectedException);
        }

        [Theory]
        [InlineData(HttpStatusCode.MultipleChoices, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Ambiguous, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.MovedPermanently, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Moved, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Found, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Redirect, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.SeeOther, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.RedirectMethod, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.NotModified, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.UseProxy, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Unused, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.TemporaryRedirect, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.RedirectKeepVerb, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.PermanentRedirect, ResultStatus.Failure)]
        public async Task ClientProxyBase_HandleResponseX_3xx_ErrorStatus(HttpStatusCode statusCode,
                                                                          ResultStatus resultStatus) {
            await TestMethod_HandleResponseX(statusCode, resultStatus);
        }

        [Theory]
        [InlineData(HttpStatusCode.MultipleChoices, typeof(Exception))]
        [InlineData(HttpStatusCode.Ambiguous, typeof(Exception))]
        [InlineData(HttpStatusCode.MovedPermanently, typeof(Exception))]
        [InlineData(HttpStatusCode.Moved, typeof(Exception))]
        [InlineData(HttpStatusCode.Found, typeof(Exception))]
        [InlineData(HttpStatusCode.Redirect, typeof(Exception))]
        [InlineData(HttpStatusCode.SeeOther, typeof(Exception))]
        [InlineData(HttpStatusCode.RedirectMethod, typeof(Exception))]
        [InlineData(HttpStatusCode.NotModified, typeof(Exception))]
        [InlineData(HttpStatusCode.UseProxy, typeof(Exception))]
        [InlineData(HttpStatusCode.Unused, typeof(Exception))]
        [InlineData(HttpStatusCode.TemporaryRedirect, typeof(Exception))]
        [InlineData(HttpStatusCode.RedirectKeepVerb, typeof(Exception))]
        [InlineData(HttpStatusCode.PermanentRedirect, typeof(Exception))]
        public async Task ClientProxyBase_HandleResponse_3xx_ErrorStatus(HttpStatusCode statusCode,
                                                                         Type expectedException) {
            await TestMethod_HandleResponse(statusCode, expectedException);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, ResultStatus.Invalid)]
        [InlineData(HttpStatusCode.Unauthorized, ResultStatus.Unauthorized)]
        [InlineData(HttpStatusCode.PaymentRequired, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Forbidden, ResultStatus.Forbidden)]
        [InlineData(HttpStatusCode.NotFound, ResultStatus.NotFound)]
        [InlineData(HttpStatusCode.MethodNotAllowed, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.NotAcceptable, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.ProxyAuthenticationRequired, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.RequestTimeout, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Conflict, ResultStatus.Conflict)]
        [InlineData(HttpStatusCode.Gone, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.LengthRequired, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.PreconditionFailed, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.RequestEntityTooLarge, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.RequestUriTooLong, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.UnsupportedMediaType, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.RequestedRangeNotSatisfiable, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.ExpectationFailed, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.MisdirectedRequest, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.UnprocessableEntity, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Locked, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.FailedDependency, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.UpgradeRequired, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.PreconditionRequired, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.TooManyRequests, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.RequestHeaderFieldsTooLarge, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.UnavailableForLegalReasons, ResultStatus.Failure)]
        public async Task ClientProxyBase_HandleResponseX_4xx_ErrorStatus(HttpStatusCode statusCode,
                                                                          ResultStatus resultStatus) {
            await TestMethod_HandleResponseX(statusCode, resultStatus);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadRequest, typeof(InvalidOperationException))]
        [InlineData(HttpStatusCode.Unauthorized, typeof(UnauthorizedAccessException))]
        [InlineData(HttpStatusCode.PaymentRequired, typeof(Exception))]
        [InlineData(HttpStatusCode.Forbidden, typeof(UnauthorizedAccessException))]
        [InlineData(HttpStatusCode.NotFound, typeof(KeyNotFoundException))]
        [InlineData(HttpStatusCode.MethodNotAllowed, typeof(Exception))]
        [InlineData(HttpStatusCode.NotAcceptable, typeof(Exception))]
        [InlineData(HttpStatusCode.ProxyAuthenticationRequired, typeof(Exception))]
        [InlineData(HttpStatusCode.RequestTimeout, typeof(Exception))]
        [InlineData(HttpStatusCode.Conflict, typeof(Exception))]
        [InlineData(HttpStatusCode.Gone, typeof(Exception))]
        [InlineData(HttpStatusCode.LengthRequired, typeof(Exception))]
        [InlineData(HttpStatusCode.PreconditionFailed, typeof(Exception))]
        [InlineData(HttpStatusCode.RequestEntityTooLarge, typeof(Exception))]
        [InlineData(HttpStatusCode.RequestUriTooLong, typeof(Exception))]
        [InlineData(HttpStatusCode.UnsupportedMediaType, typeof(Exception))]
        [InlineData(HttpStatusCode.RequestedRangeNotSatisfiable, typeof(Exception))]
        [InlineData(HttpStatusCode.ExpectationFailed, typeof(Exception))]
        [InlineData(HttpStatusCode.MisdirectedRequest, typeof(Exception))]
        [InlineData(HttpStatusCode.UnprocessableEntity, typeof(Exception))]
        [InlineData(HttpStatusCode.Locked, typeof(Exception))]
        [InlineData(HttpStatusCode.FailedDependency, typeof(Exception))]
        [InlineData(HttpStatusCode.UpgradeRequired, typeof(Exception))]
        [InlineData(HttpStatusCode.PreconditionRequired, typeof(Exception))]
        [InlineData(HttpStatusCode.TooManyRequests, typeof(Exception))]
        [InlineData(HttpStatusCode.RequestHeaderFieldsTooLarge, typeof(Exception))]
        [InlineData(HttpStatusCode.UnavailableForLegalReasons, typeof(Exception))]
        public async Task ClientProxyBase_HandleResponse_4xx_ErrorStatus(HttpStatusCode statusCode,
                                                                         Type expectedException) {
            await TestMethod_HandleResponse(statusCode, expectedException);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, ResultStatus.CriticalError)]
        [InlineData(HttpStatusCode.NotImplemented, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.BadGateway, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.ServiceUnavailable, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.GatewayTimeout, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.HttpVersionNotSupported, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.VariantAlsoNegotiates, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.InsufficientStorage, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.LoopDetected, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.NotExtended, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.NetworkAuthenticationRequired, ResultStatus.Failure)]
        public async Task ClientProxyBase_HandleResponseX_5xx_ErrorStatus(HttpStatusCode statusCode,
                                                                          ResultStatus resultStatus) {
            await TestMethod_HandleResponseX(statusCode, resultStatus);
        }

        [Theory]
        [InlineData(HttpStatusCode.InternalServerError, typeof(Exception))]
        [InlineData(HttpStatusCode.NotImplemented, typeof(Exception))]
        [InlineData(HttpStatusCode.BadGateway, typeof(Exception))]
        [InlineData(HttpStatusCode.ServiceUnavailable, typeof(Exception))]
        [InlineData(HttpStatusCode.GatewayTimeout, typeof(Exception))]
        [InlineData(HttpStatusCode.HttpVersionNotSupported, typeof(Exception))]
        [InlineData(HttpStatusCode.VariantAlsoNegotiates, typeof(Exception))]
        [InlineData(HttpStatusCode.InsufficientStorage, typeof(Exception))]
        [InlineData(HttpStatusCode.LoopDetected, typeof(Exception))]
        [InlineData(HttpStatusCode.NotExtended, typeof(Exception))]
        [InlineData(HttpStatusCode.NetworkAuthenticationRequired, typeof(Exception))]
        public async Task ClientProxyBase_HandleResponse_5xx_ErrorStatus(HttpStatusCode statusCode,
                                                                         Type expectedException) {
            await TestMethod_HandleResponse(statusCode, expectedException);
        }

        private async Task TestMethod_HandleResponseX(HttpStatusCode statusCode,
                                                      ResultStatus resultStatus) {
            var proxybase = new TestClient(new HttpClient());
            var result = await proxybase.Test_HandleResponseX(new HttpResponseMessage(statusCode), CancellationToken.None);
            result.Status.ShouldBe(resultStatus);
        }

        private async Task TestMethod_HandleResponse(HttpStatusCode statusCode,
                                                     Type expectedException) {
            var proxybase = new TestClient(new HttpClient());
            var exception = Should.Throw<Exception>(async () => { await proxybase.Test_HandleResponse(new HttpResponseMessage(statusCode), CancellationToken.None); });
            exception.ShouldBeOfType(expectedException);
        }

        [Fact]
        public void HandleResponseContent_NullContent_ReturnsDefaultList() {
            // Arrange
            string content = null;
            TestClient proxybase = new(new HttpClient());

            // Act
            var result = proxybase.Test_HandleResponseContent<List<ApiResourceDetails>>(content);

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
            result.Data.ShouldBeEmpty();
        }

        [Fact]
        public void HandleResponseContent_NullContent_ReturnsDefaultObject() {
            // Arrange
            string content = null;
            TestClient proxybase = new(new HttpClient());

            // Act
            var result = proxybase.Test_HandleResponseContent<ApiResourceDetails>(content);

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
        }

        [Fact]
        public void HandleResponseContent_EmptyContent_ReturnsDefaultList() {
            // Arrange
            string content = string.Empty;
            TestClient proxybase = new(new HttpClient());

            // Act
            var result = proxybase.Test_HandleResponseContent<List<ApiResourceDetails>>(content);

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
            result.Data.ShouldBeEmpty();
        }

        [Fact]
        public void HandleResponseContent_EmptyContent_ReturnsDefaultObject() {
            // Arrange
            string content = string.Empty;
            TestClient proxybase = new(new HttpClient());

            // Act
            var result = proxybase.Test_HandleResponseContent<ApiResourceDetails>(content);

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
        }

        [Fact]
        public void HandleResponseContent_ValidJsonContent_ReturnsDeserializedObject() {
            // Arrange
            string json = "{ \"Data\": { \"name\": \"test\", \"description\": \"test description\" } }";
            TestClient proxybase = new(new HttpClient());

            // Act
            var result = proxybase.Test_HandleResponseContent<ApiResourceDetails>(json);

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
            result.Data.name.ShouldBe("test");
            result.Data.description.ShouldBe("test description");
        }

        [Fact]
        public void HandleResponseContent_ValidJsonContent_ReturnsDeserializedList() {
            // Arrange
            string json = "{ \"Data\": [{ \"name\": \"test1\", \"description\": \"test description 1\" }, { \"name\": \"test2\", \"description\": \"test description 2\" }] }";
            TestClient proxybase = new(new HttpClient());

            // Act
            var result = proxybase.Test_HandleResponseContent<List<ApiResourceDetails>>(json);

            // Assert
            result.ShouldNotBeNull();
            result.Data.ShouldNotBeNull();
            result.Data.Count.ShouldBe(2);
            result.Data[0].name.ShouldBe("test1");
            result.Data[0].description.ShouldBe("test description 1");
            result.Data[1].name.ShouldBe("test2");
            result.Data[1].description.ShouldBe("test description 2");
        }
    }

    public class TestClient : ClientProxyBase.ClientProxyBase{
        public TestClient(HttpClient httpClient) : base(httpClient){
        }
        public async Task Test_HandleResponse(HttpResponseMessage responseMessage, CancellationToken cancellationToken)
        {
            await this.HandleResponse(responseMessage, cancellationToken);
        }

        public async Task<Result<String>> Test_HandleResponseX(HttpResponseMessage responseMessage, CancellationToken cancellationToken){
            return await this.HandleResponseX(responseMessage, cancellationToken);
        }

        public ResponseData<T> Test_HandleResponseContent<T>(String content)
        {
            return this.HandleResponseContent<T>(content);
        }
    }

    public class ApiResourceDetails
    {
        public string name { get; set; }
        public string description { get; set; }
    }
}
