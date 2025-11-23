using CodeLogic.Configuration;
using System.ComponentModel.DataAnnotations;

namespace CL.MySQL2.Configuration;

/// <summary>
/// Configuration model for MySQL database connections and behavior.
/// This model is auto-generated as config/mysql.json when missing.
/// </summary>
[ConfigSection("mysql")]
public class DatabaseConfiguration : ConfigModelBase
{
    // === Connection Settings ===

    /// <summary>
    /// Unique identifier for this database configuration.
    /// Allows multiple database connections to be managed.
    /// </summary>
    public string ConnectionId { get; set; } = "Default";

    /// <summary>
    /// Whether this database connection is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Database server host address.
    /// </summary>
    [Required]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Database server port.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 3306;

    /// <summary>
    /// Name of the database to connect to.
    /// </summary>
    [Required]
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// Database username.
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Database password.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    // === Connection Pooling ===

    /// <summary>
    /// Whether to enable connection pooling.
    /// </summary>
    public bool EnablePooling { get; set; } = true;

    /// <summary>
    /// Minimum number of connections to keep in the pool.
    /// </summary>
    [Range(0, 100)]
    public int MinPoolSize { get; set; } = 5;

    /// <summary>
    /// Maximum number of connections allowed in the pool.
    /// </summary>
    [Range(1, 1000)]
    public int MaxPoolSize { get; set; } = 100;

    /// <summary>
    /// Connection lifetime in seconds. 0 = no limit.
    /// </summary>
    [Range(0, 3600)]
    public int ConnectionLifetime { get; set; } = 300;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    [Range(1, 300)]
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Command timeout in seconds.
    /// </summary>
    [Range(1, 3600)]
    public int CommandTimeout { get; set; } = 60;

    // === Performance & Caching ===

    /// <summary>
    /// Whether to enable query result caching.
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Default cache TTL in seconds.
    /// </summary>
    [Range(0, 86400)]
    public int CacheTtl { get; set; } = 300;

    /// <summary>
    /// Maximum number of cached query results. 0 = unlimited.
    /// </summary>
    [Range(0, 100000)]
    public int MaxCacheItems { get; set; } = 10000;

    /// <summary>
    /// Whether to enable prepared statements.
    /// </summary>
    public bool EnablePreparedStatements { get; set; } = true;

    /// <summary>
    /// Whether to compress network traffic.
    /// </summary>
    public bool EnableCompression { get; set; } = false;

    // === Security ===

    /// <summary>
    /// Whether to use SSL/TLS for connections.
    /// </summary>
    public bool EnableSsl { get; set; } = false;

    /// <summary>
    /// SSL certificate file path (optional).
    /// </summary>
    public string? SslCertificatePath { get; set; }

    /// <summary>
    /// Whether to allow public key retrieval (for caching_sha2_password).
    /// </summary>
    public bool AllowPublicKeyRetrieval { get; set; } = false;

    // === Character Set & Collation ===

    /// <summary>
    /// Character set to use for the connection.
    /// </summary>
    public string CharacterSet { get; set; } = "utf8mb4";

    /// <summary>
    /// Collation to use for comparisons.
    /// </summary>
    public string Collation { get; set; } = "utf8mb4_unicode_ci";

    // === Logging & Debugging ===

    /// <summary>
    /// Whether to enable query logging.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Whether to log slow queries.
    /// </summary>
    public bool LogSlowQueries { get; set; } = true;

    /// <summary>
    /// Threshold in milliseconds to consider a query slow.
    /// </summary>
    [Range(0, 60000)]
    public int SlowQueryThreshold { get; set; } = 1000;

    // === Table Sync Settings ===

    /// <summary>
    /// Whether to enable automatic table synchronization.
    /// When enabled, tables will be created/updated to match model definitions.
    /// </summary>
    public bool EnableTableSync { get; set; } = true;

    /// <summary>
    /// Whether to allow destructive sync operations (dropping columns).
    /// </summary>
    public bool AllowDestructiveSync { get; set; } = false;

    /// <summary>
    /// Whether to create a backup before destructive sync operations.
    /// </summary>
    public bool BackupBeforeSync { get; set; } = true;

    // === Backup Settings ===

    /// <summary>
    /// Whether to enable automatic backups.
    /// </summary>
    public bool EnableAutoBackup { get; set; } = false;

    /// <summary>
    /// Backup directory path.
    /// </summary>
    public string BackupDirectory { get; set; } = "backups";

    /// <summary>
    /// Backup retention days. 0 = keep forever.
    /// </summary>
    [Range(0, 365)]
    public int BackupRetentionDays { get; set; } = 30;

    // === Migration Settings ===

    /// <summary>
    /// Whether to enable migration tracking.
    /// </summary>
    public bool EnableMigrations { get; set; } = true;

    /// <summary>
    /// Migrations directory path.
    /// </summary>
    public string MigrationsDirectory { get; set; } = "migrations";

    /// <summary>
    /// Whether to automatically run pending migrations on startup.
    /// </summary>
    public bool AutoRunMigrations { get; set; } = false;

    // === Health Check Settings ===

    /// <summary>
    /// Whether to enable health checks.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check interval in seconds.
    /// </summary>
    [Range(5, 3600)]
    public int HealthCheckInterval { get; set; } = 60;

    /// <summary>
    /// Builds a MySQL connection string from the configuration.
    /// </summary>
    public string BuildConnectionString()
    {
        var builder = new System.Text.StringBuilder();

        builder.Append($"Server={Host};");
        builder.Append($"Port={Port};");
        builder.Append($"Database={Database};");
        builder.Append($"User ID={Username};");
        builder.Append($"Password={Password};");

        // Pooling
        builder.Append($"Pooling={EnablePooling};");
        if (EnablePooling)
        {
            builder.Append($"Min Pool Size={MinPoolSize};");
            builder.Append($"Max Pool Size={MaxPoolSize};");
            builder.Append($"Connection Lifetime={ConnectionLifetime};");
        }

        // Timeouts
        builder.Append($"Connection Timeout={ConnectionTimeout};");
        builder.Append($"Default Command Timeout={CommandTimeout};");

        // Character set
        builder.Append($"CharSet={CharacterSet};");

        // SSL
        if (EnableSsl)
        {
            builder.Append("SslMode=Required;");
            if (!string.IsNullOrEmpty(SslCertificatePath))
            {
                builder.Append($"SslCa={SslCertificatePath};");
            }
        }
        else
        {
            builder.Append("SslMode=None;");
        }

        // Security
        builder.Append($"AllowPublicKeyRetrieval={AllowPublicKeyRetrieval};");

        // Performance
        if (EnableCompression)
        {
            builder.Append("UseCompression=True;");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Validates the configuration.
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
}
