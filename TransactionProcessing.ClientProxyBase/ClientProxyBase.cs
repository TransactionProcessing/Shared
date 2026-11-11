using TransactionProcessing.Shared.Logger.TennantContext;
using SimpleResults;
using System;
using System.Collections.Generic;
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
            if (requestResult.IsFailed)
                return FailureResultFactory.Create(requestResult);
            using HttpRequestMessage request = requestResult.Data;

            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            return await MapNonGenericResult(method, response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return Result.Failure(exception.Message);
        }
        catch (HttpRequestException exception) {
            return Result.CriticalError(exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.CriticalError(HttpTimeoutErrorMessage);
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
            if (requestResult.IsFailed)
                return FailureResultFactory.Create(requestResult);
            using HttpRequestMessage request = requestResult.Data;
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            return await MapNonGenericResult(method, response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return Result.Failure(exception.Message);
        }
        catch (HttpRequestException exception) {
            return Result.CriticalError(exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.CriticalError(HttpTimeoutErrorMessage);
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
            if (requestResult.IsFailed)
                return FailureResultFactory.Create(requestResult);
            using HttpRequestMessage request = requestResult.Data;
            using HttpResponseMessage response = await this.HttpClient.SendAsync(request, cancellationToken);
            return await this.MapNonGenericResult(HttpMethod.Post, response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return Result.Failure(exception.Message);
        }
        catch (HttpRequestException exception) {
            return Result.CriticalError(exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.CriticalError(HttpTimeoutErrorMessage);
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
            if (requestResult.IsFailed)
                return FailureResultFactory.Create(requestResult);
            using HttpRequestMessage request = requestResult.Data;
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            return await MapGenericResult<TResponse>(method, response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return Result.Failure(exception.Message);
        }
        catch (HttpRequestException exception) {
            return Result.CriticalError(exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.CriticalError(HttpTimeoutErrorMessage);
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
            if (requestResult.IsFailed)
                return FailureResultFactory.Create(requestResult);
            using HttpRequestMessage request = requestResult.Data;
            using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
            return await MapGenericResult<TResponse>(method, response, cancellationToken);
        }
        catch (InvalidOperationException exception) {
            return Result.Failure(exception.Message);
        }
        catch (HttpRequestException exception) {
            return Result.CriticalError(exception.Message);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.CriticalError(HttpTimeoutErrorMessage);
        }
    }

    private Result<HttpRequestMessage> CreateRequest(HttpMethod method,
                                             string requestUri,
                                             object? body,
                                             string? accessToken,
                                             List<(string header, string value)>? additionalHeaders) {
        if (string.IsNullOrWhiteSpace(requestUri)) {
            return Result.Invalid("Request URI cannot be null, empty, or whitespace.");
        }

        HttpRequestMessage request = new HttpRequestMessage(method, requestUri);

        if (accessToken is not null) {
            if (string.IsNullOrWhiteSpace(accessToken)) {
                return Result.Invalid("Access token cannot be null, empty, or whitespace.");
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
                return Result.Failure($"The request body could not be serialized: {exception.Message}");
            }

            if (serialisedBody is null) {
                return Result.Failure("The request body could not be serialized.");
            }

            request.Content = new StringContent(serialisedBody, Encoding.UTF8, "application/json");
        }

        if (additionalHeaders is not null) {
            Result result = AddHeaders(request, additionalHeaders);
            if (result.IsFailed)
                return FailureResultFactory.Create(result);
        }

        return  Result.Success(request);
    }

    private Result<HttpRequestMessage> CreateRequest(HttpMethod method,
                                             string requestUri,
                                             HttpContent content,
                                             string? accessToken,
                                             List<(string header, string value)>? additionalHeaders) {
        if (content is null) {
            return Result.Invalid("Request content cannot be null");
        }
    
        Result<HttpRequestMessage> createRequestResult = CreateRequest(method, requestUri, body: null, accessToken, additionalHeaders: null);
        if (createRequestResult.IsFailed)
            return createRequestResult;

        HttpRequestMessage request = createRequestResult.Data;
        request.Content = content;

        if (additionalHeaders is not null) {
            Result result = AddHeaders(request, additionalHeaders);
            if (result.IsFailed)
                return FailureResultFactory.Create(result);
        }

        return Result.Success(request);
    }

    private Result<HttpRequestMessage> CreateMultipartRequest(string requestUri,
                                                      byte[] fileData,
                                                      string fileName,
                                                      List<(string field, string value)> formFields,
                                                      string accessToken,
                                                      List<(string header, string value)>? additionalHeaders) {
        if (fileData is null) {
            return Result.Invalid("file data content cannot be null");
        }

        if (string.IsNullOrWhiteSpace(fileName)) {
            return Result.Invalid("file name cannot be null, empty, or whitespace.");
        }

        if (formFields is null) {
            return Result.Invalid("form fields cannot be null");
        }

        Result<HttpRequestMessage> createRequestResult = CreateRequest(HttpMethod.Post, requestUri, body: null, accessToken, additionalHeaders: null);
        if (createRequestResult.IsFailed)
            return FailureResultFactory.Create(createRequestResult);

        HttpRequestMessage request = createRequestResult.Data;
        
        Result<MultipartFormDataContent> createMultipartContentResult = CreateMultipartContent(fileData, fileName, formFields);
        if (createMultipartContentResult.IsFailed)
            return FailureResultFactory.Create(createMultipartContentResult);

        request.Content = createMultipartContentResult.Data;

        if (additionalHeaders is not null) {
            Result result = AddHeaders(request, additionalHeaders);
            if (result.IsFailed)
                return FailureResultFactory.Create(result);
        }

        return request;
    }

    private static Result AddHeaders(HttpRequestMessage request,
                                   List<(string header, string value)> additionalHeaders) {
        foreach ((String header, String value) in additionalHeaders) {
            if (string.IsNullOrWhiteSpace(header)) {
                return Result.Invalid("Header name cannot be null, empty, or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(value)) {
                return Result.Invalid($"Header value for '{header}' cannot be null, empty, or whitespace.");
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

            return Result.Invalid($"The header '{header}' could not be added to the request.");
        }

        return Result.Success();
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
                return Result.Invalid("Form field name cannot be null, empty, or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(value)) {
                return Result.Invalid($"Form field value for '{field}' cannot be null, empty, or whitespace.");
            }

            multipartContent.Add(new StringContent(value), field);
        }

        return Result.Success(multipartContent);
    }

    private async Task<Result> MapNonGenericResult(HttpMethod method,
                                                   HttpResponseMessage response,
                                                   CancellationToken cancellationToken) {
        if (response.IsSuccessStatusCode) {
            return MapSuccessfulResult(method, response.StatusCode);
        }

        return await MapFailedResult(response, cancellationToken);
    }

    private async Task<Result<TResponse>> MapGenericResult<TResponse>(HttpMethod method,
                                                                      HttpResponseMessage response,
                                                                      CancellationToken cancellationToken) {
        if (!response.IsSuccessStatusCode) {
            return await MapFailedResult(response, cancellationToken);
        }

        String rawContent = await ReadContent(response, cancellationToken);
        if (string.IsNullOrWhiteSpace(rawContent)) {
            return Result.Failure("The response content was empty.");
        }

        if (typeof(TResponse) == typeof(string)) {
            return Result.Success((TResponse)(object)rawContent);
        }

        object deserialisedValue;

        try {
            deserialisedValue = Deserialise(rawContent, typeof(TResponse));
        }
        catch (Exception exception) {
            return Result.Failure($"The response body could not be deserialized: {exception.Message}");
        }

        if (deserialisedValue is null) {
            return Result.Failure("The response body could not be deserialized.");
        }

        if (deserialisedValue is not TResponse value) {
            return Result.Failure($"The deserialized response type '{deserialisedValue.GetType().FullName}' is not assignable to '{typeof(TResponse).FullName}'.");
        }

        return method == HttpMethod.Get ? Result.ObtainedResource(value) : Result.Success(value);
    }

    private static Result MapSuccessfulResult(HttpMethod method,
                                              HttpStatusCode statusCode) {
        if (method == HttpMethod.Delete) {
            return Result.DeletedResource();
        }

        if (method == HttpMethod.Post && statusCode == HttpStatusCode.Created) {
            return Result.CreatedResource();
        }

        if (method == HttpMethod.Put || method == HttpMethod.Patch) {
            return Result.UpdatedResource();
        }

        return Result.Success();
    }

    private static async Task<Result> MapFailedResult(HttpResponseMessage response,
                                                      CancellationToken cancellationToken) {
        String rawContent = await ReadContent(response, cancellationToken);
        Result<IReadOnlyList<String>> errorMessages = ExtractErrors(rawContent);
        if (errorMessages.IsFailed)
            return FailureResultFactory.Create(errorMessages);

        String primaryMessage = BuildPrimaryMessage(response, errorMessages.Data);

        return response.StatusCode switch {
            HttpStatusCode.BadRequest => Result.Invalid(primaryMessage, errorMessages.Data),
            HttpStatusCode.Unauthorized => Result.Unauthorized(primaryMessage, errorMessages.Data),
            HttpStatusCode.Forbidden => Result.Forbidden(primaryMessage, errorMessages.Data),
            HttpStatusCode.NotFound => Result.NotFound(primaryMessage, errorMessages.Data),
            HttpStatusCode.Conflict => Result.Conflict(primaryMessage, errorMessages.Data),
            _ when (int)response.StatusCode >= 500 => Result.CriticalError(primaryMessage, errorMessages.Data),
            _ => Result.Failure(primaryMessage, errorMessages.Data)
        };
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
            return Result.Success<IReadOnlyList<String>>(new List<String>());
        }

        try {
            using JsonDocument document = JsonDocument.Parse(rawContent);
            List<String> errors = new List<string>();
            Result result = AppendErrors(document.RootElement, errors);
            if (result.IsFailed)
                return FailureResultFactory.Create(result);

            return errors.Count == 0 ? [rawContent] : errors;
        }
        catch (JsonException) {
            return Result.Success<IReadOnlyList<String>>([rawContent]);
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
                return Result.Invalid($"Invalid Value Kind {element.ValueKind}");
        }

        return Result.Success();
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
