using CodeLogic.Localization;

namespace CL.MySQL2.Localization;

/// <summary>
/// Localization strings for MySQL2 library.
/// Auto-generated as localization/{culture}_mysql.json when missing.
/// </summary>
[LocalizationSection("mysql")]
public class MySQL2Strings : LocalizationModelBase
{
    // === Connection Messages ===

    /// <summary>
    /// Message when a database connection is established.
    /// </summary>
    [LocalizedString(Description = "Connection established message")]
    public string ConnectionEstablished { get; set; } = "Database connection established";

    /// <summary>
    /// Message when a database connection fails.
    /// </summary>
    [LocalizedString(Description = "Connection failed message")]
    public string ConnectionFailed { get; set; } = "Failed to connect to database";

    /// <summary>
    /// Message when a database connection is closed.
    /// </summary>
    [LocalizedString(Description = "Connection closed message")]
    public string ConnectionClosed { get; set; } = "Database connection closed";

    /// <summary>
    /// Message when a connection test succeeds.
    /// </summary>
    [LocalizedString(Description = "Connection test successful")]
    public string ConnectionTestSuccess { get; set; } = "Connection test successful";

    /// <summary>
    /// Message when a connection test fails.
    /// </summary>
    [LocalizedString(Description = "Connection test failed")]
    public string ConnectionTestFailed { get; set; } = "Connection test failed";

    // === Library Lifecycle ===

    /// <summary>
    /// Message when the library finishes initializing.
    /// </summary>
    [LocalizedString(Description = "Library initialized")]
    public string LibraryInitialized { get; set; } = "MySQL2 library initialized";

    /// <summary>
    /// Message when the library starts.
    /// </summary>
    [LocalizedString(Description = "Library started")]
    public string LibraryStarted { get; set; } = "MySQL2 library started";

    /// <summary>
    /// Message when the library stops.
    /// </summary>
    [LocalizedString(Description = "Library stopped")]
    public string LibraryStopped { get; set; } = "MySQL2 library stopped";

    /// <summary>
    /// Message when the library is disposed.
    /// </summary>
    [LocalizedString(Description = "Library disposed")]
    public string LibraryDisposed { get; set; } = "MySQL2 library disposed";

    // === Table Sync Messages ===

    /// <summary>
    /// Message when table synchronization starts.
    /// </summary>
    [LocalizedString(Description = "Table sync started")]
    public string TableSyncStarted { get; set; } = "Table synchronization started for {0}";

    /// <summary>
    /// Message when table synchronization completes.
    /// </summary>
    [LocalizedString(Description = "Table sync completed")]
    public string TableSyncCompleted { get; set; } = "Table synchronization completed for {0}";

    /// <summary>
    /// Message when a table is created.
    /// </summary>
    [LocalizedString(Description = "Table created")]
    public string TableCreated { get; set; } = "Table {0} created successfully";

    /// <summary>
    /// Message when a table is updated.
    /// </summary>
    [LocalizedString(Description = "Table updated")]
    public string TableUpdated { get; set; } = "Table {0} updated successfully";

    /// <summary>
    /// Message when table synchronization fails.
    /// </summary>
    [LocalizedString(Description = "Table sync failed")]
    public string TableSyncFailed { get; set; } = "Table synchronization failed for {0}";

    // === Migration Messages ===

    /// <summary>
    /// Message when migrations start.
    /// </summary>
    [LocalizedString(Description = "Migrations started")]
    public string MigrationsStarted { get; set; } = "Running pending migrations";

    /// <summary>
    /// Message when migrations complete successfully.
    /// </summary>
    [LocalizedString(Description = "Migrations completed")]
    public string MigrationsCompleted { get; set; } = "Migrations completed successfully";

    /// <summary>
    /// Message when a migration is applied.
    /// </summary>
    [LocalizedString(Description = "Migration applied")]
    public string MigrationApplied { get; set; } = "Migration {0} applied successfully";

    /// <summary>
    /// Message when migrations fail.
    /// </summary>
    [LocalizedString(Description = "Migrations failed")]
    public string MigrationsFailed { get; set; } = "Migrations failed";

    // === Backup Messages ===

