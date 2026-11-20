using System.Diagnostics.CodeAnalysis;

namespace TransactionProcessing.UnionResults;

public enum ErrorType
{
    Validation,
    NotFound,
    Unauthorized,
    Forbidden,
    Conflict,
    Failure,
    Critical,
    Unknown
}

public sealed record class Ok;
public sealed record class Ok<T>(T Value);
public sealed record class Created<T>(T Value, string? Location = null);
public sealed record class NoContent;

public sealed record class Error
{
    public Error(string code, string message, ErrorType errorType, string? field = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(message);
        Code = code;
        Message = message;
        ErrorType = errorType;
        Field = field;
    }

    public string Code { get; }
    public string Message { get; }
    public ErrorType ErrorType { get; }
    public string? Field { get; }

    public static Error Failure(string message) => Create(ErrorType.Failure, message);
    public static Error Validation(string message, string? field = null) => Create(ErrorType.Validation, message, field);
    public static Error NotFound(string message) => Create(ErrorType.NotFound, message);
    public static Error Unauthorized(string message) => Create(ErrorType.Unauthorized, message);
    public static Error Forbidden(string message) => Create(ErrorType.Forbidden, message);
    public static Error Conflict(string message) => Create(ErrorType.Conflict, message);
    public static Error Critical(string message) => Create(ErrorType.Critical, message);

    private static Error Create(ErrorType type, string message, string? field = null) => new(type.ToString(), message, type, field);
}

public sealed class ErrorList : IReadOnlyList<Error>
{
    private readonly Error[] errors;

    public ErrorList(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        this.errors = errors.ToArray();
        if (this.errors.Length == 0)
            throw new ArgumentException("An error list must contain at least one error.", nameof(errors));
        if (this.errors.Any(static error => error is null))
            throw new ArgumentException("An error list cannot contain null errors.", nameof(errors));
        ErrorType = this.errors[0].ErrorType;
        if (this.errors.Any(error => error.ErrorType != ErrorType))
            throw new ArgumentException("All errors in an error list must have the same category.", nameof(errors));
    }

    public ErrorType ErrorType { get; }
    public int Count => errors.Length;
    public Error this[int index] => errors[index];
    public IEnumerator<Error> GetEnumerator() => ((IEnumerable<Error>)errors).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => errors.GetEnumerator();
}

public enum ResultKind { Ok, Created, NoContent, Error, ErrorList }
public readonly record struct ResultOutcome(ResultKind Kind, object? Value = null, string? Location = null, Error? Error = null, ErrorList? Errors = null);
public interface IResultOutcome { ResultOutcome GetOutcome(); }

public union Result(Ok, NoContent, Error, ErrorList) : IResultOutcome
{
    private const string UnspecifiedErrorMessage = "An unspecified error occurred.";

    internal static Error Uninitialized { get; } = new("Result.Uninitialized", "An uninitialized result has no outcome.", ErrorType.Critical);
    internal static Error MissingError { get; } = new("Result.MissingError", "A failed result requires an error.", ErrorType.Critical);

    public bool IsSuccess => this is TransactionProcessing.UnionResults.Ok or TransactionProcessing.UnionResults.NoContent;
    public bool IsFailed => !IsSuccess;
    public string Message => FailureMessage ?? string.Empty;
    public IReadOnlyList<Error> Errors => this switch
    {
        Error error => [error],
        ErrorList errors => errors,
        TransactionProcessing.UnionResults.Ok or TransactionProcessing.UnionResults.NoContent => [],
        null => [Uninitialized],
    };

    public string? GetFailureMessage() => FailureMessage;

    internal string? FailureMessage => this switch
    {
        Error error => error.Message,
        ErrorList errors => errors[0].Message,
        TransactionProcessing.UnionResults.Ok or TransactionProcessing.UnionResults.NoContent => null,
        null => Uninitialized.Message,
    };

