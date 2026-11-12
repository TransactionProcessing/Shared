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
    public bool IsSuccess => this switch
    {
        TransactionProcessing.UnionResults.Ok or TransactionProcessing.UnionResults.NoContent => true,
        Error or ErrorList => false,
        null => throw new InvalidOperationException("A default Result has no value."),
    };
    public bool IsFailed => !IsSuccess;
    public string Message => this.GetFailureMessage() ?? string.Empty;
    public IReadOnlyList<Error> Errors => this switch
    {
        Error error => [error], ErrorList errors => errors,
        TransactionProcessing.UnionResults.Ok or TransactionProcessing.UnionResults.NoContent => [],
        null => throw new InvalidOperationException("A default Result has no value."),
    };
    public static Result Ok() => new(new Ok());
    public static Result Success() => Ok();
    public static Result<T> Success<T>(T value) => Result<T>.Ok(value);
    public static Result NoContent() => new(new NoContent());
    public static Result Fail(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));
    public static Result Fail(ErrorList errors) => new(errors ?? throw new ArgumentNullException(nameof(errors)));
    public static Error Failure(string message) => Error.Failure(message);
    public static ErrorList Failure(IEnumerable<string> errors) => CreateErrors(ErrorType.Failure, string.Empty, errors);
    public static ErrorList Failure(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Failure, message, errors);
    public static Error Validation(string message) => Error.Validation(message);
    public static Error Invalid(string message) => Error.Validation(message);
    public static ErrorList Invalid(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Validation, message, errors);
    public static ErrorList Validation(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Validation, message, errors);
    public static Error NotFound(string message) => Error.NotFound(message);
    public static ErrorList NotFound(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.NotFound, message, errors);
    public static Error Unauthorized(string message) => Error.Unauthorized(message);
    public static ErrorList Unauthorized(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Unauthorized, message, errors);
    public static Error Forbidden(string message) => Error.Forbidden(message);
    public static ErrorList Forbidden(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Forbidden, message, errors);
    public static Error Conflict(string message) => Error.Conflict(message);
    public static ErrorList Conflict(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Conflict, message, errors);
    public static Error CriticalError(string message) => Error.Critical(message);
    public static ErrorList CriticalError(string message, IEnumerable<string> errors) => CreateErrors(ErrorType.Critical, message, errors);
    public static Result CreatedResource() => Ok();
    public static Result DeletedResource() => NoContent();
    public static Result UpdatedResource() => NoContent();
    public static Result<T> ObtainedResource<T>(T value) => Result<T>.Ok(value);

    private static ErrorList CreateErrors(ErrorType type, string message, IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        List<Error> values = [];
        if (!string.IsNullOrWhiteSpace(message)) values.Add(new Error(type.ToString(), message, type));
        values.AddRange(errors.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(value => new Error(type.ToString(), value, type)));
        if (values.Count == 0) values.Add(new Error(type.ToString(), "An unspecified error occurred.", type));
        return new ErrorList(values);
    }

    public ResultOutcome GetOutcome() => this switch
    {
        TransactionProcessing.UnionResults.Ok _ => new(ResultKind.Ok),
        TransactionProcessing.UnionResults.NoContent _ => new(ResultKind.NoContent),
        Error error => new(ResultKind.Error, Error: error),
        ErrorList errors => new(ResultKind.ErrorList, Errors: errors),
        null => throw new InvalidOperationException("A default Result has no value."),
    };
}

#pragma warning disable S1481 // Sonar misreads the C# 15 union declaration body as an unused local.
public union Result<T>(Ok<T>, Created<T>, NoContent, Error, ErrorList) : IResultOutcome
{
    public bool IsSuccess => this switch
    {
        Ok<T> or Created<T> or TransactionProcessing.UnionResults.NoContent => true,
        Error or ErrorList => false,
        null => throw new InvalidOperationException("A default Result<T> has no value."),
    };
    public bool IsFailed => !IsSuccess;
    public T Data => this.GetValueOrThrow();
    public string Message => this.GetFailureMessage() ?? string.Empty;
    public IReadOnlyList<Error> Errors => this switch
    {
        Error error => [error], ErrorList errors => errors,
        Ok<T> or Created<T> or TransactionProcessing.UnionResults.NoContent => [],
        null => throw new InvalidOperationException("A default Result<T> has no value."),
    };
    public static Result<T> Ok(T value) => new(new Ok<T>(value is null ? throw new ArgumentNullException(nameof(value)) : value));
    public static Result<T> Created(T value, string? location = null) => new(new Created<T>(value is null ? throw new ArgumentNullException(nameof(value)) : value, location));
    public static Result<T> NoContent() => new(new NoContent());
    public static Result<T> Fail(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));
    public static Result<T> Fail(ErrorList errors) => new(errors ?? throw new ArgumentNullException(nameof(errors)));

    public static Result<T> FromFailure(IResultOutcome result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.GetOutcome() switch
        {
            { Kind: ResultKind.Error, Error: not null } outcome => Fail(outcome.Error),
            { Kind: ResultKind.ErrorList, Errors: not null } outcome => Fail(outcome.Errors),
            _ => throw new InvalidOperationException("Only a failed result can be propagated as a failure."),
        };
    }

    public ResultOutcome GetOutcome() => this switch
    {
        Ok<T> ok => new(ResultKind.Ok, Value: ok.Value),
        Created<T> created => new(ResultKind.Created, Value: created.Value, Location: created.Location),
        TransactionProcessing.UnionResults.NoContent _ => new(ResultKind.NoContent),
        Error error => new(ResultKind.Error, Error: error),
        ErrorList errors => new(ResultKind.ErrorList, Errors: errors),
        null => throw new InvalidOperationException("A default Result<T> has no value."),
    };
}
#pragma warning restore S1481
