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
        protected virtual async Task<Result> SendHttpPatchRequest<TRequest>(String uri,
                                                                                    TRequest request,
                                                                                    String accessToken,
                                                                                    List<(String header, String value)> additionalHeaders,
                                                                                    CancellationToken cancellationToken)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Patch, uri);
            if (String.IsNullOrEmpty(accessToken) == false)
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);

            AddAdditionalHeaders(requestMessage, additionalHeaders);

            if (request.GetType() == typeof(FormUrlEncodedContent))
            {
                // Treat this specially
                requestMessage.Content = request as FormUrlEncodedContent;
            }
            else
            {
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            }

            // Make the Http Call here
            HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

            // Process the response
            Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

            if (result.IsFailed)
                return ResultHelpers.CreateFailure(result);

            return Result.Success();
        }

        protected virtual async Task<Result> SendHttpPatchRequest<TRequest>(String uri,
                                                                                      TRequest request,
                                                                                      String accessToken,
                                                                                      CancellationToken cancellationToken) =>
            await this.SendHttpPatchRequest(uri, request, accessToken, null, cancellationToken);

        protected virtual async Task<Result> SendHttpPatchRequest<TRequest>(String uri,
                                                                                      TRequest request,
                                                                                      List<(String header, String value)> additionalHeaders,
                                                                                      CancellationToken cancellationToken) =>
            await this.SendHttpPatchRequest(uri, request, null, additionalHeaders, cancellationToken);

        protected virtual async Task<Result> SendHttpPatchRequest<TRequest>(String uri,
                                                                                      TRequest request,
                                                                                      CancellationToken cancellationToken) =>
            await this.SendHttpPatchRequest(uri, request, null, null, cancellationToken);

    }
}
