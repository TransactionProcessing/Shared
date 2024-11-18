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
            TestClient proxybase = new TestClient(new HttpClient());
            Result<String> result  = await proxybase.Test(response, CancellationToken.None);
            result.IsSuccess.ShouldBeTrue();
            result.Data.ShouldBe(responseContent);
                         
        }

        [Theory]
        [InlineData(HttpStatusCode.Continue, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.SwitchingProtocols, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.Processing, ResultStatus.Failure)]
        [InlineData(HttpStatusCode.EarlyHints, ResultStatus.Failure)]
        public async Task ClientProxyBase_HandleResponse_1xx_ErrorStatus(HttpStatusCode statusCode, ResultStatus resultStatus){
            await TestMethod(statusCode, resultStatus);
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
        public async Task ClientProxyBase_HandleResponse_3xx_ErrorStatus(HttpStatusCode statusCode, ResultStatus resultStatus)
        {
            await TestMethod(statusCode, resultStatus);
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
        public async Task ClientProxyBase_HandleResponse_4xx_ErrorStatus(HttpStatusCode statusCode, ResultStatus resultStatus)
        {
            await TestMethod(statusCode, resultStatus);
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
        public async Task ClientProxyBase_HandleResponse_5xx_ErrorStatus(HttpStatusCode statusCode, ResultStatus resultStatus)
        {
            await TestMethod(statusCode, resultStatus);
        }

        private async Task TestMethod(HttpStatusCode statusCode, ResultStatus resultStatus)
        {
            var proxybase = new TestClient(new HttpClient());
            var result = await proxybase.Test(new HttpResponseMessage(statusCode), CancellationToken.None);
            result.Status.ShouldBe(resultStatus);

        }
    }

    public class TestClient : ClientProxyBase.ClientProxyBase{
        public TestClient(HttpClient httpClient) : base(httpClient){
        }

        public async Task<Result<String>> Test(HttpResponseMessage responseMessage, CancellationToken cancellationToken){
            return await this.HandleResponseX(responseMessage, cancellationToken);
        }
    }
}
