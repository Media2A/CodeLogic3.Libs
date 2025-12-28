using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.Sqlite;
using CL.SQLite.Models;
using CodeLogic.Abstractions;
using CodeLogic.Logging;

namespace CL.SQLite.Services;

/// <summary>
/// Generic repository for SQLite database operations
/// </summary>
/// <typeparam name="T">The model type that represents a database table</typeparam>
public class Repository<T> where T : class, new()
{
    private readonly ConnectionManager _connectionManager;
    private readonly ILogger _logger;
    private readonly string _tableName;
    private readonly PropertyInfo[] _properties;

    public Repository(ConnectionManager connectionManager, ILogger? logger = null)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger ?? new NullLogger();

        // Get table name from attribute or use class name
        var tableAttr = typeof(T).GetCustomAttribute<SQLiteTableAttribute>();
        _tableName = tableAttr?.TableName ?? typeof(T).Name;
        _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    /// <summary>
    /// Inserts a new record into the database
    /// </summary>
    public async Task<Result<long>> InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var columns = new List<string>();
            var parameters = new List<string>();
            var values = new Dictionary<string, object?>();

            foreach (var prop in _properties)
            {
                var colAttr = prop.GetCustomAttribute<SQLiteColumnAttribute>();
                if (colAttr?.IsAutoIncrement == true)
                    continue;

                // Also skip the property named "Id" without attributes as a safety measure (likely auto-increment)
                if (prop.Name == "Id" && colAttr?.ColumnName == null && colAttr?.IsPrimaryKey != true)
                    continue;

                var columnName = colAttr?.ColumnName ?? prop.Name;
                columns.Add(columnName);
                parameters.Add($"@{columnName}");
                values[columnName] = prop.GetValue(entity);
            }

            var sql = $"INSERT INTO {_tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)}); SELECT last_insert_rowid();";

