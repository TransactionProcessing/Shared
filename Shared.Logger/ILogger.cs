namespace Shared.Logger;

using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;

/// <summary>
/// 
/// </summary>
public interface ILogger
{

    /// <summary>
    /// Gets or sets a value indicating whether this instance is initialised.
    /// </summary>
    /// <value>
    ///   <c>true</c> if this instance is initialised; otherwise, <c>false</c>.
    /// </value>
    Boolean IsInitialised { get; set; }



    void LogCritical(Exception exception);

    void LogCritical(String messageTemplate, // NOSONAR - delegates to the required string-only overload, so it is not recursive.
                     params Object?[] args) => this.LogCritical(FormatMessageTemplate(messageTemplate, args));

    void LogCritical(Exception exception,
                     String messageTemplate,
                     params Object?[] args) => this.LogCritical(FormatMessageTemplate(messageTemplate, args), exception);

    void LogCritical(String message,
                     Exception exception);

    void LogDebug(String message);

    void LogDebug(String messageTemplate,
                  params Object?[] args) => this.LogDebug(FormatMessageTemplate(messageTemplate, args));

    void LogError(Exception exception);

    void LogError(String messageTemplate,
                  params Object?[] args) => this.LogError(FormatMessageTemplate(messageTemplate, args));

    void LogError(Exception exception,
                  String messageTemplate,
                  params Object?[] args) => this.LogError(FormatMessageTemplate(messageTemplate, args), exception);

    void LogError(String message);

    void LogError(String message,
                  Exception exception);

    void LogInformation(String message);

    void LogInformation(String messageTemplate,
                        params Object?[] args) => this.LogInformation(FormatMessageTemplate(messageTemplate, args));

    void LogTrace(String message);

    void LogTrace(String messageTemplate,
                  params Object?[] args) => this.LogTrace(FormatMessageTemplate(messageTemplate, args));

    void LogWarning(String message);

    void LogWarning(String messageTemplate,
                    params Object?[] args) => this.LogWarning(FormatMessageTemplate(messageTemplate, args));

    private static String FormatMessageTemplate(String messageTemplate, Object?[] args)
    {
        if (args == null || args.Length == 0) return messageTemplate;
        StringBuilder result = new(messageTemplate.Length + args.Length * 8);
        Int32 argumentIndex = 0;
        Int32 index = 0;
        while (index < messageTemplate.Length)
        {
            if (messageTemplate[index] == '{' && index + 1 < messageTemplate.Length && messageTemplate[index + 1] == '{')
            {
                result.Append("{{"); index += 2; continue;
            }
            if (messageTemplate[index] == '{')
            {
                Int32 end = messageTemplate.IndexOf('}', index + 1);
                if (end >= 0 && argumentIndex < args.Length)
                {
                    result.Append(FormatArgument(args[argumentIndex++]));
                    index = end + 1;
                    continue;
                }
            }
            result.Append(messageTemplate[index++]);
        }
        return result.ToString();
    }

    private static String FormatArgument(Object? argument)
    {
        if (argument is IEnumerable && argument is not String)
        {
            try
            {
                return JsonSerializer.Serialize(argument, argument.GetType());
            }
            catch (NotSupportedException)
            {
                // Unsupported collection types use the invariant scalar fallback below.
            }
            catch (JsonException)
            {
                // Collections that cannot be serialized also use the invariant scalar fallback below.
            }
        }
        return Convert.ToString(argument, CultureInfo.InvariantCulture) ?? String.Empty;
    }

}
