using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shared.Results;
using SimpleResults;

namespace ClientProxyBase
{
    public abstract partial class ClientProxyBase
    {
        internal virtual async Task<Result> SendHttpDeleteRequest(String uri,
                                                                  String accessToken,
                                                                  List<(String header, String value)> additionalHeaders,
                                                                  CancellationToken cancellationToken)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Delete, uri);
            if (String.IsNullOrEmpty(accessToken) == false)
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);

            AddAdditionalHeaders(requestMessage, additionalHeaders);

            // Make the Http Call here
            HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

            // Process the response
            Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

            if (result.IsFailed)
                return ResultHelpers.CreateFailure(result);

            return Result.Success();
        }

        internal virtual async Task<Result> SendHttpDeleteRequest(String uri,
                                                                  String accessToken,
                                                                  CancellationToken cancellationToken) =>
            await this.SendHttpDeleteRequest(uri, accessToken,null, cancellationToken);

        internal virtual async Task<Result> SendHttpDeleteRequest(String uri,
                                                                  List<(String header, String value)> additionalHeaders,
                                                                  CancellationToken cancellationToken) =>
            await this.SendHttpDeleteRequest(uri, null, additionalHeaders, cancellationToken);

        internal virtual async Task<Result> SendHttpDeleteRequest(String uri,
                                                                  CancellationToken cancellationToken) =>
            await this.SendHttpDeleteRequest(uri, null, null, cancellationToken);
    }
}
