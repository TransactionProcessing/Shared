using System.Reflection;

namespace TransactionProcessing.UnionResults;

public static class ResultHelpers {
    public static ErrorList CreateFailure(object result) {
        ArgumentNullException.ThrowIfNull(result);

        if (result is IResultOutcome unionResult) {
            return unionResult.GetOutcome() switch {
                       { Kind: ResultKind.Error, Error: not null } outcome => new ErrorList([outcome.Error]),
                       { Kind: ResultKind.ErrorList, Errors: not null } outcome => outcome.Errors,
                       _ => throw new InvalidOperationException("Only a failed result can be propagated as a failure."),
                   };
        }

        Type resultType = result.GetType();
        bool isFailed = resultType.GetProperty("IsFailed", BindingFlags.Instance | BindingFlags.Public)?.GetValue(result) as bool? ?? false;
        if (!isFailed) {
            throw new InvalidOperationException("Only a failed result can be propagated as a failure.");
        }

        string? message = resultType.GetProperty("Message", BindingFlags.Instance | BindingFlags.Public)?.GetValue(result) as string;
        return new ErrorList([Error.Failure(string.IsNullOrWhiteSpace(message) ? "The operation failed." : message)]);
    }
}
