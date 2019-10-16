using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Shared.General;

namespace Shared.Middleware
{
    public class ResponseLoggingMiddleware
    {
        #region Fields

        /// <summary>
        /// The next
        /// </summary>
        private readonly RequestDelegate next;

        #endregion
        
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseLoggingMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        public ResponseLoggingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }
        #endregion

        #region Public Methods

        #region public async Task Invoke(HttpContext context)        
        /// <summary>
        /// Invokes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext context)
        {
            var bodyStream = context.Response.Body;

            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await next(context);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(responseBodyStream).ReadToEnd();
            StringBuilder logMessage = new StringBuilder();
            logMessage.Append($"Response: Status Code: {context.Response.StatusCode}");
            if (!String.IsNullOrEmpty(responseBody))
            {
                logMessage.Append(" ");
                logMessage.Append($"Body: {responseBody}");
            }
            Logger.LogInformation(logMessage.ToString());
            
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(bodyStream);
        }
        #endregion

        #endregion
    }
}