using CodeLogic.Abstractions;
using CodeLogic.Logging;
using CL.SQLite.Models;
using Microsoft.Data.Sqlite;
using System.Reflection;
using System.Text;

namespace CL.SQLite.Services;

/// <summary>
/// Analyzes and compares SQLite database schemas with C# model definitions.
/// </summary>
public class SchemaAnalyzer
{
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a schema analyzer with optional logging.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public SchemaAnalyzer(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Represents a column definition from the model.
    /// </summary>
    public class ModelColumnDefinition
    {
        /// <summary>
        /// CLR property name backing the column.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Column name in the database.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// SQLite data type for the column.
        /// </summary>
        public SQLiteDataType DataType { get; set; }

        /// <summary>
        /// Indicates whether the column is a primary key.
        /// </summary>
        public bool Primary { get; set; }

        /// <summary>
        /// Indicates whether the column auto-increments.
        /// </summary>
        public bool AutoIncrement { get; set; }

        /// <summary>
        /// Indicates whether the column disallows null values.
        /// </summary>
        public bool NotNull { get; set; }

        /// <summary>
        /// Indicates whether the column has a unique constraint.
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Indicates whether the column is indexed.
        /// </summary>
        public bool IsIndexed { get; set; }

        /// <summary>
        /// Default value expression for the column.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Optional foreign key metadata.
        /// </summary>
        public SQLiteForeignKeyAttribute? ForeignKey { get; set; }

        /// <summary>
        /// Optional column comment.
        /// </summary>
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Represents a column definition from the database.
    /// </summary>
    public class DatabaseColumnDefinition
    {
        /// <summary>
        /// Column identifier (ordinal).
        /// </summary>
        public int Cid { get; set; }

        /// <summary>
        /// Column name in the database.
        /// </summary>
        public string ColumnName { get; set; } = string.Empty;

        /// <summary>
        /// Database-reported data type.
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the column disallows null values.
        /// </summary>
        public bool NotNull { get; set; }

        /// <summary>
        /// Default value expression in the database.
        /// </summary>
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Indicates whether the column is part of the primary key.
        /// </summary>
        public bool PrimaryKey { get; set; }
    }

    /// <summary>
    /// Generates column definitions from a model type based on SQLiteColumnAttribute.
    /// </summary>
    public List<ModelColumnDefinition> GenerateModelColumnDefinitions(Type modelType)
    {
        var columns = new List<ModelColumnDefinition>();

        var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var columnAttr = property.GetCustomAttribute<SQLiteColumnAttribute>();
            if (columnAttr == null)
                continue;

            var columnDef = new ModelColumnDefinition
            {
                PropertyName = property.Name,
                ColumnName = columnAttr.ColumnName ?? property.Name,
                DataType = columnAttr.DataType,
                Primary = columnAttr.IsPrimaryKey,
                AutoIncrement = columnAttr.IsAutoIncrement,
                NotNull = columnAttr.IsNotNull,
                Unique = columnAttr.IsUnique,
                IsIndexed = columnAttr.IsIndexed,
                DefaultValue = columnAttr.DefaultValue,
                ForeignKey = property.GetCustomAttribute<SQLiteForeignKeyAttribute>(),
                Comment = null
            };

            columns.Add(columnDef);
        }

        return columns;
    }

    /// <summary>
    /// Retrieves existing columns from a database table.
    /// </summary>
    public async Task<List<DatabaseColumnDefinition>> GetDatabaseColumnsAsync(
        SqliteConnection connection,
        string tableName)
    {
        var columns = new List<DatabaseColumnDefinition>();

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA table_info({tableName})";
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var columnDef = new DatabaseColumnDefinition
                {
                    Cid = reader.GetInt32(0),              // cid
                    ColumnName = reader.GetString(1),      // name
                    DataType = reader.GetString(2),        // type
                    NotNull = reader.GetInt32(3) == 1,     // notnull
                    DefaultValue = reader.IsDBNull(4) ? null : reader.GetString(4),  // dflt_value
                    PrimaryKey = reader.GetInt32(5) == 1   // pk
                };

                columns.Add(columnDef);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error retrieving columns for table '{tableName}': {ex.Message}", ex);
        }

        return columns;
    }

