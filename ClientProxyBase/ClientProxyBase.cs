using System;
using SimpleResults;

namespace ClientProxyBase
{
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ClientProxyBase
    {
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
        protected ClientProxyBase(HttpClient httpClient)
        {
            this.HttpClient = httpClient;
        }

        #endregion

        #region Methods

        protected virtual async Task<String> HandleResponse(HttpResponseMessage responseMessage,
                                                            CancellationToken cancellationToken)
        {
            String result = String.Empty;

            // Read the content from the response
            String content = await responseMessage.Content.ReadAsStringAsync(cancellationToken);

            // Check the response code
            if (!responseMessage.IsSuccessStatusCode)
            {
                // throw a specific  exception to inherited class
                switch (responseMessage.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        throw new InvalidOperationException(content);
                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        throw new UnauthorizedAccessException(content);
                    case HttpStatusCode.NotFound:
                        throw new KeyNotFoundException(content);
                    case HttpStatusCode.InternalServerError:
                        throw new Exception("An internal error has occurred");
                    default:
                        throw new Exception($"An internal error has occurred ({responseMessage.StatusCode})");
                }
            }

            // Set the result
            result = content;

            return result;
        }

        /// <summary>
        /// Handles the response.
        /// </summary>
        /// <param name="responseMessage">The response message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        /// <exception cref="System.UnauthorizedAccessException"></exception>
        /// <exception cref="InvalidDataException"></exception>
        /// <exception cref="System.Exception">An internal error has occurred
        /// or
        /// An internal error has occurred</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="UnauthorizedAccessException"></exception>
        /// <exception cref="Exception">An internal error has occurred
        /// or
        /// An internal error has occurred</exception>
        protected virtual async Task<Result<StringResult>> HandleResponseX(HttpResponseMessage responseMessage,
                                                                           CancellationToken cancellationToken)
        {
           
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
            
            return Result.Success(new StringResult(content));
        }

        #endregion

        public Result CreateFailure(Result result)
        {
            if (result.IsFailed)
            {
                return BuildResult(result.Status, result.Message, result.Errors);
            }
            return Result.Failure("Unknown Failure");
        }

        public Result CreateFailure<T>(Result<T> result)
        {
            if (result.IsFailed)
            {
                return BuildResult(result.Status, result.Message, result.Errors);
            }
            return Result.Failure("Unknown Failure");
        }

        private static Result BuildResult(ResultStatus status, String messageValue, IEnumerable<String> errorList)
        {
            return (status, messageValue, errorList) switch
            {
                // If the status is NotFound and there are errors, return the errors
                (ResultStatus.NotFound, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.NotFound(errors),

                // If the status is NotFound and the message is not null or empty, return the message
                (ResultStatus.NotFound, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.NotFound(message),

                // If the status is Failure and there are errors, return the errors
                (ResultStatus.Failure, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Failure(errors),

                // If the status is Failure and the message is not null or empty, return the message
                (ResultStatus.Failure, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.Failure(message),

                // If the status is Forbidden and there are errors, return the errors
                (ResultStatus.Forbidden, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Forbidden(errors),

                // If the status is Forbidden and the message is not null or empty, return the message
                (ResultStatus.Forbidden, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.NotFound(message),
                //###
                // If the status is Invalid and there are errors, return the errors
                (ResultStatus.Invalid, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Invalid(errors),

                // If the status is Invalid and the message is not null or empty, return the message
                (ResultStatus.Invalid, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.Invalid(message),

                // If the status is Unauthorized and there are errors, return the errors
                (ResultStatus.Unauthorized, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Unauthorized(errors),

                // If the status is Unauthorized and the message is not null or empty, return the message
                (ResultStatus.Unauthorized, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.Unauthorized(message),

                // If the status is Conflict and there are errors, return the errors
                (ResultStatus.Conflict, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.Conflict(errors),

                // If the status is Conflict and the message is not null or empty, return the message
                (ResultStatus.Conflict, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.Conflict(message),

                // If the status is CriticalError and there are errors, return the errors
                (ResultStatus.CriticalError, _, List<string> errors) when errors is { Count: > 0 } =>
                    Result.CriticalError(errors),

                // If the status is CriticalError and the message is not null or empty, return the message
                (ResultStatus.CriticalError, string message, _) when !string.IsNullOrEmpty(message) =>
                    Result.CriticalError(message),

                // Default case, return a generic failure message
                _ => Result.Failure("An unexpected error occurred.")
            };
        }
    }

    public record StringResult(String StringData);
}
