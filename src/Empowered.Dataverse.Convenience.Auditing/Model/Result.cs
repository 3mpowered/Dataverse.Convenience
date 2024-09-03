namespace Empowered.Dataverse.Convenience.Auditing.Model;

internal class Result<TResult>
{
    public static Result<TResult> Ok(TResult value) => new()
    {
        Value = value,
        IsSuccess = true,
        IsFailed = false,
        Error = null
    };

    public static Result<TResult> Fail(TResult value, string error) => new()
    {
        Value = value,
        IsSuccess = false,
        IsFailed = true,
        Error = error
    };

    public static Result<TResult> Fail(TResult value, Exception exception) => new(exception)
    {
        Value = value,
        IsSuccess = false,
        IsFailed = true,
        Error = exception.Message,
    };

    private Result()
    {

    }
    private Result(Exception exception)
    {
        _exception = exception;
    }

    private readonly Exception? _exception = null;
    public required TResult Value { get; init; }
    public required bool IsFailed { get; init; }
    public required bool IsSuccess { get; init; }
    public required string? Error { get; init; }
}
