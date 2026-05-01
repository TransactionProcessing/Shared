using SimpleResults;
using System;
using System.Linq;

namespace ClientProxyBase;

using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

public abstract partial class ClientProxyBase {

    protected readonly HttpClient HttpClient;
    private readonly Func<Object, String> Serialise;
    private readonly Func<String, Type, Object> Deserialise;

    protected ClientProxyBase(HttpClient httpClient, Func<Object, String> Serialise, Func<String, Type, Object> Deserialise) {
        this.HttpClient = httpClient;
        this.Serialise = Serialise;
        this.Deserialise = Deserialise;
    }

    // This ctor will be removed once all the client proxies have been updated to use the new ctor above. It is kept for backwards compatibility.
    protected ClientProxyBase(HttpClient httpClient) {
        this.HttpClient = httpClient;

        JsonSerializerOptions JsonSerializerOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    typeInfo =>
                    {
                        String[] names = new[] { "AggregateId", "AggregateVersion", "EventId", "EventNumber", "EventTimestamp", "EventType" };
                        List<JsonPropertyInfo> matches = typeInfo.Properties
                            .Where(p => names.Any(n => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)))
                            .ToList();

                        foreach (JsonPropertyInfo match in matches) {
                            match.ShouldSerialize = (_, _) => false;
                        }
                    }
                }
            }
        };

        this.Serialise = (obj) => System.Text.Json.JsonSerializer.Serialize(obj, JsonSerializerOptions);
        this.Deserialise = (json, type) => System.Text.Json.JsonSerializer.Deserialize(json, type, JsonSerializerOptions);
    }


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

        return this.DeserialiseContent<ResponseData<T>>(content);
    }

    protected virtual T DeserialiseContent<T>(String content) =>
        (T)this.Deserialise(content, typeof(T));

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
