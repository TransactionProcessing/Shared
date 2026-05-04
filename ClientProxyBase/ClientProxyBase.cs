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

namespace ClientProxyBase
{
    public abstract class ClientProxyBase
    {
        private readonly HttpClient HttpClient;
        private readonly Func<object, string> Serialise;
        private readonly Func<string, Type, object> Deserialise;

        public ClientProxyBase(
            HttpClient httpClient,
            Func<object, string> serialise,
            Func<string, Type, object> deserialise)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            Serialise = serialise ?? throw new ArgumentNullException(nameof(serialise));
            Deserialise = deserialise ?? throw new ArgumentNullException(nameof(deserialise));
        }

        public Task<Result<TResponse>> Get<TResponse>(string requestUri, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Get, requestUri, body: null, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Get<TResponse>(string requestUri, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Get, requestUri, body: null, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Get<TResponse>(string requestUri, string accessToken, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Get, requestUri, body: null, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Get<TResponse>(string requestUri, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Get, requestUri, body: null, accessToken, additionalHeaders, cancellationToken);

        public Task<Result> Delete(string requestUri, CancellationToken cancellationToken = default)
            => SendWithoutResponse<object?>(HttpMethod.Delete, requestUri, body: null, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result> Delete(string requestUri, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse<object?>(HttpMethod.Delete, requestUri, body: null, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result> Delete(string requestUri, string accessToken, CancellationToken cancellationToken = default)
            => SendWithoutResponse<object?>(HttpMethod.Delete, requestUri, body: null, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result> Delete(string requestUri, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse<object?>(HttpMethod.Delete, requestUri, body: null, accessToken, additionalHeaders, cancellationToken);

        public Task<Result> Post<TRequest>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Post, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result> Post<TRequest>(string requestUri, TRequest request, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Post, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result> Post<TRequest>(string requestUri, TRequest request, string accessToken, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Post, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result> Post<TRequest>(string requestUri, TRequest request, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Post, requestUri, request, accessToken, additionalHeaders, cancellationToken);

        public Task<Result> Post(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Post, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result> Post(string requestUri, HttpContent content, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Post, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result> Post(string requestUri, HttpContent content, string accessToken, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Post, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result> Post(string requestUri, HttpContent content, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Post, requestUri, content, accessToken, additionalHeaders, cancellationToken);

        public Task<Result> Post(string requestUri, byte[] fileData, string fileName, List<(string field, string value)> formFields, string accessToken, CancellationToken cancellationToken = default)
            => SendMultipartWithoutResponse(requestUri, fileData, fileName, formFields, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result> Post(string requestUri, byte[] fileData, string fileName, List<(string field, string value)> formFields, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendMultipartWithoutResponse(requestUri, fileData, fileName, formFields, accessToken, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Post<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Post, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Post<TRequest, TResponse>(string requestUri, TRequest request, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Post, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Post<TRequest, TResponse>(string requestUri, TRequest request, string accessToken, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Post, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Post<TRequest, TResponse>(string requestUri, TRequest request, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Post, requestUri, request, accessToken, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Post<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Post, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Post<TResponse>(string requestUri, HttpContent content, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Post, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Post<TResponse>(string requestUri, HttpContent content, string accessToken, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Post, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Post<TResponse>(string requestUri, HttpContent content, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Post, requestUri, content, accessToken, additionalHeaders, cancellationToken);

        public Task<Result> Put<TRequest>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Put, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result> Put<TRequest>(string requestUri, TRequest request, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Put, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result> Put<TRequest>(string requestUri, TRequest request, string accessToken, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Put, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result> Put<TRequest>(string requestUri, TRequest request, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Put, requestUri, request, accessToken, additionalHeaders, cancellationToken);

        public Task<Result> Put(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Put, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result> Put(string requestUri, HttpContent content, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Put, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result> Put(string requestUri, HttpContent content, string accessToken, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Put, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result> Put(string requestUri, HttpContent content, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Put, requestUri, content, accessToken, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Put<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Put, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Put<TRequest, TResponse>(string requestUri, TRequest request, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Put, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Put<TRequest, TResponse>(string requestUri, TRequest request, string accessToken, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Put, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Put<TRequest, TResponse>(string requestUri, TRequest request, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Put, requestUri, request, accessToken, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Put<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Put, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Put<TResponse>(string requestUri, HttpContent content, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Put, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Put<TResponse>(string requestUri, HttpContent content, string accessToken, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Put, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Put<TResponse>(string requestUri, HttpContent content, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Put, requestUri, content, accessToken, additionalHeaders, cancellationToken);

        public Task<Result> Patch<TRequest>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Patch, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result> Patch<TRequest>(string requestUri, TRequest request, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Patch, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result> Patch<TRequest>(string requestUri, TRequest request, string accessToken, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Patch, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result> Patch<TRequest>(string requestUri, TRequest request, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Patch, requestUri, request, accessToken, additionalHeaders, cancellationToken);

        public Task<Result> Patch(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Patch, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result> Patch(string requestUri, HttpContent content, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Patch, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result> Patch(string requestUri, HttpContent content, string accessToken, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Patch, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result> Patch(string requestUri, HttpContent content, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendWithoutResponse(HttpMethod.Patch, requestUri, content, accessToken, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Patch<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Patch, requestUri, request, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Patch<TRequest, TResponse>(string requestUri, TRequest request, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Patch, requestUri, request, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Patch<TRequest, TResponse>(string requestUri, TRequest request, string accessToken, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Patch, requestUri, request, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Patch<TRequest, TResponse>(string requestUri, TRequest request, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Patch, requestUri, request, accessToken, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Patch<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Patch, requestUri, content, accessToken: null, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Patch<TResponse>(string requestUri, HttpContent content, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Patch, requestUri, content, accessToken: null, additionalHeaders, cancellationToken);

        public Task<Result<TResponse>> Patch<TResponse>(string requestUri, HttpContent content, string accessToken, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Patch, requestUri, content, accessToken, additionalHeaders: null, cancellationToken);

        public Task<Result<TResponse>> Patch<TResponse>(string requestUri, HttpContent content, string accessToken, List<(string header, string value)> additionalHeaders, CancellationToken cancellationToken = default)
            => SendForResponse<TResponse>(HttpMethod.Patch, requestUri, content, accessToken, additionalHeaders, cancellationToken);

        private async Task<Result> SendWithoutResponse<TRequest>(HttpMethod method,
                                                                 string requestUri,
                                                                 TRequest? body,
                                                                 string? accessToken,
                                                                 List<(string header, string value)>? additionalHeaders,
                                                                 CancellationToken cancellationToken)
        {
            try
            {
                using var request = CreateRequest(method, requestUri, body, accessToken, additionalHeaders);
                using var response = await HttpClient.SendAsync(request, cancellationToken);
                return await MapNonGenericResult(method, response, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure(exception.Message);
            }
            catch (HttpRequestException exception)
            {
                return Result.CriticalError(exception.Message);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result.CriticalError("The HTTP request timed out.");
            }
        }

        private async Task<Result> SendWithoutResponse(HttpMethod method,
                                                       string requestUri,
                                                       HttpContent content,
                                                       string? accessToken,
                                                       List<(string header, string value)>? additionalHeaders,
                                                       CancellationToken cancellationToken)
        {
            try
            {
                using var request = CreateRequest(method, requestUri, content, accessToken, additionalHeaders);
                using var response = await HttpClient.SendAsync(request, cancellationToken);
                return await MapNonGenericResult(method, response, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure(exception.Message);
            }
            catch (HttpRequestException exception)
            {
                return Result.CriticalError(exception.Message);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result.CriticalError("The HTTP request timed out.");
            }
        }

        private async Task<Result> SendMultipartWithoutResponse(
            string requestUri,
            byte[] fileData,
            string fileName,
            List<(string field, string value)> formFields,
            string accessToken,
            List<(string header, string value)>? additionalHeaders,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = CreateMultipartRequest(requestUri, fileData, fileName, formFields, accessToken, additionalHeaders);
                using var response = await HttpClient.SendAsync(request, cancellationToken);
                return await MapNonGenericResult(HttpMethod.Post, response, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure(exception.Message);
            }
            catch (HttpRequestException exception)
            {
                return Result.CriticalError(exception.Message);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result.CriticalError("The HTTP request timed out.");
            }
        }

        private async Task<Result<TResponse>> SendForResponse<TResponse>(HttpMethod method,
                                                                          string requestUri,
                                                                          object? body,
                                                                         string? accessToken,
                                                                         List<(string header, string value)>? additionalHeaders,
                                                                         CancellationToken cancellationToken)
        {
            try
            {
                using var request = CreateRequest(method, requestUri, body, accessToken, additionalHeaders);
                using var response = await HttpClient.SendAsync(request, cancellationToken);
                return await MapGenericResult<TResponse>(method, response, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure(exception.Message);
            }
            catch (HttpRequestException exception)
            {
                return Result.CriticalError(exception.Message);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result.CriticalError("The HTTP request timed out.");
            }
        }

        private async Task<Result<TResponse>> SendForResponse<TResponse>(HttpMethod method,
                                                                         string requestUri,
                                                                         HttpContent content,
                                                                         string? accessToken,
                                                                         List<(string header, string value)>? additionalHeaders,
                                                                         CancellationToken cancellationToken)
        {
            try
            {
                using var request = CreateRequest(method, requestUri, content, accessToken, additionalHeaders);
                using var response = await HttpClient.SendAsync(request, cancellationToken);
                return await MapGenericResult<TResponse>(method, response, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                return Result.Failure(exception.Message);
            }
            catch (HttpRequestException exception)
            {
                return Result.CriticalError(exception.Message);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return Result.CriticalError("The HTTP request timed out.");
            }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method,
                                                 string requestUri,
                                                 object? body,
                                                 string? accessToken,
                                                 List<(string header, string value)>? additionalHeaders)
        {
            if (string.IsNullOrWhiteSpace(requestUri))
            {
                throw new ArgumentException("Request URI cannot be null, empty, or whitespace.", nameof(requestUri));
            }

            var request = new HttpRequestMessage(method, requestUri);

            if (accessToken is not null)
            {
                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new ArgumentException("Access token cannot be null, empty, or whitespace.", nameof(accessToken));
                }

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }

            if (body is HttpContent httpContent)
            {
                request.Content = httpContent;
            }
            else if (body is not null)
            {
                string serialisedBody;

                try
                {
                    serialisedBody = Serialise(body);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"The request body could not be serialized: {exception.Message}", exception);
                }

                if (serialisedBody is null)
                {
                    throw new InvalidOperationException("The request body could not be serialized.");
                }

                request.Content = new StringContent(serialisedBody, Encoding.UTF8, "application/json");
            }

            if (additionalHeaders is not null)
            {
                AddHeaders(request, additionalHeaders);
            }

            return request;
        }

        private HttpRequestMessage CreateRequest(HttpMethod method,
                                                 string requestUri,
                                                 HttpContent content,
                                                 string? accessToken,
                                                 List<(string header, string value)>? additionalHeaders)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            var request = CreateRequest(method, requestUri, body: null, accessToken, additionalHeaders: null);
            request.Content = content;

            if (additionalHeaders is not null)
            {
                AddHeaders(request, additionalHeaders);
            }

            return request;
        }

        private HttpRequestMessage CreateMultipartRequest(
            string requestUri,
            byte[] fileData,
            string fileName,
            List<(string field, string value)> formFields,
            string accessToken,
            List<(string header, string value)>? additionalHeaders)
        {
            if (fileData is null)
            {
                throw new ArgumentNullException(nameof(fileData));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null, empty, or whitespace.", nameof(fileName));
            }

            if (formFields is null)
            {
                throw new ArgumentNullException(nameof(formFields));
            }

            var request = CreateRequest(HttpMethod.Post, requestUri, body: null, accessToken, additionalHeaders: null);
            request.Content = CreateMultipartContent(fileData, fileName, formFields);

            if (additionalHeaders is not null)
            {
                AddHeaders(request, additionalHeaders);
            }

            return request;
        }

        private static void AddHeaders(HttpRequestMessage request, List<(string header, string value)> additionalHeaders)
        {
            foreach (var (header, value) in additionalHeaders)
            {
                if (string.IsNullOrWhiteSpace(header))
                {
                    throw new ArgumentException("Header name cannot be null, empty, or whitespace.", nameof(additionalHeaders));
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException($"Header value for '{header}' cannot be null, empty, or whitespace.", nameof(additionalHeaders));
                }

                if (request.Headers.TryAddWithoutValidation(header, value))
                {
                    continue;
                }

                if (request.Content is not null && request.Content.Headers.TryAddWithoutValidation(header, value))
                {
                    continue;
                }

                throw new InvalidOperationException($"The header '{header}' could not be added to the request.");
            }
        }

        private static MultipartFormDataContent CreateMultipartContent(
            byte[] fileData,
            string fileName,
            List<(string field, string value)> formFields)
        {
            var multipartContent = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(fileData);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            multipartContent.Add(fileContent, "file", fileName);

            foreach (var (field, value) in formFields)
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    throw new ArgumentException("Form field name cannot be null, empty, or whitespace.", nameof(formFields));
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException($"Form field value for '{field}' cannot be null, empty, or whitespace.", nameof(formFields));
                }

                multipartContent.Add(new StringContent(value), field);
            }

            return multipartContent;
        }

        private async Task<Result> MapNonGenericResult(HttpMethod method,
                                                       HttpResponseMessage response,
                                                       CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return MapSuccessfulResult(method, response.StatusCode);
            }

            return await MapFailedResult(response, cancellationToken);
        }

        private async Task<Result<TResponse>> MapGenericResult<TResponse>(HttpMethod method,
                                                                          HttpResponseMessage response,
                                                                          CancellationToken cancellationToken)
        {
            if (!response.IsSuccessStatusCode)
            {
                return await MapFailedResult(response, cancellationToken);
            }

            var rawContent = await ReadContent(response, cancellationToken);
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return Result.Failure("The response content was empty.");
            }

            if (typeof(TResponse) == typeof(string))
            {
                return Result.Success((TResponse)(object)rawContent);
            }

            object deserialisedValue;

            try
            {
                deserialisedValue = Deserialise(rawContent, typeof(TResponse));
            }
            catch (Exception exception)
            {
                return Result.Failure($"The response body could not be deserialized: {exception.Message}");
            }

            if (deserialisedValue is null)
            {
                return Result.Failure("The response body could not be deserialized.");
            }

            if (deserialisedValue is not TResponse value)
            {
                return Result.Failure(
                    $"The deserialized response type '{deserialisedValue.GetType().FullName}' is not assignable to '{typeof(TResponse).FullName}'.");
            }

            return method == HttpMethod.Get
                ? Result.ObtainedResource(value)
                : Result.Success(value);
        }

        private static Result MapSuccessfulResult(HttpMethod method, HttpStatusCode statusCode)
        {
            if (method == HttpMethod.Delete)
            {
                return Result.DeletedResource();
            }

            if (method == HttpMethod.Post && statusCode == HttpStatusCode.Created)
            {
                return Result.CreatedResource();
            }

            if (method == HttpMethod.Put || method == HttpMethod.Patch)
            {
                return Result.UpdatedResource();
            }

            return Result.Success();
        }

        private static async Task<Result> MapFailedResult(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var rawContent = await ReadContent(response, cancellationToken);
            var errorMessages = ExtractErrors(rawContent);
            var primaryMessage = BuildPrimaryMessage(response, errorMessages);

            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => Result.Invalid(primaryMessage, errorMessages),
                HttpStatusCode.Unauthorized => Result.Unauthorized(primaryMessage, errorMessages),
                HttpStatusCode.Forbidden => Result.Forbidden(primaryMessage, errorMessages),
                HttpStatusCode.NotFound => Result.NotFound(primaryMessage, errorMessages),
                HttpStatusCode.Conflict => Result.Conflict(primaryMessage, errorMessages),
                _ when (int)response.StatusCode >= 500 => Result.CriticalError(primaryMessage, errorMessages),
                _ => Result.Failure(primaryMessage, errorMessages)
            };
        }

        private static async Task<string?> ReadContent(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.Content is null)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }

        private static IReadOnlyList<string> ExtractErrors(string? rawContent)
        {
            if (string.IsNullOrWhiteSpace(rawContent))
            {
                return [];
            }

            try
            {
                using var document = JsonDocument.Parse(rawContent);
                var errors = new List<string>();
                AppendErrors(document.RootElement, errors);

                return errors.Count == 0 ? [rawContent] : errors;
            }
            catch (JsonException)
            {
                return [rawContent];
            }
        }

        private static void AppendErrors(JsonElement element, ICollection<string> errors)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                {
                    AddIfPresent(errors, title.GetString());
                }

                if (element.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                {
                    AddIfPresent(errors, detail.GetString());
                }

                if (element.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                {
                    AddIfPresent(errors, message.GetString());
                }

                if (element.TryGetProperty("errors", out var nestedErrors))
                {
                    AppendErrors(nestedErrors, errors);
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("title") || property.NameEquals("detail") || property.NameEquals("message") || property.NameEquals("errors"))
                    {
                        continue;
                    }

                    if (property.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object)
                    {
                        AppendErrors(property.Value, errors);
                    }
                }

                return;
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    AppendErrors(item, errors);
                }

                return;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                AddIfPresent(errors, element.GetString());
            }
        }

        private static void AddIfPresent(ICollection<string> errors, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
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
}