    public static Result Ok() => new(new Ok());
    public static Result Success() => Ok();
    public static Result<T> Success<T>(T value) => Result<T>.Ok(value);
    public static Result NoContent() => new(new NoContent());
    public static Result Fail(Error error) => new(error ?? MissingError);
    public static Result Fail(ErrorList errors) => errors is null ? Fail(MissingError) : new(errors);
    public static Error Failure(string message) => Error.Failure(message ?? UnspecifiedErrorMessage);
    public static ErrorList Failure(IEnumerable<string> errors) => CreateErrors(ErrorType.Failure, string.Empty, errors);
    public static ErrorList Failure(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Failure, message, errors);
    public static Error Validation(string message) => Error.Validation(message ?? UnspecifiedErrorMessage);
    public static Error Invalid(string message) => Validation(message);
    public static ErrorList Invalid(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Validation, message, errors);
    public static ErrorList Validation(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Validation, message, errors);
    public static Error NotFound(string message) => Error.NotFound(message ?? UnspecifiedErrorMessage);
    public static ErrorList NotFound(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.NotFound, message, errors);
    public static Error Unauthorized(string message) => Error.Unauthorized(message ?? UnspecifiedErrorMessage);
    public static ErrorList Unauthorized(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Unauthorized, message, errors);
    public static Error Forbidden(string message) => Error.Forbidden(message ?? UnspecifiedErrorMessage);
    public static ErrorList Forbidden(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Forbidden, message, errors);
    public static Error Conflict(string message) => Error.Conflict(message ?? UnspecifiedErrorMessage);
    public static ErrorList Conflict(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Conflict, message, errors);
    public static Error CriticalError(string message) => Error.Critical(message ?? UnspecifiedErrorMessage);
    public static ErrorList CriticalError(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Critical, message, errors);
    public static Result CreatedResource() => Ok();
    public static Result DeletedResource() => NoContent();
    public static Result UpdatedResource() => NoContent();
    public static Result<T> ObtainedResource<T>(T value) => Result<T>.Ok(value);

    private static ErrorList CreateErrors(ErrorType type, string message, IEnumerable<string> errors)
    {
        return Capture(() =>
        {
            List<Error> values = [];
            if (!string.IsNullOrWhiteSpace(message))
                values.Add(new Error(type.ToString(), message, type));

            if (errors is not null)
            {
                foreach (var value in errors)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        values.Add(new Error(type.ToString(), value, type));
                }
            }

            if (values.Count == 0)
                values.Add(new Error(type.ToString(), UnspecifiedErrorMessage, type));

            return new ErrorList(values);
        }, static error => new ErrorList([error]));
    }

    public ResultOutcome GetOutcome() => this switch
    {
        TransactionProcessing.UnionResults.Ok _ => new(ResultKind.Ok),
        TransactionProcessing.UnionResults.NoContent _ => new(ResultKind.NoContent),
        Error error => new(ResultKind.Error, Error: error),
        ErrorList errors => new(ResultKind.ErrorList, Errors: errors),
        null => new(ResultKind.Error, Error: Uninitialized),
    };

    public Result<TResult> Match<TResult>(Func<TResult> ok, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors)
    {
        var result = this;
        return Capture(() => Result<TResult>.Ok(result.MatchCore(ok, noContent, error, errors)), Result<TResult>.Fail);
    }

    public Result Bind(Func<Result> bind) => BindCore(bind);

    internal Result BindCore(Func<Result> bind) => MatchCore(
        () => Capture(bind, Fail), NoContent, Fail, Fail);

    public Result Tap(Action action) => TapCore(action);

    internal Result TapCore(Action action)
    {
        var result = this;
        Result TapSuccess() => Capture(() => { action(); return result; }, Fail);

        return MatchCore(TapSuccess, TapSuccess, Fail, Fail);
    }

    public Result TapError(Action<Error> action) => TapErrorCore(action);

    internal Result TapErrorCore(Action<Error> action)
    {
        var result = this;
        return MatchCore(
            () => result,
            () => result,
            error => Capture(() => { action(error); return Fail(error); }, Fail),
            errors => Capture(() => { NotifyErrors(errors, action); return Fail(errors); }, Fail));
    }

    private TResult MatchCore<TResult>(Func<TResult> ok, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors) => this switch
    {
        TransactionProcessing.UnionResults.Ok => ok(),
        TransactionProcessing.UnionResults.NoContent => noContent(),
        Error value => error(value),
        ErrorList values => errors(values),
        null => error(Uninitialized),
    };

    internal static void NotifyErrors(ErrorList errors, Action<Error> action)
    {
        foreach (var error in errors)
            action(error);
    }

    internal static TResult Capture<TResult>(Func<TResult> action, Func<Error, TResult> fail)
    {
        try
        {
            return action();
        }
        catch (Exception exception)
        {
            return fail(FromException(exception));
        }
    }

    internal static Error FromException(Exception exception) => new("Result.Exception", exception.Message ?? UnspecifiedErrorMessage, ErrorType.Critical);
}

#pragma warning disable S1481 // Sonar misreads the C# 15 union declaration body as an unused local.
public union Result<T>(Ok<T>, Created<T>, NoContent, Error, ErrorList) : IResultOutcome
{
    private static readonly Error MissingValue = new("Result.MissingValue", "A successful result requires a value.", ErrorType.Critical);
    private static readonly Error ExpectedFailure = new("Result.ExpectedFailure", "Only a failed result can be propagated as a failure.", ErrorType.Critical);

    public bool IsSuccess => this is Ok<T> or Created<T> or TransactionProcessing.UnionResults.NoContent;
    public bool IsFailed => !IsSuccess;