    /// <summary>
    /// Checks if a table exists in the database.
    /// </summary>
    public async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=@tableName";
            cmd.Parameters.AddWithValue("@tableName", tableName);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error checking if table '{tableName}' exists: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets existing indexes for a table.
    /// </summary>
    public async Task<List<(string IndexName, bool IsUnique, List<string> Columns)>> GetTableIndexesAsync(
        SqliteConnection connection,
        string tableName)
    {
        var indexes = new List<(string IndexName, bool IsUnique, List<string> Columns)>();

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"PRAGMA index_list({tableName})";
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var indexName = reader.GetString(1);  // name
                var isUnique = reader.GetInt32(2) == 1;  // unique

                // Get columns for this index
                using var colCmd = connection.CreateCommand();
                colCmd.CommandText = $"PRAGMA index_info({indexName})";
                using var colReader = await colCmd.ExecuteReaderAsync();

                var indexColumns = new List<string>();
                while (await colReader.ReadAsync())
                {
                    indexColumns.Add(colReader.GetString(2)); // name
                }

                if (indexColumns.Any())
                {
                    indexes.Add((indexName, isUnique, indexColumns));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error retrieving indexes for table '{tableName}': {ex.Message}", ex);
        }

        return indexes;
    }

    /// <summary>
    /// Generates SQL CREATE TABLE statement from model definition.
    /// </summary>
    public string GenerateCreateTableStatement(
        string tableName,
        List<ModelColumnDefinition> columns,
        SQLiteTableAttribute? tableAttr = null)
    {
        if (!columns.Any())
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {tableName} (");

        var columnDefs = new List<string>();
        var primaryKeys = columns.Where(c => c.Primary).ToList();
        var compositePrimaryKey = primaryKeys.Count > 1;

        foreach (var col in columns)
        {
            var colDef = GenerateColumnDefinition(col, allowInlinePrimaryKey: !compositePrimaryKey);
            columnDefs.Add($"  {colDef}");
        }

        if (compositePrimaryKey)
        {
            var pkColumns = string.Join(", ", primaryKeys.Select(c => c.ColumnName));
            columnDefs.Add($"  PRIMARY KEY ({pkColumns})");
        }

        foreach (var col in columns.Where(c => c.ForeignKey != null))
        {
            var fk = col.ForeignKey!;
            var fkClause = $"  FOREIGN KEY ({col.ColumnName}) REFERENCES {fk.ReferencedTable}({fk.ReferencedColumn})";
            fkClause += $" ON DELETE {ConvertForeignKeyAction(fk.OnDelete)}";
            fkClause += $" ON UPDATE {ConvertForeignKeyAction(fk.OnUpdate)}";
            columnDefs.Add(fkClause);
        }

        sb.Append(string.Join(",\n", columnDefs));
        sb.AppendLine();
        sb.Append(");");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a single column definition string.
    /// </summary>
    private string GenerateColumnDefinition(ModelColumnDefinition col, bool allowInlinePrimaryKey)
    {
        var sb = new StringBuilder();
        sb.Append($"{col.ColumnName} ");
        sb.Append(ConvertDataTypeToSQLite(col.DataType));

        if (allowInlinePrimaryKey && col.Primary && col.AutoIncrement)
        {
            sb.Append(" PRIMARY KEY AUTOINCREMENT");
        }
        else if (allowInlinePrimaryKey && col.Primary)
        {
            sb.Append(" PRIMARY KEY");
        }
        else if (col.Unique)
        {
            sb.Append(" UNIQUE");
        }

        if (col.NotNull)
        {
            sb.Append(" NOT NULL");
        }

        if (!string.IsNullOrEmpty(col.DefaultValue))
        {
            if (col.DefaultValue.Equals("CURRENT_TIMESTAMP", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(" DEFAULT CURRENT_TIMESTAMP");
            }
            else
            {
                sb.Append($" DEFAULT {col.DefaultValue}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates SQL CREATE INDEX statements from model definition.
    /// </summary>
    public List<string> GenerateCreateIndexStatements(Type modelType, string tableName, List<ModelColumnDefinition> columns)
    {
        var statements = new List<string>();

        foreach (var col in columns.Where(c => c.IsIndexed))
        {
            var indexName = $"idx_{tableName}_{col.ColumnName}";
            var statement = $"CREATE INDEX IF NOT EXISTS {indexName} ON {tableName} ({col.ColumnName});";
            statements.Add(statement);
        }

        var indexAttributes = modelType.GetCustomAttributes<SQLiteIndexAttribute>();
        foreach (var indexAttr in indexAttributes)
        {
            if (indexAttr.Columns.Length == 0)
                continue;

            var name = string.IsNullOrWhiteSpace(indexAttr.Name)
                ? $"idx_{tableName}_{string.Join("_", indexAttr.Columns)}"
                : indexAttr.Name;

            var unique = indexAttr.IsUnique ? "UNIQUE " : string.Empty;
            var cols = string.Join(", ", indexAttr.Columns);
            statements.Add($"CREATE {unique}INDEX IF NOT EXISTS {name} ON {tableName} ({cols});");
        }

        return statements;
    }

    /// <summary>
    /// Converts C# SQLiteDataType enum to SQLite data type string.
    /// </summary>
    private string ConvertDataTypeToSQLite(SQLiteDataType dataType)
    {
        return dataType switch
        {
            SQLiteDataType.INTEGER => "INTEGER",
            SQLiteDataType.REAL => "REAL",
            SQLiteDataType.TEXT => "TEXT",
            SQLiteDataType.BLOB => "BLOB",
            SQLiteDataType.NUMERIC => "NUMERIC",
            SQLiteDataType.BOOLEAN => "BOOLEAN",
            SQLiteDataType.DATE => "DATE",
            SQLiteDataType.DATETIME => "DATETIME",
            _ => "TEXT"
        };
    }

    private static string ConvertForeignKeyAction(ForeignKeyAction action)
    {
        return action switch
        {
            ForeignKeyAction.NoAction => "NO ACTION",
            ForeignKeyAction.Restrict => "RESTRICT",
            ForeignKeyAction.SetNull => "SET NULL",
            ForeignKeyAction.SetDefault => "SET DEFAULT",
            ForeignKeyAction.Cascade => "CASCADE",
            _ => "NO ACTION"
        };
    }

    /// <summary>
    /// Detects differences between model definition and database schema.
    /// </summary>
    public SchemaDifferences DetectDifferences(
        List<ModelColumnDefinition> modelColumns,
        List<DatabaseColumnDefinition> dbColumns)
    {
        var differences = new SchemaDifferences();

        var dbColumnMap = dbColumns.ToDictionary(c => c.ColumnName.ToLower());
        var modelColumnMap = modelColumns.ToDictionary(c => c.ColumnName.ToLower());

        // Find missing columns in database
        foreach (var modelCol in modelColumns)
        {
            if (!dbColumnMap.ContainsKey(modelCol.ColumnName.ToLower()))
            {
                differences.MissingColumns.Add(modelCol);
            }
        }

        // Find extra columns in database
        foreach (var dbCol in dbColumns)
        {
            if (!modelColumnMap.ContainsKey(dbCol.ColumnName.ToLower()))
            {
                differences.ExtraColumns.Add(dbCol);
            }
        }

        // Find modified columns
        foreach (var modelCol in modelColumns)
        {
            var key = modelCol.ColumnName.ToLower();
            if (dbColumnMap.TryGetValue(key, out var dbCol))
            {
                if (HasColumnChanged(modelCol, dbCol))
                {
                    differences.ModifiedColumns.Add((modelCol, dbCol));
                }
            }
        }

        return differences;
    }

    /// <summary>
    /// Determines if a column definition has changed.
    /// </summary>
    private bool HasColumnChanged(ModelColumnDefinition modelCol, DatabaseColumnDefinition dbCol)
    {
        var expectedType = ConvertDataTypeToSQLite(modelCol.DataType);
        if (dbCol.DataType != expectedType)
            return true;

        if (modelCol.NotNull != dbCol.NotNull)
            return true;

        if (modelCol.Primary != dbCol.PrimaryKey)
            return true;

        return false;
    }

    /// <summary>
    /// Schema differences result.
    /// </summary>
    public class SchemaDifferences
    {
        /// <summary>
        /// Columns present in the model but missing in the database.
        /// </summary>
        public List<ModelColumnDefinition> MissingColumns { get; } = new();

        /// <summary>
        /// Columns present in the database but missing in the model.
        /// </summary>
        public List<DatabaseColumnDefinition> ExtraColumns { get; } = new();

        /// <summary>
        /// Columns that exist in both but differ in definition.
        /// </summary>
        public List<(ModelColumnDefinition Model, DatabaseColumnDefinition Database)> ModifiedColumns { get; } = new();

        /// <summary>
        /// Indicates whether any differences were found.
        /// </summary>
        public bool HasDifferences =>
            MissingColumns.Any() ||
            ExtraColumns.Any() ||
            ModifiedColumns.Any();
    }
}
