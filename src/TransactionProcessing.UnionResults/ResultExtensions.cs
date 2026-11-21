namespace TransactionProcessing.UnionResults;

public static class ResultExtensions {
        public static bool IsSuccess(this Result result)
            => result.Value is Success;

        public static bool IsSuccess<T>(this Result<T> result)
            => result.Value is Success<T>;

        public static IReadOnlyList<Error> GetErrors(this Result result)
            => ExtractErrors(result.Value);

        public static IReadOnlyList<Error> GetErrors<T>(this Result<T> result)
            => ExtractErrors(result.Value);

        public static string? GetErrorMessage(this Result result)
            => FormatErrors(result.GetErrors());

        public static string? GetErrorMessage<T>(this Result<T> result)
            => FormatErrors(result.GetErrors());

        private static IReadOnlyList<Error> ExtractErrors(object? value)
            => value switch {
                   Error error => [error],
                   ErrorList errors => errors,
                   null => [Results.Uninitialized],
                   _ => []
               };

        private static string? FormatErrors(IReadOnlyList<Error> errors)
            => errors.Count == 0
                ? null
                : string.Join("; ", errors.Select(e => e.Message));
}
