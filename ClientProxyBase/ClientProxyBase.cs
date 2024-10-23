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
            String content = await responseMessage.Content.ReadAsStringAsync();

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
        protected virtual async Task<Result<String>> HandleResponseX(HttpResponseMessage responseMessage,
                                                            CancellationToken cancellationToken)
        {
            String result = String.Empty;
            
            // Read the content from the response
            // Cant passs cancellation token as net standard does not support this :|
            String content = await responseMessage.Content.ReadAsStringAsync();

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

            // Set the result
            result = content;

            return result;
        }

        #endregion
    }
}
