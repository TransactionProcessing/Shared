using Newtonsoft.Json;
using SimpleResults;
using System;
using System.Linq;

namespace ClientProxyBase;

using Shared.Results;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public abstract partial class ClientProxyBase {
    #region Fields

    /// <summary>
    /// The HTTP client
    /// </summary>
    protected readonly HttpClient HttpClient;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientProxyBase"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    protected ClientProxyBase(HttpClient httpClient) {
        this.HttpClient = httpClient;
    }

    #endregion

    #region Methods

    protected virtual async Task<String> HandleResponse(HttpResponseMessage responseMessage,
                                                        CancellationToken cancellationToken) {
        String result = String.Empty;

        // Read the content from the response
        String content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        // Check the response code
        if (!responseMessage.IsSuccessStatusCode) {
            // throw a specific  exception to inherited class
            switch (responseMessage.StatusCode) {
                case HttpStatusCode.BadRequest:
                    throw new InvalidOperationException(content);
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    throw new UnauthorizedAccessException(content);
                case HttpStatusCode.NotFound:
                    throw new KeyNotFoundException(content);
                case HttpStatusCode.InternalServerError:
                    throw new ClientHttpException("An internal error has occurred");
                default:
                    throw new ClientHttpException($"An internal error has occurred ({responseMessage.StatusCode})");
            }
        }

        // Set the result
        result = content;

        return result;
    }
        
    protected virtual async Task<Result<String>> HandleResponseX(HttpResponseMessage responseMessage,
                                                                 CancellationToken cancellationToken) {

        // Read the content from the response
        String content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

        // Check the response code
        if (!responseMessage.IsSuccessStatusCode) {
            return responseMessage.StatusCode switch {
                HttpStatusCode.BadRequest => Result.Invalid(content),
                HttpStatusCode.Forbidden => Result.Forbidden(content),
                HttpStatusCode.Unauthorized => Result.Unauthorized(content),
                HttpStatusCode.NotFound => Result.NotFound(content),
                HttpStatusCode.Conflict => Result.Conflict(content),
                HttpStatusCode.InternalServerError => Result.CriticalError("An internal error has occurred"),
                _ => Result.Failure($"An internal error has occurred ({responseMessage.StatusCode})")
            };
        }

        return Result.Success<String>(content);
    }

    protected virtual ResponseData<T> HandleResponseContent<T>(String content)
    {
        if (String.IsNullOrEmpty(content))
        {
            T data = typeof(IEnumerable).IsAssignableFrom(typeof(T))
                ? (T)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(T).GetGenericArguments()))
                : (T)Activator.CreateInstance(typeof(T));

            return new ResponseData<T> { Data = data };
        }
        return JsonConvert.DeserializeObject<ResponseData<T>>(content);
    }

    /*
    protected virtual async Task<Result<TResponse>> SendGetRequest<TResponse>(String uri,
                                                                              String accessToken,
                                                                              CancellationToken cancellationToken) =>
        await this.SendGetRequest<TResponse>(uri, accessToken, null, null, cancellationToken);

    protected virtual async Task<Result<TResponse>> SendGetRequest<TResponse>(String uri,
                                                                              String accessToken,
                                                                              List<(String header, String value)> additionalHeaders,
                                                                              CancellationToken cancellationToken) =>
        await this.SendGetRequest<TResponse>(uri, accessToken, additionalHeaders, null, cancellationToken);

    protected virtual async Task<Result<TResponse>> SendGetRequest<TResponse>(String uri,
                                                                              String accessToken,
                                                                              HttpContent content,
                                                                              CancellationToken cancellationToken) =>
        await this.SendGetRequest<TResponse>(uri, accessToken, null, content, cancellationToken);


    protected virtual async Task<Result<TResponse>> SendGetRequest<TResponse>(String uri, String accessToken, List<(String header, String value)> additionalHeaders, HttpContent content, CancellationToken cancellationToken)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Get, uri);
        if (String.IsNullOrEmpty(accessToken) == false)
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);
        
        AddAdditionalHeaders(requestMessage, additionalHeaders);

        if (content != null) {
            requestMessage.Content = content;
        }

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
    }

    protected virtual async Task<Result<TResponse>> SendPostRequest<TRequest, TResponse>(String uri, String accessToken, TRequest content, CancellationToken cancellationToken) => await this.SendPostRequest<TRequest, TResponse>(uri, accessToken, content, null, cancellationToken);

    protected virtual async Task<Result<TResponse>> SendPostRequest<TRequest, TResponse>(String uri, String accessToken, TRequest content, List<(String header, String value)> additionalHeaders, CancellationToken cancellationToken)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Post, uri);
        if (String.IsNullOrEmpty(accessToken) == false)
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);
        
        AddAdditionalHeaders(requestMessage, additionalHeaders);

        if (content.GetType() == typeof(FormUrlEncodedContent)) {
            // Treat this specially
            requestMessage.Content = content as FormUrlEncodedContent;
        }
        else {
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
        }

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
    }

    protected virtual async Task<Result> SendPostRequest<TResponse>(string uri,
                                                                    string accessToken,
                                                                    List<(string header, string value)> additionalHeaders,
                                                                    CancellationToken cancellationToken) {
        Result result = await this.SendPostRequest<Object>(uri, accessToken, additionalHeaders, cancellationToken);
        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);
        return Result.Success();
    }

    protected virtual async Task<Result> SendPostRequest<TResponse>(string uri,
                                                                    string accessToken,
                                                                    CancellationToken cancellationToken) =>  await this.SendPostRequest<TResponse>(uri, accessToken, null, cancellationToken);

    //protected virtual async Task<Result> SendPostRequest(string uri,
    //                                                     string accessToken = null,
    //                                                     List<(string header, string value)> additionalHeaders = null)
    //{
    //    Result<String> result = await SendPostRequest<object, string>(uri, accessToken, null, cancellationToken, additionalHeaders);
    //    if (result.IsFailed)
    //        return ResultHelpers.CreateFailure(result);
    //    return Result.Success();
    //}

    protected virtual async Task<Result<TResponse>> SendPutRequest<TRequest, TResponse>(String uri, String accessToken, TRequest content, CancellationToken cancellationToken) => 
        await this.SendPutRequest<TRequest, TResponse>(uri, accessToken, content, cancellationToken, null);

    protected virtual async Task<Result<TResponse>> SendPutRequest<TRequest, TResponse>(String uri, String accessToken, TRequest content, CancellationToken cancellationToken, List<(String header, String value)> additionalHeaders)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Put, uri);
        if (String.IsNullOrEmpty(accessToken) == false)
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);

        AddAdditionalHeaders(requestMessage, additionalHeaders);

        if (content.GetType() == typeof(FormUrlEncodedContent))
        {
            // Treat this specially
            requestMessage.Content = content as FormUrlEncodedContent;
        }
        else
        {
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
        }

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
    }

    protected virtual async Task<Result<TResponse>> SendPatchRequest<TRequest, TResponse>(String uri, String accessToken, TRequest content, CancellationToken cancellationToken) =>         await this.SendPatchRequest<TRequest, TResponse>(uri, accessToken, content, cancellationToken, null);

    protected virtual async Task<Result<TResponse>> SendPatchRequest<TRequest, TResponse>(String uri, String accessToken, TRequest content, CancellationToken cancellationToken, List<(String header, String value)> additionalHeaders)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Patch, uri);
        if (String.IsNullOrEmpty(accessToken) == false)
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);

        AddAdditionalHeaders(requestMessage, additionalHeaders);

        if (content.GetType() == typeof(FormUrlEncodedContent))
        {
            // Treat this specially
            requestMessage.Content = content as FormUrlEncodedContent;
        }
        else
        {
            requestMessage.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
        }

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
    }

    protected virtual async Task<Result<TResponse>> SendDeleteRequest<TResponse>(String uri, String accessToken, CancellationToken cancellationToken) =>         await this.SendDeleteRequest<TResponse>(uri, accessToken, cancellationToken, null);

    protected virtual async Task<Result<TResponse>> SendDeleteRequest<TResponse>(String uri, String accessToken, CancellationToken cancellationToken, List<(String header, String value)> additionalHeaders)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Delete, uri);
        if (String.IsNullOrEmpty(accessToken) == false)
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);

        AddAdditionalHeaders(requestMessage,additionalHeaders);

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
    }*/

    static void AddAdditionalHeaders(HttpRequestMessage requestMessage,
                                     List<(String header, String value)> additionalHeaders) {
        if (additionalHeaders != null && additionalHeaders.Any()) {
            foreach ((String header, String value) additionalHeader in additionalHeaders) {
                requestMessage.Headers.Add(additionalHeader.header, additionalHeader.value);
            }
        }
    }

    #endregion
}

public class ClientHttpException : Exception {
    public ClientHttpException(string? message,
                               Exception? innerException = null) : base(message, innerException) {

    }
}

public static class AuthenticationSchemes {
    public static readonly String Bearer = "Bearer";
}