    public string Message => GetFailureMessage() ?? string.Empty;
    public IReadOnlyList<Error> Errors => this switch
    {
        Error error => [error],
        ErrorList errors => errors,
        Ok<T> or Created<T> or TransactionProcessing.UnionResults.NoContent => [],
        null => [Result.Uninitialized],
    };

    public string? GetFailureMessage() => this switch
    {
        Error error => error.Message,
        ErrorList errors => errors[0].Message,
        Ok<T> or Created<T> or TransactionProcessing.UnionResults.NoContent => null,
        null => Result.Uninitialized.Message,
    };

    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        switch (this)
        {
            case Ok<T> ok:
                value = ok.Value;
                return true;
            case Created<T> created:
                value = created.Value;
                return true;
            default:
                value = default;
                return false;
        }
    }

    public static Result<T> Ok(T value) => value is null ? Fail(MissingValue) : new(new Ok<T>(value));
    public static Result<T> Created(T value, string? location = null) => value is null ? Fail(MissingValue) : new(new Created<T>(value, location));
    public static Result<T> NoContent() => new(new NoContent());
    public static Result<T> Fail(Error error) => new(error ?? Result.MissingError);
    public static Result<T> Fail(ErrorList errors) => errors is null ? Fail(Result.MissingError) : new(errors);

    public static Result<T> FromFailure(IResultOutcome result) => Result.Capture(() => result?.GetOutcome() switch
    {
        { Kind: ResultKind.Error, Error: not null } outcome => Fail(outcome.Error),
        { Kind: ResultKind.ErrorList, Errors: not null } outcome => Fail(outcome.Errors),
        _ => Fail(ExpectedFailure),
    }, Fail);

    public ResultOutcome GetOutcome() => this switch
    {
        Ok<T> ok => new(ResultKind.Ok, Value: ok.Value),
        Created<T> created => new(ResultKind.Created, Value: created.Value, Location: created.Location),
        TransactionProcessing.UnionResults.NoContent _ => new(ResultKind.NoContent),
        Error error => new(ResultKind.Error, Error: error),
        ErrorList errors => new(ResultKind.ErrorList, Errors: errors),
        null => new(ResultKind.Error, Error: Result.Uninitialized),
    };

    public Result ToResult() => MatchCore(
        _ => Result.Ok(), (_, _) => Result.Ok(), Result.NoContent, Result.Fail, Result.Fail);

    public Result<TResult> Match<TResult>(Func<T, TResult> ok, Func<T, string?, TResult> created, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors)
    {
        var result = this;
        return Result.Capture(() => Result<TResult>.Ok(result.MatchCore(ok, created, noContent, error, errors)), Result<TResult>.Fail);
    }

    public Result<TResult> Map<TResult>(Func<T, TResult> map) => MatchCore(
        value => Result.Capture(() => Result<TResult>.Ok(map(value)), Result<TResult>.Fail),
        (value, location) => Result.Capture(() => Result<TResult>.Created(map(value), location), Result<TResult>.Fail),
        Result<TResult>.NoContent, Result<TResult>.Fail, Result<TResult>.Fail);

    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> bind) => MatchCore(
        value => Result.Capture(() => bind(value), Result<TResult>.Fail),
        (value, _) => Result.Capture(() => bind(value), Result<TResult>.Fail),
        Result<TResult>.NoContent, Result<TResult>.Fail, Result<TResult>.Fail);

    public Result<T> Tap(Action<T> action)
    {
        var result = this;
        Result<T> TapValue(T value) => Result.Capture(() => { action(value); return result; }, Fail);

        return MatchCore(TapValue, (value, _) => TapValue(value), () => result, Fail, Fail);
    }

    public Result<T> TapError(Action<Error> action)
    {
        var result = this;
        return MatchCore(
            _ => result,
            (_, _) => result,
            () => result,
            error => Result.Capture(() => { action(error); return Fail(error); }, Fail),
            errors => Result.Capture(() => { Result.NotifyErrors(errors, action); return Fail(errors); }, Fail));
    }

    internal TResult MatchCore<TResult>(Func<T, TResult> ok, Func<T, string?, TResult> created, Func<TResult> noContent, Func<Error, TResult> error, Func<ErrorList, TResult> errors) => this switch
    {
        Ok<T> value => ok(value.Value),
        Created<T> value => created(value.Value, value.Location),
        TransactionProcessing.UnionResults.NoContent => noContent(),
        Error value => error(value),
        ErrorList values => errors(values),
        null => error(Result.Uninitialized),
    };

    internal static async Task<Result<T>> CaptureAsync(Func<Task<Result<T>>> action, CancellationToken cancellationToken)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return Fail(Result.FromException(exception));
        }
    }
}
#pragma warning restore S1481
