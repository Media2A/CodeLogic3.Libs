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

    [LocalizedString(Description = "Connection established message")]
    public string ConnectionEstablished { get; set; } = "Database connection established";

    [LocalizedString(Description = "Connection failed message")]
    public string ConnectionFailed { get; set; } = "Failed to connect to database";

    [LocalizedString(Description = "Connection closed message")]
    public string ConnectionClosed { get; set; } = "Database connection closed";

    [LocalizedString(Description = "Connection test successful")]
    public string ConnectionTestSuccess { get; set; } = "Connection test successful";

    [LocalizedString(Description = "Connection test failed")]
    public string ConnectionTestFailed { get; set; } = "Connection test failed";

    // === Library Lifecycle ===

    [LocalizedString(Description = "Library initialized")]
    public string LibraryInitialized { get; set; } = "MySQL2 library initialized";

    [LocalizedString(Description = "Library started")]
    public string LibraryStarted { get; set; } = "MySQL2 library started";

    [LocalizedString(Description = "Library stopped")]
    public string LibraryStopped { get; set; } = "MySQL2 library stopped";

    [LocalizedString(Description = "Library disposed")]
    public string LibraryDisposed { get; set; } = "MySQL2 library disposed";

    // === Table Sync Messages ===

    [LocalizedString(Description = "Table sync started")]
    public string TableSyncStarted { get; set; } = "Table synchronization started for {0}";

    [LocalizedString(Description = "Table sync completed")]
    public string TableSyncCompleted { get; set; } = "Table synchronization completed for {0}";

    [LocalizedString(Description = "Table created")]
    public string TableCreated { get; set; } = "Table {0} created successfully";

    [LocalizedString(Description = "Table updated")]
    public string TableUpdated { get; set; } = "Table {0} updated successfully";

    [LocalizedString(Description = "Table sync failed")]
    public string TableSyncFailed { get; set; } = "Table synchronization failed for {0}";

    // === Migration Messages ===

    [LocalizedString(Description = "Migrations started")]
    public string MigrationsStarted { get; set; } = "Running pending migrations";

    [LocalizedString(Description = "Migrations completed")]
    public string MigrationsCompleted { get; set; } = "Migrations completed successfully";

    [LocalizedString(Description = "Migration applied")]
    public string MigrationApplied { get; set; } = "Migration {0} applied successfully";

    [LocalizedString(Description = "Migrations failed")]
    public string MigrationsFailed { get; set; } = "Migrations failed";

    // === Backup Messages ===

    [LocalizedString(Description = "Backup started")]
    public string BackupStarted { get; set; } = "Database backup started";

    [LocalizedString(Description = "Backup completed")]
    public string BackupCompleted { get; set; } = "Database backup completed: {0}";

    [LocalizedString(Description = "Backup failed")]
    public string BackupFailed { get; set; } = "Database backup failed";

    // === Query Messages ===

    [LocalizedString(Description = "Query executed")]
    public string QueryExecuted { get; set; } = "Query executed successfully";

    [LocalizedString(Description = "Slow query detected")]
    public string SlowQueryDetected { get; set; } = "Slow query detected ({0}ms): {1}";

    [LocalizedString(Description = "Query failed")]
    public string QueryFailed { get; set; } = "Query execution failed";

    // === Repository Messages ===

    [LocalizedString(Description = "Record inserted")]
    public string RecordInserted { get; set; } = "Record inserted into {0}";

    [LocalizedString(Description = "Records bulk inserted")]
    public string RecordsBulkInserted { get; set; } = "{0} records inserted into {1}";

    [LocalizedString(Description = "Record updated")]
    public string RecordUpdated { get; set; } = "Record updated in {0}";

    [LocalizedString(Description = "Record deleted")]
    public string RecordDeleted { get; set; } = "Record deleted from {0}";

    [LocalizedString(Description = "Record not found")]
    public string RecordNotFound { get; set; } = "Record not found in {0}";

    // === Transaction Messages ===

    [LocalizedString(Description = "Transaction started")]
    public string TransactionStarted { get; set; } = "Transaction started";

    [LocalizedString(Description = "Transaction committed")]
    public string TransactionCommitted { get; set; } = "Transaction committed successfully";

    [LocalizedString(Description = "Transaction rolled back")]
    public string TransactionRolledBack { get; set; } = "Transaction rolled back";

    [LocalizedString(Description = "Transaction failed")]
    public string TransactionFailed { get; set; } = "Transaction failed";

    // === Health Check Messages ===

    [LocalizedString(Description = "Health check passed")]
    public string HealthCheckPassed { get; set; } = "Health check passed";

    [LocalizedString(Description = "Health check failed")]
    public string HealthCheckFailed { get; set; } = "Health check failed: {0}";

    // === Error Messages ===

    [LocalizedString(Description = "Configuration error")]
    public string ConfigurationError { get; set; } = "Configuration error: {0}";

    [LocalizedString(Description = "Database error")]
    public string DatabaseError { get; set; } = "Database error: {0}";

    [LocalizedString(Description = "Invalid operation")]
    public string InvalidOperation { get; set; } = "Invalid operation: {0}";
}
