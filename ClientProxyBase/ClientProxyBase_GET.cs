using Newtonsoft.Json;
using Shared.Results;
using SimpleResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClientProxyBase
{
    public abstract partial class ClientProxyBase
    {
        protected virtual async Task<Result<TResponse>> SendHttpGetRequest<TResponse>(String uri,
                                                           String accessToken,
                                                           List<(String header, String value)> additionalHeaders,
                                                           CancellationToken cancellationToken)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Get, uri);
            if (String.IsNullOrEmpty(accessToken) == false)
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);

            AddAdditionalHeaders(requestMessage, additionalHeaders);

            // Make the Http Call here
            HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

            // Process the response
            Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

            if (result.IsFailed)
                return ResultHelpers.CreateFailure(result);

            TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

            return Result.Success(responseData);
        }

        protected virtual async Task<Result<TResponse>> SendHttpGetRequest<TResponse>(String uri,
                                                                                      String accessToken,
                                                                                      CancellationToken cancellationToken) =>
            await this.SendHttpGetRequest<TResponse>(uri, accessToken, null, cancellationToken);

        protected virtual async Task<Result<TResponse>> SendHttpGetRequest<TResponse>(String uri,
                                                                                      List<(String header, String value)> additionalHeaders,
                                                                                      CancellationToken cancellationToken) =>
            await this.SendHttpGetRequest<TResponse>(uri, null, additionalHeaders, cancellationToken);

        protected virtual async Task<Result<TResponse>> SendHttpGetRequest<TResponse>(String uri,
                                                                                      CancellationToken cancellationToken) =>
            await this.SendHttpGetRequest<TResponse>(uri, null, null, cancellationToken);
    }
}
