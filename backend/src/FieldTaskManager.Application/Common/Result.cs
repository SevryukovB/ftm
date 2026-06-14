namespace FieldTaskManager.Application.Common;

public readonly record struct Error(string Message, int StatusCode)
{
    public static readonly Error None = new(string.Empty, 0);

    public static Error BadRequest(string message) => new(message, 400);

    public static Error Unauthorized(string message) => new(message, 401);

    public static Error Forbidden(string message) => new(message, 403);

    public static Error NotFound(string message) => new(message, 404);

    public static Error Conflict(string message) => new(message, 409);
}

public readonly struct Result
{
    private readonly Error _error;

    private Result(bool isSuccess, Error error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error => IsFailure
        ? _error
        : throw new InvalidOperationException("Successful result does not contain an error.");

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error)
    {
        EnsureFailureError(error);
        return new Result(false, error);
    }

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);

    internal static void EnsureFailureError(Error error)
    {
        if (error.StatusCode == 0)
        {
            throw new ArgumentException("Failure result must contain an error.", nameof(error));
        }
    }
}

public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Error _error;

    private Result(T? value, Error error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Failed result does not contain a value.");

    public Error Error => IsFailure
        ? _error
        : throw new InvalidOperationException("Successful result does not contain an error.");

    public static Result<T> Success(T value) => new(value, Error.None, true);

    public static Result<T> Failure(Error error)
    {
        Result.EnsureFailureError(error);
        return new Result<T>(default, error, false);
    }
}