            return await _connectionManager.ExecuteAsync(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;

                foreach (var kvp in values)
                {
                    cmd.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }

                var id = (long)(await cmd.ExecuteScalarAsync(cancellationToken) ?? 0L);
                _logger.Debug($"Inserted record into {_tableName} with ID {id}");
                return Result<long>.Success(id);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to insert into {_tableName}", ex);
            return Result<long>.Failure(ex.Message, ex);
        }
    }

    /// <summary>
    /// Inserts or replaces a record based on primary key.
    /// </summary>
    public async Task<Result> UpsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var columns = new List<string>();
            var parameters = new List<string>();
            var values = new Dictionary<string, object?>();

            foreach (var prop in _properties)
            {
                var colAttr = prop.GetCustomAttribute<SQLiteColumnAttribute>();
                if (colAttr?.IsAutoIncrement == true)
                    continue;

                if (prop.Name == "Id" && colAttr?.ColumnName == null && colAttr?.IsPrimaryKey != true)
                    continue;

                var columnName = colAttr?.ColumnName ?? prop.Name;
                columns.Add(columnName);
                parameters.Add($"@{columnName}");
                values[columnName] = prop.GetValue(entity);
            }

            var sql = $"INSERT OR REPLACE INTO {_tableName} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", parameters)});";

            return await _connectionManager.ExecuteAsync(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;

                foreach (var kvp in values)
                {
                    cmd.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }

                await cmd.ExecuteNonQueryAsync(cancellationToken);
                _logger.Debug($"Upserted record into {_tableName}");
                return Result.Success();
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to upsert into {_tableName}", ex);
            return Result.Failure(ex.Message, ex);
        }
    }

    /// <summary>
    /// Gets a record by ID
    /// </summary>
    public async Task<Result<T?>> GetByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        try
        {
            var primaryKey = GetPrimaryKeyColumn();
            var sql = $"SELECT * FROM {_tableName} WHERE {primaryKey} = @id LIMIT 1";

            return await _connectionManager.ExecuteAsync(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    var entity = MapFromReader(reader);
                    return Result<T?>.Success(entity);
                }

                return Result<T?>.Success(null);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get record from {_tableName}", ex);
            return Result<T?>.Failure(ex.Message, ex);
        }
    }

    /// <summary>
    /// Gets all records from the table
    /// </summary>
    public async Task<Result<List<T>>> GetAllAsync(int limit = 1000, CancellationToken cancellationToken = default)
    {
        try
        {
            var sql = $"SELECT * FROM {_tableName} LIMIT {limit}";

            return await _connectionManager.ExecuteAsync(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;

                var results = new List<T>();
                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(MapFromReader(reader));
                }

                _logger.Debug($"Retrieved {results.Count} records from {_tableName}");
                return Result<List<T>>.Success(results);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to get all records from {_tableName}", ex);
            return Result<List<T>>.Failure(ex.Message, ex);
        }
    }

    /// <summary>
    /// Updates a record in the database
    /// </summary>
    public async Task<Result> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            var primaryKey = GetPrimaryKeyColumn();
            var setParts = new List<string>();
            var values = new Dictionary<string, object?>();
            object? pkValue = null;

            foreach (var prop in _properties)
            {
                var colAttr = prop.GetCustomAttribute<SQLiteColumnAttribute>();
                var columnName = colAttr?.ColumnName ?? prop.Name;
                var value = prop.GetValue(entity);

                // Check if this is the primary key column
                if (colAttr?.IsPrimaryKey == true || prop.Name == "Id")
                {
                    pkValue = value;
                }
                else if (colAttr?.IsAutoIncrement != true)
                {
                    setParts.Add($"{columnName} = @{columnName}");
                    values[columnName] = value;
                }
            }

            if (pkValue == null)
                return Result.Failure("Primary key value is null");

            var sql = $"UPDATE {_tableName} SET {string.Join(", ", setParts)} WHERE {primaryKey} = @pk";

            return await _connectionManager.ExecuteAsync(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@pk", pkValue);

                foreach (var kvp in values)
                {
                    cmd.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value ?? DBNull.Value);
                }

                var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
                _logger.Debug($"Updated {affected} record(s) in {_tableName}");
                return Result.Success();
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to update record in {_tableName}", ex);
            return Result.Failure(ex.Message, ex);
        }
    }

    /// <summary>
    /// Deletes a record by ID
    /// </summary>
    public async Task<Result> DeleteAsync(object id, CancellationToken cancellationToken = default)
    {
        try
        {
            var primaryKey = GetPrimaryKeyColumn();
            var sql = $"DELETE FROM {_tableName} WHERE {primaryKey} = @id";

            return await _connectionManager.ExecuteAsync(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@id", id);

                var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
                _logger.Debug($"Deleted {affected} record(s) from {_tableName}");
                return Result.Success();
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete record from {_tableName}", ex);
            return Result.Failure(ex.Message, ex);
        }
    }

    /// <summary>
    /// Deletes records matching a predicate.
    /// </summary>
    public async Task<Result<int>> DeleteWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        try
        {
            var conditions = ExpressionVisitor.Parse(predicate);
            if (conditions.Count == 0)
                return Result<int>.Failure("DeleteWhereAsync requires at least one condition");

            var where = WhereClauseBuilder.Build(conditions);
            var sql = $"DELETE FROM {_tableName} WHERE {where.Clause}";

            return await _connectionManager.ExecuteAsync(async conn =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                foreach (var (key, value) in where.Parameters)
                {
                    cmd.Parameters.AddWithValue(key, value ?? DBNull.Value);
                }

                var affected = await cmd.ExecuteNonQueryAsync(cancellationToken);
                _logger.Debug($"Deleted {affected} record(s) from {_tableName}");
                return Result<int>.Success(affected);
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to delete records from {_tableName}", ex);
            return Result<int>.Failure(ex.Message, ex);
        }
    }

    private T MapFromReader(SqliteDataReader reader)
    {
        var entity = new T();

        foreach (var prop in _properties)
        {
            var colAttr = prop.GetCustomAttribute<SQLiteColumnAttribute>();
            var columnName = colAttr?.ColumnName ?? prop.Name;

            try
            {
                var ordinal = reader.GetOrdinal(columnName);
                if (!reader.IsDBNull(ordinal))
                {
                    var value = reader.GetValue(ordinal);
                    prop.SetValue(entity, Convert.ChangeType(value, prop.PropertyType));
                }
            }
            catch
            {
                // Column doesn't exist or conversion failed
            }
        }

        return entity;
    }

    private string GetPrimaryKeyColumn()
    {
        var primaryKeys = new List<string>();
        foreach (var prop in _properties)
        {
            var colAttr = prop.GetCustomAttribute<SQLiteColumnAttribute>();
            if (colAttr?.IsPrimaryKey == true)
            {
                primaryKeys.Add(colAttr.ColumnName ?? prop.Name);
            }
        }

        if (primaryKeys.Count > 1)
            throw new InvalidOperationException($"Composite primary keys are not supported by Repository<{typeof(T).Name}>.");

        if (primaryKeys.Count == 1)
            return primaryKeys[0];

        return "Id"; // Default fallback
    }
}
