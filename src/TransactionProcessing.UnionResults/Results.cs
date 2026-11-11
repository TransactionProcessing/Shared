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
    public static Result Ok() => new(new Ok());
    public static Result NoContent() => new(new NoContent());
    public static Result Fail(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));
    public static Result Fail(ErrorList errors) => new(errors ?? throw new ArgumentNullException(nameof(errors)));

    public ResultOutcome GetOutcome() => this switch
    {
        TransactionProcessing.UnionResults.Ok _ => new(ResultKind.Ok),
        TransactionProcessing.UnionResults.NoContent _ => new(ResultKind.NoContent),
        Error error => new(ResultKind.Error, Error: error),
        ErrorList errors => new(ResultKind.ErrorList, Errors: errors),
        null => throw new InvalidOperationException("A default Result has no value."),
    };
}

public union Result<T>(Ok<T>, Created<T>, NoContent, Error, ErrorList) : IResultOutcome
{
    public static Result<T> Ok(T value) => new(new Ok<T>(value is null ? throw new ArgumentNullException(nameof(value)) : value));
    public static Result<T> Created(T value, string? location = null) => new(new Created<T>(value is null ? throw new ArgumentNullException(nameof(value)) : value, location));
    public static Result<T> NoContent() => new(new NoContent());
    public static Result<T> Fail(Error error) => new(error ?? throw new ArgumentNullException(nameof(error)));
    public static Result<T> Fail(ErrorList errors) => new(errors ?? throw new ArgumentNullException(nameof(errors)));

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
