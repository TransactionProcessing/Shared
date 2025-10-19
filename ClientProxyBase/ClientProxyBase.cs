using Newtonsoft.Json;
using SimpleResults;
using System;

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

public abstract class ClientProxyBase {
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

    protected virtual async Task<Result<TResponse>> SendGetRequest<TResponse>(String uri, String accessToken, CancellationToken cancellationToken)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Get, uri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
    }

    protected virtual async Task<Result<TResponse>> SendPostRequest<TRequest, TResponse>(String uri, String accessToken, TRequest content, CancellationToken cancellationToken)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Post, uri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);
        requestMessage.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
    }

    protected virtual async Task<Result<TResponse>> SendPutRequest<TRequest, TResponse>(String uri, String accessToken, TRequest content, CancellationToken cancellationToken)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Put, uri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);
        requestMessage.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
    }

    protected virtual async Task<Result<TResponse>> SendPatchRequest<TRequest, TResponse>(String uri, String accessToken, TRequest content, CancellationToken cancellationToken)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Patch, uri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);
        requestMessage.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
    }

    protected virtual async Task<Result<TResponse>> SendDeleteRequest<TResponse>(String uri, String accessToken, CancellationToken cancellationToken)
    {

        HttpRequestMessage requestMessage = new(HttpMethod.Delete, uri);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(AuthenticationSchemes.Bearer, accessToken);

        // Make the Http Call here
        HttpResponseMessage httpResponse = await this.HttpClient.SendAsync(requestMessage, cancellationToken);

        // Process the response
        Result<String> result = await this.HandleResponseX(httpResponse, cancellationToken);

        if (result.IsFailed)
            return ResultHelpers.CreateFailure(result);

        TResponse responseData = JsonConvert.DeserializeObject<TResponse>(result.Data);

        return Result.Success<TResponse>(responseData);
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