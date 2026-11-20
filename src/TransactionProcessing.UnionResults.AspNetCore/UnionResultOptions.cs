using Microsoft.AspNetCore.Http;

namespace TransactionProcessing.UnionResults.AspNetCore;

public sealed class UnionResultOptions
{
    private readonly Dictionary<ErrorType, int> categoryMappings = [];
    private bool includeTraceId = true;
    private bool frozen;

    public bool IncludeTraceId
    {
        get => includeTraceId;
        set { EnsureMutable(); includeTraceId = value; }
    }

    public UnionResultOptions Map(ErrorType errorType, int statusCode)
    {
        EnsureMutable();
        ValidateStatusCode(statusCode);
        categoryMappings[errorType] = statusCode;
        return this;
    }

    internal void Freeze() => frozen = true;

    internal int Resolve(ErrorType type) => categoryMappings.TryGetValue(type, out var status)
        ? status
        : type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

    private void EnsureMutable()
    {
        if (frozen) throw new InvalidOperationException("Union result options are frozen after registration.");
    }

    private static void ValidateStatusCode(int statusCode)
    {
        if (statusCode is < 400 or > 599) throw new ArgumentOutOfRangeException(nameof(statusCode), "Error status codes must be between 400 and 599.");
    }
}