    /// <summary>
    /// Message when a backup starts.
    /// </summary>
    [LocalizedString(Description = "Backup started")]
    public string BackupStarted { get; set; } = "Database backup started";

    /// <summary>
    /// Message when a backup completes.
    /// </summary>
    [LocalizedString(Description = "Backup completed")]
    public string BackupCompleted { get; set; } = "Database backup completed: {0}";

    /// <summary>
    /// Message when a backup fails.
    /// </summary>
    [LocalizedString(Description = "Backup failed")]
    public string BackupFailed { get; set; } = "Database backup failed";

    // === Query Messages ===

    /// <summary>
    /// Message when a query executes successfully.
    /// </summary>
    [LocalizedString(Description = "Query executed")]
    public string QueryExecuted { get; set; } = "Query executed successfully";

    /// <summary>
    /// Message when a slow query is detected.
    /// </summary>
    [LocalizedString(Description = "Slow query detected")]
    public string SlowQueryDetected { get; set; } = "Slow query detected ({0}ms): {1}";

    /// <summary>
    /// Message when a query fails to execute.
    /// </summary>
    [LocalizedString(Description = "Query failed")]
    public string QueryFailed { get; set; } = "Query execution failed";

    // === Repository Messages ===

    /// <summary>
    /// Message when a record is inserted.
    /// </summary>
    [LocalizedString(Description = "Record inserted")]
    public string RecordInserted { get; set; } = "Record inserted into {0}";

    /// <summary>
    /// Message when multiple records are inserted.
    /// </summary>
    [LocalizedString(Description = "Records bulk inserted")]
    public string RecordsBulkInserted { get; set; } = "{0} records inserted into {1}";

    /// <summary>
    /// Message when a record is updated.
    /// </summary>
    [LocalizedString(Description = "Record updated")]
    public string RecordUpdated { get; set; } = "Record updated in {0}";

    /// <summary>
    /// Message when a record is deleted.
    /// </summary>
    [LocalizedString(Description = "Record deleted")]
    public string RecordDeleted { get; set; } = "Record deleted from {0}";

    /// <summary>
    /// Message when a record is not found.
    /// </summary>
    [LocalizedString(Description = "Record not found")]
    public string RecordNotFound { get; set; } = "Record not found in {0}";

    // === Transaction Messages ===

    /// <summary>
    /// Message when a transaction starts.
    /// </summary>
    [LocalizedString(Description = "Transaction started")]
    public string TransactionStarted { get; set; } = "Transaction started";

    /// <summary>
    /// Message when a transaction is committed.
    /// </summary>
    [LocalizedString(Description = "Transaction committed")]
    public string TransactionCommitted { get; set; } = "Transaction committed successfully";

    /// <summary>
    /// Message when a transaction is rolled back.
    /// </summary>
    [LocalizedString(Description = "Transaction rolled back")]
    public string TransactionRolledBack { get; set; } = "Transaction rolled back";

    /// <summary>
    /// Message when a transaction fails.
    /// </summary>
    [LocalizedString(Description = "Transaction failed")]
    public string TransactionFailed { get; set; } = "Transaction failed";

    // === Health Check Messages ===

    /// <summary>
    /// Message when a health check passes.
    /// </summary>
    [LocalizedString(Description = "Health check passed")]
    public string HealthCheckPassed { get; set; } = "Health check passed";

    /// <summary>
    /// Message when a health check fails.
    /// </summary>
    [LocalizedString(Description = "Health check failed")]
    public string HealthCheckFailed { get; set; } = "Health check failed: {0}";

    // === Error Messages ===

    /// <summary>
    /// Message when configuration validation fails.
    /// </summary>
    [LocalizedString(Description = "Configuration error")]
    public string ConfigurationError { get; set; } = "Configuration error: {0}";

    /// <summary>
    /// Message when a database error occurs.
    /// </summary>
    [LocalizedString(Description = "Database error")]
    public string DatabaseError { get; set; } = "Database error: {0}";

    /// <summary>
    /// Message when an invalid operation is attempted.
    /// </summary>
    [LocalizedString(Description = "Invalid operation")]
    public string InvalidOperation { get; set; } = "Invalid operation: {0}";
}
