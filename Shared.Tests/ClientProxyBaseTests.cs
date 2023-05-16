using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Tests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using ClientProxyBase;
    using Shouldly;
    using Xunit;

    public partial class SharedTests
    {
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

        public async Task ClientProxyBase_HandleResponse_SuccessStatus(HttpStatusCode statusCode){
            String responseContent = $"Content - {statusCode}";
            HttpResponseMessage response = new HttpResponseMessage(statusCode);
            response.Content = new StringContent(responseContent);
            var proxybase = new TestClient(new HttpClient());
            var content = await proxybase.Test(response, CancellationToken.None);
            content.ShouldBe(responseContent);
                         
        }

        [Theory]
        [InlineData(HttpStatusCode.Continue, typeof(Exception))]
        [InlineData(HttpStatusCode.SwitchingProtocols, typeof(Exception))]
        [InlineData(HttpStatusCode.Processing, typeof(Exception))]
        [InlineData(HttpStatusCode.EarlyHints, typeof(Exception))]
        public async Task ClientProxyBase_HandleResponse_1xx_ErrorStatus(HttpStatusCode statusCode, Type exceptionType){
            TestMethod(statusCode, exceptionType);
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
        public async Task ClientProxyBase_HandleResponse_3xx_ErrorStatus(HttpStatusCode statusCode, Type exceptionType)
        {
            TestMethod(statusCode, exceptionType);
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
        public async Task ClientProxyBase_HandleResponse_4xx_ErrorStatus(HttpStatusCode statusCode, Type exceptionType)
        {
            TestMethod(statusCode, exceptionType);
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
        public async Task ClientProxyBase_HandleResponse_5xx_ErrorStatus(HttpStatusCode statusCode, Type exceptionType)
        {
            TestMethod(statusCode, exceptionType);
        }


        private void TestMethod(HttpStatusCode statusCode, Type exceptionType)
        {
            var proxybase = new TestClient(new HttpClient());
            Should.Throw(async () => {
                             await proxybase.Test(new HttpResponseMessage(statusCode), CancellationToken.None);
                         }, exceptionType);
        }
    }

    public class TestClient : ClientProxyBase{
        public TestClient(HttpClient httpClient) : base(httpClient){
        }

        public async Task<String> Test(HttpResponseMessage responseMessage, CancellationToken cancellationToken){
            return await this.HandleResponse(responseMessage, cancellationToken);
        }
    }
}
