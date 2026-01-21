namespace CL.SQLite.Models;

/// <summary>
/// Represents the result of a database operation.
/// </summary>
/// <typeparam name="T">The type of data returned by the operation.</typeparam>
public class Result<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the data returned by the operation.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that occurred during the operation.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    public static Result<T> Success(T? data)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    public static Result<T> Failure(string errorMessage, Exception? exception = null)
    {
        return new Result<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}

/// <summary>
/// Represents the result of a database operation without data.
/// </summary>
public class Result
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the exception that occurred during the operation.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    public static Result Success()
    {
        return new Result
        {
            IsSuccess = true
        };
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    public static Result Failure(string errorMessage, Exception? exception = null)
    {
        return new Result
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception
        };
    }
}

/// <summary>
/// Represents a SQL query with parameters
/// </summary>
public class SQLiteQuery
{
    /// <summary>
    /// Gets or sets the SQL query string
    /// </summary>
    public required string QueryString { get; init; }

    /// <summary>
    /// Gets or sets the query parameters
    /// </summary>
    public Dictionary<string, object?> Parameters { get; init; } = new();
}

/// <summary>
/// Represents the result of a table synchronization operation
/// </summary>
public record TableSyncResult
{
    /// <summary>
    /// Gets whether the synchronization was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the message describing the result
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets any exception that occurred
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Creates a successful synchronization result.
    /// </summary>
    /// <param name="message">Result message.</param>
    public static TableSyncResult Succeeded(string message) =>
        new() { Success = true, Message = message };

    /// <summary>
    /// Creates a failed synchronization result.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="exception">Optional exception.</param>
    public static TableSyncResult Failed(string message, Exception? exception = null) =>
        new() { Success = false, Message = message, Exception = exception };
}

/// <summary>
/// Transaction isolation levels for SQLite
/// </summary>
public enum TransactionIsolation
{
    /// <summary>
    /// Deferred transaction - locks are acquired when needed
    /// </summary>
    Deferred,

    /// <summary>
    /// Immediate transaction - acquires a reserved lock immediately
    /// </summary>
    Immediate,

    /// <summary>
    /// Exclusive transaction - acquires an exclusive lock immediately
    /// </summary>
    Exclusive
}

/// <summary>
/// Represents a WHERE condition for LINQ query building
/// </summary>
public class WhereCondition
{
    /// <summary>
    /// Gets or sets the column name
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the comparison operator (=, !=, &gt;, &lt;, &gt;=, &lt;=, LIKE, IN, IS, IS NOT)
    /// </summary>
    public required string Operator { get; set; }

    /// <summary>
    /// Gets or sets the value to compare against
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the logical operator (AND, OR)
    /// </summary>
    public string LogicalOperator { get; set; } = "AND";
}

/// <summary>
/// Represents an ORDER BY clause
/// </summary>
public class OrderByClause
{
    /// <summary>
    /// Gets or sets the column name
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the sort order (Asc or Desc)
    /// </summary>
    public required SortOrder Order { get; set; }
}

/// <summary>
/// Sort order enumeration
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Ascending order
    /// </summary>
    Asc,

    /// <summary>
    /// Descending order
    /// </summary>
    Desc
}

/// <summary>
/// Represents an aggregate function (SUM, AVG, MIN, MAX, COUNT)
/// </summary>
public class AggregateFunction
{
    /// <summary>
    /// Gets or sets the aggregate type
    /// </summary>
    public required AggregateType Type { get; set; }

    /// <summary>
    /// Gets or sets the column name to aggregate
    /// </summary>
    public required string Column { get; set; }

    /// <summary>
    /// Gets or sets the alias for the result
    /// </summary>
    public required string Alias { get; set; }
}

/// <summary>
/// Aggregate function type enumeration
/// </summary>
public enum AggregateType
{
    /// <summary>
    /// SUM aggregate
    /// </summary>
    Sum,

    /// <summary>
    /// AVG aggregate
    /// </summary>
    Avg,

    /// <summary>
    /// MIN aggregate
    /// </summary>
    Min,

    /// <summary>
    /// MAX aggregate
    /// </summary>
    Max,

    /// <summary>
    /// COUNT aggregate
    /// </summary>
    Count
}

/// <summary>
/// Represents paginated results
/// </summary>
public class PagedResult<T> where T : class
{
    /// <summary>
    /// Gets or sets the items on this page
    /// </summary>
    public required List<T> Items { get; set; }

    /// <summary>
    /// Gets or sets the current page number
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items
    /// </summary>
    public long TotalItems { get; set; }

    /// <summary>
    /// Gets the total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}
