namespace CL.Core.Models;

/// <summary>
/// Represents the result of an operation that may succeed or fail
/// </summary>
public record Result
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Exception if the operation threw
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new() { IsSuccess = true };

    /// <summary>
    /// Creates a failed result with an error message and optional exception.
    /// </summary>
    /// <param name="errorMessage">Error message describing the failure.</param>
    /// <param name="exception">Optional exception associated with the failure.</param>
    public static Result Failure(string errorMessage, Exception? exception = null) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, Exception = exception };
}

/// <summary>
/// Represents the result of an operation that may succeed or fail with a value
/// </summary>
public record Result<T>
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public required bool IsSuccess { get; init; }

    /// <summary>
    /// The value if the operation succeeded
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Exception if the operation threw
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful result containing a value.
    /// </summary>
    /// <param name="value">Value returned from the operation.</param>
    public static Result<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    /// <summary>
    /// Creates a failed result with an error message and optional exception.
    /// </summary>
    /// <param name="errorMessage">Error message describing the failure.</param>
    /// <param name="exception">Optional exception associated with the failure.</param>
    public static Result<T> Failure(string errorMessage, Exception? exception = null) =>
        new() { IsSuccess = false, ErrorMessage = errorMessage, Exception = exception };
}
