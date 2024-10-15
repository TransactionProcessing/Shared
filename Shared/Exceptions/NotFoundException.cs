using System;
using System.Collections.Generic;

namespace Shared.Exceptions
{
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    [ExcludeFromCodeCoverage]
    public class NotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class.
        /// </summary>
        public NotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public NotFoundException(String message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public NotFoundException(String message, Exception innerException) : base(message, innerException)
        {
        }

    }

    public static class ExceptionHelper
    {
        public static string GetCombinedExceptionMessages(this Exception ex)
        {
            StringBuilder sb = new();
            AppendExceptionMessages(ex, sb);
            return sb.ToString();
        }

        public static List<String> GetExceptionMessages(this Exception ex)
        {
            List<String> messages = new();
            AppendExceptionMessages(ex, messages);
            return messages;
        }

        private static void AppendExceptionMessages(Exception ex, StringBuilder sb)
        {
            if (ex == null) return;

            sb.AppendLine(ex.Message);

            if (ex.InnerException != null)
            {
                AppendExceptionMessages(ex.InnerException, sb);
            }
        }

        private static void AppendExceptionMessages(Exception ex, List<String> sb)
        {
            if (ex == null) return;

            sb.Add(ex.Message);

            if (ex.InnerException != null)
            {
                AppendExceptionMessages(ex.InnerException, sb);
            }
        }
    }
}