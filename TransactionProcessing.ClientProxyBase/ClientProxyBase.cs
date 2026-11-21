#nullable enable

using TransactionProcessing.Shared.Logger.TennantContext;
using TransactionProcessing.UnionResults;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionProcessing.ClientProxyBase;

public abstract class ClientProxyBase {
    private readonly HttpClient HttpClient;
    private readonly Func<object, string> Serialise;
    private readonly Func<string, Type, object> Deserialise;

    public ClientProxyBase(HttpClient httpClient,
                           Func<object, string> serialise,
                           Func<string, Type, object> deserialise) {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Serialise = serialise ?? throw new ArgumentNullException(nameof(serialise));
        Deserialise = deserialise ?? throw new ArgumentNullException(nameof(deserialise));
    }

    public Task<Result<TResponse>> Get<TResponse>(string requestUri,
                                                  CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Get, requestUri, body: null, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Get<TResponse>(string requestUri,
                                                  List<(string header, string value)> additionalHeaders,
                                                  CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Get, requestUri, body: null, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Get<TResponse>(string requestUri,
                                                  string accessToken,
                                                  CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Get, requestUri, body: null, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Get<TResponse>(string requestUri,
                                                  string accessToken,
                                                  List<(string header, string value)> additionalHeaders,
                                                  CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Get, requestUri, body: null, accessToken, additionalHeaders, cancellationToken);

    public Task<Result> Delete(string requestUri,
                               CancellationToken cancellationToken = default) =>
        SendWithoutResponse<object?>(HttpMethod.Delete, requestUri, body: null, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result> Delete(string requestUri,
                               List<(string header, string value)> additionalHeaders,
                               CancellationToken cancellationToken = default) =>
        SendWithoutResponse<object?>(HttpMethod.Delete, requestUri, body: null, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result> Delete(string requestUri,
                               string accessToken,
                               CancellationToken cancellationToken = default) =>
        SendWithoutResponse<object?>(HttpMethod.Delete, requestUri, body: null, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result> Delete(string requestUri,
                               string accessToken,
                               List<(string header, string value)> additionalHeaders,
                               CancellationToken cancellationToken = default) =>
        SendWithoutResponse<object?>(HttpMethod.Delete, requestUri, body: null, accessToken, additionalHeaders, cancellationToken);

    public Task<Result> Post<TRequest>(string requestUri,
                                       TRequest request,
                                       CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Post, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result> Post<TRequest>(string requestUri,
                                       TRequest request,
                                       List<(string header, string value)> additionalHeaders,
                                       CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Post, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result> Post<TRequest>(string requestUri,
                                       TRequest request,
                                       string accessToken,
                                       CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Post, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result> Post<TRequest>(string requestUri,
                                       TRequest request,
                                       string accessToken,
                                       List<(string header, string value)> additionalHeaders,
                                       CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Post, requestUri, request, accessToken, additionalHeaders, cancellationToken);

    public Task<Result> Post(string requestUri,
                             HttpContent content,
                             CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Post, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result> Post(string requestUri,
                             HttpContent content,
                             List<(string header, string value)> additionalHeaders,
                             CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Post, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result> Post(string requestUri,
                             HttpContent content,
                             string accessToken,
                             CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Post, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result> Post(string requestUri,
                             HttpContent content,
                             string accessToken,
                             List<(string header, string value)> additionalHeaders,
                             CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Post, requestUri, content, accessToken, additionalHeaders, cancellationToken);

    public Task<Result> Post(string requestUri,
                             byte[] fileData,
                             string fileName,
                             List<(string field, string value)> formFields,
                             string accessToken,
                             CancellationToken cancellationToken = default) =>
        SendMultipartWithoutResponse(requestUri, fileData, fileName, formFields, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result> Post(string requestUri,
                             byte[] fileData,
                             string fileName,
                             List<(string field, string value)> formFields,
                             string accessToken,
                             List<(string header, string value)> additionalHeaders,
                             CancellationToken cancellationToken = default) =>
        SendMultipartWithoutResponse(requestUri, fileData, fileName, formFields, accessToken, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Post<TRequest, TResponse>(string requestUri,
                                                             TRequest request,
                                                             CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Post, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Post<TRequest, TResponse>(string requestUri,
                                                             TRequest request,
                                                             List<(string header, string value)> additionalHeaders,
                                                             CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Post, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Post<TRequest, TResponse>(string requestUri,
                                                             TRequest request,
                                                             string accessToken,
                                                             CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Post, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Post<TRequest, TResponse>(string requestUri,
                                                             TRequest request,
                                                             string accessToken,
                                                             List<(string header, string value)> additionalHeaders,
                                                             CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Post, requestUri, request, accessToken, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Post<TResponse>(string requestUri,
                                                   HttpContent content,
                                                   CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Post, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Post<TResponse>(string requestUri,
                                                   HttpContent content,
                                                   List<(string header, string value)> additionalHeaders,
                                                   CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Post, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Post<TResponse>(string requestUri,
                                                   HttpContent content,
                                                   string accessToken,
                                                   CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Post, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Post<TResponse>(string requestUri,
                                                   HttpContent content,
                                                   string accessToken,
                                                   List<(string header, string value)> additionalHeaders,
                                                   CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Post, requestUri, content, accessToken, additionalHeaders, cancellationToken);

    public Task<Result> Put<TRequest>(string requestUri,
                                      TRequest request,
                                      CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Put, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result> Put<TRequest>(string requestUri,
                                      TRequest request,
                                      List<(string header, string value)> additionalHeaders,
                                      CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Put, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result> Put<TRequest>(string requestUri,
                                      TRequest request,
                                      string accessToken,
                                      CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Put, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result> Put<TRequest>(string requestUri,
                                      TRequest request,
                                      string accessToken,
                                      List<(string header, string value)> additionalHeaders,
                                      CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Put, requestUri, request, accessToken, additionalHeaders, cancellationToken);

    public Task<Result> Put(string requestUri,
                            HttpContent content,
                            CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Put, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result> Put(string requestUri,
                            HttpContent content,
                            List<(string header, string value)> additionalHeaders,
                            CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Put, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result> Put(string requestUri,
                            HttpContent content,
                            string accessToken,
                            CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Put, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result> Put(string requestUri,
                            HttpContent content,
                            string accessToken,
                            List<(string header, string value)> additionalHeaders,
                            CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Put, requestUri, content, accessToken, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Put<TRequest, TResponse>(string requestUri,
                                                            TRequest request,
                                                            CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Put, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Put<TRequest, TResponse>(string requestUri,
                                                            TRequest request,
                                                            List<(string header, string value)> additionalHeaders,
                                                            CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Put, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Put<TRequest, TResponse>(string requestUri,
                                                            TRequest request,
                                                            string accessToken,
                                                            CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Put, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Put<TRequest, TResponse>(string requestUri,
                                                            TRequest request,
                                                            string accessToken,
                                                            List<(string header, string value)> additionalHeaders,
                                                            CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Put, requestUri, request, accessToken, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Put<TResponse>(string requestUri,
                                                  HttpContent content,
                                                  CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Put, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Put<TResponse>(string requestUri,
                                                  HttpContent content,
                                                  List<(string header, string value)> additionalHeaders,
                                                  CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Put, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Put<TResponse>(string requestUri,
                                                  HttpContent content,
                                                  string accessToken,
                                                  CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Put, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Put<TResponse>(string requestUri,
                                                  HttpContent content,
                                                  string accessToken,
                                                  List<(string header, string value)> additionalHeaders,
                                                  CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Put, requestUri, content, accessToken, additionalHeaders, cancellationToken);

    public Task<Result> Patch<TRequest>(string requestUri,
                                        TRequest request,
                                        CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Patch, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result> Patch<TRequest>(string requestUri,
                                        TRequest request,
                                        List<(string header, string value)> additionalHeaders,
                                        CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Patch, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result> Patch<TRequest>(string requestUri,
                                        TRequest request,
                                        string accessToken,
                                        CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Patch, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result> Patch<TRequest>(string requestUri,
                                        TRequest request,
                                        string accessToken,
                                        List<(string header, string value)> additionalHeaders,
                                        CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Patch, requestUri, request, accessToken, additionalHeaders, cancellationToken);

    public Task<Result> Patch(string requestUri,
                              HttpContent content,
                              CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Patch, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result> Patch(string requestUri,
                              HttpContent content,
                              List<(string header, string value)> additionalHeaders,
                              CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Patch, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result> Patch(string requestUri,
                              HttpContent content,
                              string accessToken,
                              CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Patch, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result> Patch(string requestUri,
                              HttpContent content,
                              string accessToken,
                              List<(string header, string value)> additionalHeaders,
                              CancellationToken cancellationToken = default) =>
        SendWithoutResponse(HttpMethod.Patch, requestUri, content, accessToken, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Patch<TRequest, TResponse>(string requestUri,
                                                              TRequest request,
                                                              CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Patch, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Patch<TRequest, TResponse>(string requestUri,
                                                              TRequest request,
                                                              List<(string header, string value)> additionalHeaders,
                                                              CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Patch, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Patch<TRequest, TResponse>(string requestUri,
                                                              TRequest request,
                                                              string accessToken,
                                                              CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Patch, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Patch<TRequest, TResponse>(string requestUri,
                                                              TRequest request,
                                                              string accessToken,
                                                              List<(string header, string value)> additionalHeaders,
                                                              CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Patch, requestUri, request, accessToken, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Patch<TResponse>(string requestUri,
                                                    HttpContent content,
                                                    CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Patch, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Patch<TResponse>(string requestUri,
                                                    HttpContent content,
                                                    List<(string header, string value)> additionalHeaders,
                                                    CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Patch, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

    public Task<Result<TResponse>> Patch<TResponse>(string requestUri,
                                                    HttpContent content,
                                                    string accessToken,
                                                    CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Patch, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

    public Task<Result<TResponse>> Patch<TResponse>(string requestUri,
                                                    HttpContent content,
                                                    string accessToken,
                                                    List<(string header, string value)> additionalHeaders,
                                                    CancellationToken cancellationToken = default) =>
        SendForResponse<TResponse>(HttpMethod.Patch, requestUri, content, accessToken, additionalHeaders, cancellationToken);

    private async Task<Result> SendWithoutResponse<TRequest>(HttpMethod method,
                                                             string requestUri,
                                                             TRequest? body,
                                                             string? accessToken,
                                                             List<(string header, string value)>? additionalHeaders,
                                                             CancellationToken cancellationToken) {
        try {
            Result<HttpRequestMessage> requestResult = CreateRequest(method, requestUri, body, accessToken, additionalHeaders);
            if (!TryGetValueOrFailure(requestResult, out var requestMessage, out var failure))
                return failure;

            using HttpRequestMessage request = requestMessage;

            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            return await MapNonGenericResult(response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return Results.Fail(exception.Message);
        }
        catch (HttpRequestException exception) {
            return Results.Critical(exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Results.Critical(HttpTimeoutErrorMessage);
        }
    }

    private const String HttpTimeoutErrorMessage = "The HTTP request timed out.";

    private async Task<Result> SendWithoutResponse(HttpMethod method,
                                                   string requestUri,
                                                   HttpContent content,
                                                   string? accessToken,
                                                   List<(string header, string value)>? additionalHeaders,
                                                   CancellationToken cancellationToken) {
        try {
            Result<HttpRequestMessage> requestResult = CreateRequest(method, requestUri, content, accessToken, additionalHeaders);
            if (!TryGetValueOrFailure(requestResult, out var requestMessage, out var failure))
                return failure;

            using HttpRequestMessage request = requestMessage;
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            return await MapNonGenericResult(response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return Results.Fail(exception.Message);
        }
        catch (HttpRequestException exception) {
            return Results.Critical(exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Results.Critical(HttpTimeoutErrorMessage);
        }
    }

    private async Task<Result> SendMultipartWithoutResponse(string requestUri,
                                                            byte[] fileData,
                                                            string fileName,
                                                            List<(string field, string value)> formFields,
                                                            string accessToken,
                                                            List<(string header, string value)>? additionalHeaders,
                                                            CancellationToken cancellationToken) {
        try {
            Result<HttpRequestMessage> requestResult = CreateMultipartRequest(requestUri, fileData, fileName, formFields, accessToken, additionalHeaders);
            if (!TryGetValueOrFailure(requestResult, out var requestMessage, out var failure))
                return failure;

            using HttpRequestMessage request = requestMessage;
            using HttpResponseMessage response = await this.HttpClient.SendAsync(request, cancellationToken);
            return await MapNonGenericResult(response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return Results.Fail(exception.Message);
        }
        catch (HttpRequestException exception) {
            return Results.Critical(exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Results.Critical(HttpTimeoutErrorMessage);
        }
    }

    private async Task<Result<TResponse>> SendForResponse<TResponse>(HttpMethod method,
                                                                     string requestUri,
                                                                     object? body,
                                                                     string? accessToken,
                                                                     List<(string header, string value)>? additionalHeaders,
                                                                     CancellationToken cancellationToken) {
        try {
            Result<HttpRequestMessage> requestResult = CreateRequest(method, requestUri, body, accessToken, additionalHeaders);
            if (!TryGetValueOrFailure(requestResult, out var requestMessage, out var failure))
                return failure;

            using HttpRequestMessage request = requestMessage;
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            return await MapGenericResult<TResponse>(response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return new Error(ErrorType.Failure, exception.Message);
        }
        catch (HttpRequestException exception) {
            return new Error(ErrorType.Critical, exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return new Error(ErrorType.Critical, HttpTimeoutErrorMessage);
        }
    }

    private async Task<Result<TResponse>> SendForResponse<TResponse>(HttpMethod method,
                                                                     string requestUri,
                                                                     HttpContent content,
                                                                     string? accessToken,
                                                                     List<(string header, string value)>? additionalHeaders,
                                                                     CancellationToken cancellationToken) {
        try {
            Result<HttpRequestMessage> requestResult = CreateRequest(method, requestUri, content, accessToken, additionalHeaders);
            if (!TryGetValueOrFailure(requestResult, out var requestMessage, out var failure))
                return failure;

            using HttpRequestMessage request = requestMessage;
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            return await MapGenericResult<TResponse>(response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return new Error(ErrorType.Failure, exception.Message);
        }
        catch (HttpRequestException exception) {
            return new Error(ErrorType.Critical, exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return new Error(ErrorType.Critical, HttpTimeoutErrorMessage);
        }
    }

    private Result<HttpRequestMessage> CreateRequest(HttpMethod method,
                                             string requestUri,
                                             object? body,
                                             string? accessToken,
                                             List<(string header, string value)>? additionalHeaders) {
        if (string.IsNullOrWhiteSpace(requestUri)) {
            return new Error(ErrorType.Validation, "Request URI cannot be null, empty, or whitespace.");
        }

        HttpRequestMessage request = new HttpRequestMessage(method, requestUri);

        if (accessToken is not null) {
            if (string.IsNullOrWhiteSpace(accessToken)) {
                return new Error(ErrorType.Validation, "Access token cannot be null, empty, or whitespace.");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (TenantContext.CurrentTenant != null) {
            request.Headers.Add(TenantContext.KeyNameCorrelationId, TenantContext.CurrentTenant.CorrelationId.ToString());
        }

        if (body is HttpContent httpContent) {
            request.Content = httpContent;
        }
        else if (body is not null) {
            string serialisedBody;

            try {
                serialisedBody = Serialise(body);
            }
            catch (Exception exception) {
                return new Error(ErrorType.Failure, $"The request body could not be serialized: {exception.Message}");
            }

            if (serialisedBody is null) {
                return new Error(ErrorType.Failure, "The request body could not be serialized.");
            }

            request.Content = new StringContent(serialisedBody, Encoding.UTF8, "application/json");
        }

        if (additionalHeaders is not null) {
            Result result = AddHeaders(request, additionalHeaders);
            if (!result.IsSuccess())
                return ToFailure<HttpRequestMessage>(result);
        }

        return Results.Ok(request);
    }

    private Result<HttpRequestMessage> CreateRequest(HttpMethod method,
                                             string requestUri,
                                             HttpContent content,
                                             string? accessToken,
                                             List<(string header, string value)>? additionalHeaders) {
        if (content is null) {
            return new Error(ErrorType.Validation, "Request content cannot be null");
        }
    
        Result<HttpRequestMessage> createRequestResult = CreateRequest(method, requestUri, body: null, accessToken, additionalHeaders: null);
        if (!TryGetValueOrFailure(createRequestResult, out var request, out var requestFailure))
            return requestFailure;
        request.Content = content;

        if (additionalHeaders is not null) {
            Result result = AddHeaders(request, additionalHeaders);
            if (!result.IsSuccess())
                return ToFailure<HttpRequestMessage>(result);
        }

        return Results.Ok(request);
    }

    private Result<HttpRequestMessage> CreateMultipartRequest(string requestUri,
                                                      byte[] fileData,
                                                      string fileName,
                                                      List<(string field, string value)> formFields,
                                                      string accessToken,
                                                      List<(string header, string value)>? additionalHeaders) {
        if (fileData is null) {
            return new Error(ErrorType.Validation, "file data content cannot be null");
        }

        if (string.IsNullOrWhiteSpace(fileName)) {
            return new Error(ErrorType.Validation, "file name cannot be null, empty, or whitespace.");
        }

        if (formFields is null) {
            return new Error(ErrorType.Validation, "form fields cannot be null");
        }

        Result<HttpRequestMessage> createRequestResult = CreateRequest(HttpMethod.Post, requestUri, body: null, accessToken, additionalHeaders: null);
        if (!TryGetValueOrFailure(createRequestResult, out var request, out var requestFailure))
            return requestFailure;
        
        Result<MultipartFormDataContent> createMultipartContentResult = CreateMultipartContent(fileData, fileName, formFields);
        if (!TryGetValueOrFailure(createMultipartContentResult, out var multipartContent, out var contentFailure))
            return contentFailure;

        request.Content = multipartContent;

        if (additionalHeaders is not null) {
            Result result = AddHeaders(request, additionalHeaders);
            if (!result.IsSuccess())
                return ToFailure<HttpRequestMessage>(result);
        }

        return Results.Ok(request);
    }

    private static Result AddHeaders(HttpRequestMessage request,
                                   List<(string header, string value)> additionalHeaders) {
        foreach ((String header, String value) in additionalHeaders) {
            if (string.IsNullOrWhiteSpace(header)) {
                return Results.Validation("Header name cannot be null, empty, or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(value)) {
                return Results.Validation($"Header value for '{header}' cannot be null, empty, or whitespace.");
            }

            if (string.Equals(header, TenantContext.KeyNameCorrelationId, StringComparison.OrdinalIgnoreCase)
                && request.Headers.Contains(TenantContext.KeyNameCorrelationId)) {
                continue;
            }

            if (request.Headers.TryAddWithoutValidation(header, value)) {
                continue;
            }

            if (request.Content is not null && request.Content.Headers.TryAddWithoutValidation(header, value)) {
                continue;
            }

            return Results.Validation($"The header '{header}' could not be added to the request.");
        }

        return Results.Ok();
    }

    private static Result<MultipartFormDataContent> CreateMultipartContent(byte[] fileData,
                                                                           string fileName,
                                                                           List<(string field, string value)> formFields) {
        MultipartFormDataContent multipartContent = new MultipartFormDataContent();

        ByteArrayContent fileContent = new ByteArrayContent(fileData);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        multipartContent.Add(fileContent, "file", fileName);

        foreach ((String field, String value) in formFields) {
            if (string.IsNullOrWhiteSpace(field)) {
                return new Error(ErrorType.Validation, "Form field name cannot be null, empty, or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(value)) {
                return new Error(ErrorType.Validation, $"Form field value for '{field}' cannot be null, empty, or whitespace.");
            }

            multipartContent.Add(new StringContent(value), field);
        }

        return Results.Ok(multipartContent);
    }

    private static async Task<Result> MapNonGenericResult(HttpResponseMessage response,
                                                   CancellationToken cancellationToken) {
        if (response.IsSuccessStatusCode) {
            return Results.Ok();
        }

        return await MapFailedResult(response, cancellationToken);
    }

    private async Task<Result<TResponse>> MapGenericResult<TResponse>(HttpResponseMessage response,
                                                                      CancellationToken cancellationToken) {
        if (!response.IsSuccessStatusCode) {
            return await MapFailedResult(response, cancellationToken);
        }

        String? rawContent = await ReadContent(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(rawContent)) {
            return new Error(ErrorType.Failure, "The response content was empty.");
        }

        if (typeof(TResponse) == typeof(string)) {
            return Results.Ok((TResponse)(object)rawContent);
        }

        object deserialisedValue;

        try {
            deserialisedValue = Deserialise(rawContent, typeof(TResponse));
        }
        catch (Exception exception) {
            return new Error(ErrorType.Failure, $"The response body could not be deserialized: {exception.Message}");
        }

        if (deserialisedValue is null) {
            return new Error(ErrorType.Failure, "The response body could not be deserialized.");
        }

        if (deserialisedValue is not TResponse value) {
            return new Error(ErrorType.Failure, $"The deserialized response type '{deserialisedValue.GetType().FullName}' is not assignable to '{typeof(TResponse).FullName}'.");
        }

        return Results.Ok(value);
    }

    private static bool TryGetValueOrFailure<T>(Result<T> result,
                                                 [MaybeNullWhen(false)] out T value,
                                                 [NotNullWhen(false)] out ErrorList? failure) {
        if (result.Value is Success<T> success) {
            value = success.Value;
            failure = null;
            return true;
        }

        value = default;
        failure = result.Value switch {
            Error error => new ErrorList([error]),
            ErrorList errors => errors,
            _ => new ErrorList([new Error(ErrorType.Critical, "An uninitialized result has no outcome.")])
        };
        return false;
    }

    private static Result<T> ToFailure<T>(Result result) => result.Value switch {
        Error error => error,
        ErrorList errors => errors,
        _ => throw new InvalidOperationException("Only a failed result can be propagated as a failure.")
    };

    private static async Task<ErrorList> MapFailedResult(HttpResponseMessage response,
                                                      CancellationToken cancellationToken) {
        String? rawContent = await ReadContent(response, cancellationToken);
        Result<IReadOnlyList<String>> extractionResult = ExtractErrors(rawContent);
        if (!TryGetValueOrFailure(extractionResult, out var errorMessages, out var failure))
            return failure;

        String primaryMessage = BuildPrimaryMessage(response, errorMessages);

        ErrorType errorType = response.StatusCode switch {
            HttpStatusCode.BadRequest => ErrorType.Validation,
            HttpStatusCode.Unauthorized => ErrorType.Unauthorized,
            HttpStatusCode.Forbidden => ErrorType.Forbidden,
            HttpStatusCode.NotFound => ErrorType.NotFound,
            HttpStatusCode.Conflict => ErrorType.Conflict,
            _ when (int)response.StatusCode >= 500 => ErrorType.Critical,
            _ => ErrorType.Failure
        };

        List<Error> errors = new List<Error>(errorMessages.Count + 1);
        if (!string.IsNullOrWhiteSpace(primaryMessage)) {
            errors.Add(new Error(errorType, primaryMessage));
        }

        foreach (string message in errorMessages) {
            errors.Add(new Error(errorType, message));
        }

        if (errors.Count == 0) {
            errors.Add(new Error(errorType, "An unspecified error occurred."));
        }

        return new ErrorList(errors);
    }

    private static async Task<string?> ReadContent(HttpResponseMessage response,
                                                   CancellationToken cancellationToken) {
        if (response.Content is null) {
            return null;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static Result<IReadOnlyList<string>> ExtractErrors(string? rawContent) {
        if (string.IsNullOrWhiteSpace(rawContent)) {
            return Results.Ok<IReadOnlyList<String>>(new List<String>());
        }

        try {
            using JsonDocument document = JsonDocument.Parse(rawContent);
            List<String> errors = new List<string>();
            Result result = AppendErrors(document.RootElement, errors);
            if (!result.IsSuccess())
                return ToFailure<IReadOnlyList<String>>(result);

            return Results.Ok<IReadOnlyList<String>>(errors.Count == 0 ? [rawContent] : errors);
        }
        catch (JsonException) {
            return Results.Ok<IReadOnlyList<String>>([rawContent]);
        }
    }

    private static readonly string[] MessageProperties = [
        "title",
        "detail",
        "message"
    ];

    private static Result AppendErrors(JsonElement element,
                                     ICollection<string> errors) {
        switch (element.ValueKind) {
            case JsonValueKind.Object:
                AppendObjectErrors(element, errors);
                break;

            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray()) {
                    AppendErrors(item, errors);
                }
                break;

            case JsonValueKind.String:
                AddIfPresent(errors, element.GetString());
                break;
            default:
                return Results.Validation($"Invalid Value Kind {element.ValueKind}");
        }

        return Results.Ok();
    }

    private static void AppendObjectErrors(JsonElement element,
                                           ICollection<string> errors) {
        foreach (String propertyName in MessageProperties) {
            if (element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String) {
                AddIfPresent(errors, value.GetString());
            }
        }

        if (element.TryGetProperty("errors", out JsonElement nestedErrors)) {
            AppendErrors(nestedErrors, errors);
        }

        foreach (JsonProperty property in element.EnumerateObject()) {
            if (IsReservedProperty(property.Name)) {
                continue;
            }

            if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array) {
                AppendErrors(property.Value, errors);
            }
        }
    }

    private static bool IsReservedProperty(string propertyName) => propertyName is "title" or "detail" or "message" or "errors";

    private static void AddIfPresent(ICollection<string> errors,
                                     string? value) {
        if (!string.IsNullOrWhiteSpace(value)) {
            errors.Add(value);
        }
    }

    private static string BuildPrimaryMessage(HttpResponseMessage response, IReadOnlyList<string> errors)
    {
        if (errors.Count > 0)
        {
            return errors[0];
        }

        return response.ReasonPhrase ?? $"Request failed with status code {(int)response.StatusCode}.";
    }
}
