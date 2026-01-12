namespace IconProject.Common.Dtos;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
/// <typeparam name="T">The type of the value on success.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result. Check IsSuccess first.");

    public Error Error => !IsSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful result. Check IsFailure first.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    public async Task<TResult> MatchAsync<TResult>(
        Func<T, Task<TResult>> onSuccess,
        Func<Error, Task<TResult>> onFailure)
        => IsSuccess ? await onSuccess(_value!) : await onFailure(_error!);
}

/// <summary>
/// Represents the result of an operation that can either succeed (with no value) or fail.
/// </summary>
public sealed class Result
{
    private readonly Error? _error;

    private Result()
    {
        IsSuccess = true;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Error Error => !IsSuccess
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful result. Check IsFailure first.");

    public static Result Success() => new();
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess() : onFailure(_error!);
}

/// <summary>
/// Represents an error with a code and description.
/// </summary>
public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string entityName, int id) =>
        new($"{entityName}.NotFound", $"{entityName} with ID {id} was not found.");

    public static Error NotFound(string entityName, string identifier) =>
        new($"{entityName}.NotFound", $"{entityName} '{identifier}' was not found.");

    public static Error Validation(string description) =>
        new("Validation.Error", description);

    public static Error Conflict(string description) =>
        new("Conflict.Error", description);

    public static Error Unauthorized(string description) =>
        new("Unauthorized.Error", description);
}

/// <summary>
/// Domain-specific errors for the application.
/// </summary>
public static class DomainErrors
{
    public static class User
    {
        public static readonly Error EmailAlreadyExists =
            new("User.EmailAlreadyExists", "A user with this email already exists.");

        public static readonly Error InvalidCredentials =
            new("User.InvalidCredentials", "Invalid email or password.");

        public static Error NotFound(string email) =>
            Error.NotFound("User", email);
    }

    public static class Task
    {
        public static Error NotFound(int id) =>
            Error.NotFound("Task", id);

        public static readonly Error NotOwned =
            new("Task.NotOwned", "You do not have permission to access this task.");
    }
}
