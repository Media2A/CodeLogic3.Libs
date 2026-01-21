using CodeLogic.Configuration;
using System.ComponentModel.DataAnnotations;

namespace CL.SQLite.Models;

/// <summary>
/// Configuration settings for SQLite database connections.
/// This model is auto-generated as config/sqlite.json when missing.
/// </summary>
[ConfigSection("sqlite")]
public class SQLiteConfiguration : ConfigModelBase
{
    /// <summary>
    /// Gets or sets whether this SQLite connection is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the path to the SQLite database file
    /// </summary>
    [Required]
    public string DatabasePath { get; set; } = "database.db";

    /// <summary>
    /// Gets or sets the connection timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public uint ConnectionTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the command timeout in seconds
    /// </summary>
    [Range(1, 3600)]
    public uint CommandTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets whether to skip automatic table synchronization on startup
    /// </summary>
    public bool SkipTableSync { get; set; } = false;

    /// <summary>
    /// Gets or sets the cache mode for the database connection
    /// </summary>
    public CacheMode CacheMode { get; set; } = CacheMode.Default;

    /// <summary>
    /// Gets or sets whether to use Write-Ahead Logging (WAL) mode
    /// </summary>
    public bool UseWAL { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable foreign key constraints
    /// </summary>
    public bool EnableForeignKeys { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of connections in the pool
    /// </summary>
    [Range(1, 100)]
    public int MaxPoolSize { get; set; } = 10;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public override ConfigValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(DatabasePath))
            errors.Add("DatabasePath is required");

        if (MaxPoolSize < 1)
            errors.Add("MaxPoolSize must be at least 1");

        if (errors.Any())
            return ConfigValidationResult.Invalid(errors);

        return ConfigValidationResult.Valid();
    }
}

/// <summary>
/// SQLite cache modes
/// </summary>
public enum CacheMode
{
    /// <summary>
    /// Default cache mode
    /// </summary>
    Default,

    /// <summary>
    /// Private cache mode - each connection has its own cache
    /// </summary>
    Private,

    /// <summary>
    /// Shared cache mode - connections share a single cache
    /// </summary>
    Shared
}
