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
        internal virtual async Task<Result<TResponse>> SendHttpPostRequest<TResponse>(String uri,
                                                  String accessToken,
                                                  List<(String header, String value)> additionalHeaders,
                                                  CancellationToken cancellationToken) {

            HttpRequestMessage requestMessage = new(HttpMethod.Post, uri);
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

        internal virtual async Task<Result<TResponse>> SendHttpPostRequest<TResponse>(String uri, String accessToken, CancellationToken cancellationToken) =>
                    await this.SendHttpPostRequest<TResponse>(uri, accessToken, null, cancellationToken);

        internal virtual async Task<Result<TResponse>> SendHttpPostRequest<TResponse>(String uri, List<(String header, String value)> additionalHeaders, CancellationToken cancellationToken) =>
                            await this.SendHttpPostRequest<TResponse>(uri, null, additionalHeaders, cancellationToken);

        internal virtual async Task<Result<TResponse>> SendHttpPostRequest<TResponse>(String uri, CancellationToken cancellationToken) =>
                                        await this.SendHttpPostRequest<TResponse>(uri, null, null, cancellationToken);


        internal virtual async Task<Result<TResponse>> SendHttpPostRequest<TRequest, TResponse>(String uri,
                                                                                                TRequest request,
                                                                                                String accessToken,
                                                                                                List<(String header, String value)> additionalHeaders,
                                                                                                CancellationToken cancellationToken) {
            HttpRequestMessage requestMessage = new(HttpMethod.Post, uri);
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

            TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

            return Result.Success(responseData);
        }

        internal virtual async Task<Result<TResponse>> SendHttpPostRequest<TRequest, TResponse>(String uri, TRequest request, String accessToken, CancellationToken cancellationToken) =>
            await this.SendHttpPostRequest<TRequest, TResponse>(uri,request, accessToken, null, cancellationToken);

        internal virtual async Task<Result<TResponse>> SendHttpPostRequest<TRequest, TResponse>(String uri, TRequest request, List<(String header, String value)> additionalHeaders, CancellationToken cancellationToken) =>
            await this.SendHttpPostRequest<TRequest, TResponse>(uri, request, null, additionalHeaders, cancellationToken);

        internal virtual async Task<Result<TResponse>> SendHttpPostRequest<TRequest, TResponse>(String uri, TRequest request, CancellationToken cancellationToken) =>
            await this.SendHttpPostRequest<TRequest, TResponse>(uri, request, null, null, cancellationToken);

        internal virtual async Task<Result> SendHttpPostRequest(String uri,
                                                                String accessToken,
                                                                List<(String header, String value)> additionalHeaders,
                                                                CancellationToken cancellationToken)
        {
            HttpRequestMessage requestMessage = new(HttpMethod.Post, uri);
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

        internal virtual async Task<Result> SendHttpPostRequest(String uri, String accessToken, CancellationToken cancellationToken) =>
            await this.SendHttpPostRequest(uri, accessToken, null, cancellationToken);

        internal virtual async Task<Result> SendHttpPostRequest(String uri, List<(String header, String value)> additionalHeaders, CancellationToken cancellationToken) =>
            await this.SendHttpPostRequest(uri, null, additionalHeaders, cancellationToken);

        internal virtual async Task<Result> SendHttpPostRequest(String uri, CancellationToken cancellationToken) =>
            await this.SendHttpPostRequest(uri, null, null, cancellationToken);
    }
}
