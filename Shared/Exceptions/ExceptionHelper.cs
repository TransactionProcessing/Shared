using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Exceptions;

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