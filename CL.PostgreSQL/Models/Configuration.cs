using System.Text;
using System.ComponentModel.DataAnnotations;
using CodeLogic.Configuration;

namespace CL.PostgreSQL.Models;

/// <summary>
/// Wrapper configuration model for PostgreSQL multi-database support.
/// This model is registered with the centralized configuration system.
/// </summary>
[ConfigSection("postgresql")]
public class PostgreSQLConfiguration : ConfigModelBase
{
    /// <summary>
    /// Dictionary of database configurations keyed by connection ID.
    /// </summary>
    public Dictionary<string, DatabaseConfiguration> Databases { get; set; } = new();

    /// <summary>
    /// Validates the PostgreSQL configuration.
    /// </summary>
    public override ConfigValidationResult Validate()
    {
        var errors = new List<string>();

        if (Databases == null || Databases.Count == 0)
        {
            errors.Add("At least one database configuration is required");
            return ConfigValidationResult.Invalid(errors);
        }

        // Validate each database configuration
        foreach (var kvp in Databases)
        {
            var dbConfig = kvp.Value;
            var validationResult = dbConfig.Validate();

            if (!validationResult.IsValid)
            {
                errors.Add($"Database '{kvp.Key}': {string.Join(", ", validationResult.Errors)}");
            }
        }

        if (errors.Any())
            return ConfigValidationResult.Invalid(errors);

        return ConfigValidationResult.Valid();
    }
}

/// <summary>
/// Configuration settings for a PostgreSQL database connection.
/// </summary>
public class DatabaseConfiguration : ConfigModelBase
{
    /// <summary>
    /// Gets or sets the unique identifier for this connection configuration.
    /// </summary>
    public string ConnectionId { get; set; } = "Default";

    /// <summary>
    /// Gets or sets a value indicating whether this connection configuration is enabled.
    /// When false, this connection will not be initialized or used.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the database server hostname or IP address.
    /// </summary>
    [Required]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the database server port.
    /// Default is 5432 for PostgreSQL.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 5432;

    /// <summary>
    /// Gets or sets the database name/schema.
    /// </summary>
    [Required]
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username for database authentication.
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for database authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection timeout in seconds.
    /// Default is 30 seconds.
    /// </summary>
    [Range(1, 300)]
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// Default is 30 seconds.
    /// </summary>
    [Range(1, 3600)]
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Gets or sets the minimum pool size for connection pooling.
    /// Default is 5.
    /// </summary>
    [Range(0, 100)]
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum pool size for connection pooling.
    /// Default is 100.
    /// </summary>
    [Range(1, 1000)]
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum idle time for a pooled connection in seconds.
    /// Connections idle longer than this will be closed.
    /// Default is 60 seconds.
    /// </summary>
    [Range(0, 3600)]
    public int MaxIdleTime { get; set; } = 60;

    /// <summary>
    /// Gets or sets the SSL mode for the connection.
    /// </summary>
    public SslMode SslMode { get; set; } = SslMode.Prefer;

    /// <summary>
    /// Gets or sets a value indicating whether to enable automatic table synchronization.
    /// When true, tables will be automatically created/updated to match model definitions.
    /// Default is true.
    /// </summary>
    public bool EnableAutoSync { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable query result caching.
    /// Default is true.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the default cache time-to-live in seconds.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    public int DefaultCacheTtl { get; set; } = 300;

    /// <summary>
    /// Gets or sets the character set for the connection.
    /// Default is utf8.
    /// </summary>
    public string CharacterSet { get; set; } = "utf8";

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed logging.
    /// Default is false.
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to log slow queries.
    /// Default is true.
    /// </summary>
    public bool LogSlowQueries { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold in milliseconds for logging slow queries.
    /// Queries taking longer than this will be logged.
    /// Default is 1000 milliseconds (1 second).
    /// </summary>
    public int SlowQueryThreshold { get; set; } = 1000;

    /// <summary>
    /// Validates the database configuration.
    /// </summary>
    public override ConfigValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Host))
            errors.Add("Host is required");

        if (string.IsNullOrWhiteSpace(Database))
            errors.Add("Database name is required");

        if (string.IsNullOrWhiteSpace(Username))
            errors.Add("Username is required");

        if (Port < 1 || Port > 65535)
            errors.Add("Port must be between 1 and 65535");

        if (MinPoolSize > MaxPoolSize)
            errors.Add("MinPoolSize cannot be greater than MaxPoolSize");

        if (errors.Any())
            return ConfigValidationResult.Invalid(errors);

        return ConfigValidationResult.Valid();
    }

    /// <summary>
    /// Builds a PostgreSQL connection string from the configuration.
    /// </summary>
    public string BuildConnectionString()
    {
        var builder = new StringBuilder();
        builder.Append($"Host={Host};");
        builder.Append($"Port={Port};");
        builder.Append($"Database={Database};");
        builder.Append($"Username={Username};");
        builder.Append($"Password={Password};");
        builder.Append($"Connection Timeout={ConnectionTimeout};");
        builder.Append($"Command Timeout={CommandTimeout};");
        builder.Append($"Minimum Pool Size={MinPoolSize};");
        builder.Append($"Maximum Pool Size={MaxPoolSize};");
        builder.Append($"Idle In Transaction Session Timeout={MaxIdleTime};");
        builder.Append($"SSL Mode={SslMode};");
        builder.Append("Pooling=true;");

        return builder.ToString();
    }
}

/// <summary>
/// Enumerates the SSL modes for database connections.
/// </summary>
public enum SslMode
{
    /// <summary>
    /// No SSL connection.
    /// </summary>
    Disable,

    /// <summary>
    /// Allow SSL if available.
    /// </summary>
    Allow,

    /// <summary>
    /// Prefer SSL if available, otherwise use non-SSL.
    /// </summary>
    Prefer,

    /// <summary>
    /// Require SSL connection.
    /// </summary>
    Require,

    /// <summary>
    /// Require SSL with certificate verification.
    /// </summary>
    VerifyCA,

    /// <summary>
    /// Require SSL with full certificate verification.
    /// </summary>
    VerifyFull
}
