using CodeLogic.Abstractions;
using CL.MySQL2.Configuration;
using CL.MySQL2.Localization;
using CL.MySQL2.Models;
using CL.MySQL2.Services;
using CL.Core.Utilities.Caching;

namespace CL.MySQL2;

[LibraryManifest(
    Id = "mysql2",
    Name = "MySQL2 Database Library",
    Version = "2.0.0",
    Description = "MySQL database library with ORM, migrations, and backup support",
    Author = "CodeLogic Team",
    Tags = new[] { "database", "mysql", "orm", "sql" }
)]
[LibraryDependency(Id = "cl.core", MinVersion = "3.0.0")]
/// <summary>
/// MySQL2 library for CodeLogic 3.0 framework.
/// Provides MySQL database connectivity, repository pattern, query building,
/// table synchronization, migrations, and backup capabilities.
/// </summary>
public class MySQL2Library : ILibrary
{
    private DatabaseConfiguration? _config;
    private MySQL2Strings? _strings;
    private ConnectionManager? _connectionManager;
    private TableSyncService? _tableSyncService;
    private MigrationTracker? _migrationTracker;
    private BackupManager? _backupManager;
    private System.Threading.Timer? _healthCheckTimer;
    private LibraryContext? _context;
    private ICache? _cache;
    private bool _disposed = false;

    /// <summary>
    /// Gets the library manifest derived from attributes.
    /// </summary>
    public LibraryManifest Manifest => this.GetType()
        .GetCustomAttributes(typeof(LibraryManifestAttribute), false)
        .Cast<LibraryManifestAttribute>()
        .Select(a => a.ToManifest(this.GetType()))
        .First();

    /// <summary>
    /// Phase 1: Configure
    /// Registers configuration and localization models.
    /// </summary>
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;

        // Register main configuration -> config.json
        context.Configuration.Register<DatabaseConfiguration>();

        // Register localization
        context.Localization.Register<MySQL2Strings>();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 2: Initialize
    /// Sets up services based on loaded configuration.
    /// </summary>
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;

        // Get loaded configuration and localization
        _config = context.Configuration.Get<DatabaseConfiguration>();
        _strings = context.Localization.Get<MySQL2Strings>();

        context.Logger.Info(_strings.LibraryInitialized);

        // Validate configuration
        var validation = _config.Validate();
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors);
            context.Logger.Error(string.Format(_strings.ConfigurationError, errors));
            throw new InvalidOperationException($"MySQL2 configuration is invalid: {errors}");
        }

        // Check if library is enabled
        if (!_config.Enabled)
        {
            context.Logger.Info("MySQL2 library is disabled in configuration");
            return;
        }

        // Initialize ConnectionManager
        _connectionManager = new ConnectionManager(context.Logger);
        _connectionManager.RegisterConfiguration(_config);

        // Initialize TableSyncService if enabled
        if (_config.EnableTableSync)
        {
            _tableSyncService = new TableSyncService(_connectionManager, context.DataDirectory, context.Logger);
        }

        // Initialize MigrationTracker if enabled
        if (_config.EnableMigrations)
        {
            _migrationTracker = new MigrationTracker(context.DataDirectory, context.Logger);
        }

        // Initialize BackupManager if enabled
        if (_config.EnableAutoBackup)
        {
            _backupManager = new BackupManager(context.DataDirectory, context.Logger);
        }

        // Initialize query cache if enabled
        if (_config.EnableCaching)
        {
            _cache = new MemoryCache(new CacheOptions
            {
                MaxItems = _config.MaxCacheItems,
                DefaultExpiration = TimeSpan.FromSeconds(_config.CacheTtl),
                AutoCleanupInterval = TimeSpan.FromMinutes(5)
            });
            context.Logger.Info($"Query cache initialized (TTL: {_config.CacheTtl}s, Max Items: {_config.MaxCacheItems})");
        }

        context.Logger.Info("MySQL2 library initialized successfully");
    }

    /// <summary>
    /// Phase 3: Start
    /// Starts services, connections, and background workers.
    /// </summary>
    public async Task OnStartAsync(LibraryContext context)
    {
        if (_config == null || !_config.Enabled)
            return;

        context.Logger.Info("Starting MySQL2 library...");

        // Test database connection
        context.Logger.Debug("Testing database connection...");
        var connectionTest = await _connectionManager!.TestConnectionAsync(_config.ConnectionId);

        if (!connectionTest)
        {
            context.Logger.Error(_strings!.ConnectionTestFailed);
            throw new InvalidOperationException("Failed to connect to MySQL database. Check your configuration.");
        }

        context.Logger.Info(_strings!.ConnectionTestSuccess);

        // Get server info
        var serverInfo = await _connectionManager.GetServerInfoAsync(_config.ConnectionId);
        if (serverInfo != null)
        {
            context.Logger.Info($"Connected to MySQL {serverInfo.Value.Version} - {serverInfo.Value.ServerInfo}");
        }

        // Migration system is available but not auto-run
        // Users can access via GetMigrationTracker() to run migrations manually
        if (_config.EnableMigrations && _migrationTracker != null)
        {
            context.Logger.Info("Migration tracker initialized. Use GetMigrationTracker() to run migrations manually.");
        }

        // Start health check timer if enabled
        if (_config.EnableHealthChecks)
        {
            var interval = TimeSpan.FromSeconds(_config.HealthCheckInterval);
            _healthCheckTimer = new System.Threading.Timer(
                async _ => await PerformHealthCheckAsync(),
                null,
                interval,
                interval
            );
            context.Logger.Debug($"Health check timer started (interval: {interval})");
        }

        context.Logger.Info(_strings.LibraryStarted);
    }

    /// <summary>
    /// Phase 4: Stop
    /// Stops services gracefully.
    /// </summary>
    public async Task OnStopAsync()
    {
        if (_config == null || !_config.Enabled)
            return;

        _context?.Logger.Info("Stopping MySQL2 library...");

        // Stop health check timer
        if (_healthCheckTimer != null)
        {
            await _healthCheckTimer.DisposeAsync();
            _healthCheckTimer = null;
            _context?.Logger.Debug("Health check timer stopped");
        }

        _context?.Logger.Info(_strings?.LibraryStopped ?? "MySQL2 library stopped");
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _connectionManager?.Dispose();
        _healthCheckTimer?.Dispose();
        (_cache as IDisposable)?.Dispose();

        _context?.Logger.Info(_strings?.LibraryDisposed ?? "MySQL2 library disposed");

        _disposed = true;
    }

    /// <summary>
    /// Health check implementation.
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_config == null || !_config.Enabled)
        {
            return HealthStatus.Healthy("MySQL2 library is disabled");
        }

        if (_connectionManager == null)
        {
            return HealthStatus.Unhealthy("MySQL2 library not initialized");
        }

        try
        {
            var connected = await _connectionManager.TestConnectionAsync(_config.ConnectionId);

            if (!connected)
            {
                return HealthStatus.Unhealthy("Database connection test failed");
            }

            var serverInfo = await _connectionManager.GetServerInfoAsync(_config.ConnectionId);
            if (serverInfo == null)
            {
                return HealthStatus.Degraded("Connected but failed to retrieve server info");
            }

            return HealthStatus.Healthy($"Connected to MySQL {serverInfo.Value.Version}");
        }
        catch (Exception ex)
        {
            return HealthStatus.Unhealthy($"Health check failed: {ex.Message}");
        }
    }

    private async Task PerformHealthCheckAsync()
    {
        try
        {
            var status = await HealthCheckAsync();

            if (status.Status == HealthStatusLevel.Healthy)
            {
                _context?.Logger.Debug(_strings?.HealthCheckPassed ?? "Health check passed");
            }
            else
            {
                var message = string.Format(
                    _strings?.HealthCheckFailed ?? "Health check failed: {0}",
                    status.Message
                );
                _context?.Logger.Warning(message);
            }
        }
        catch (Exception ex)
        {
            _context?.Logger.Error("Health check error", ex);
        }
    }

    // === Public API ===

    /// <summary>
    /// Synchronizes a model type's table structure with the database.
    /// </summary>
    public async Task<bool> SyncTableAsync<T>(string? connectionId = null) where T : class, new()
    {
        if (_tableSyncService == null)
        {
            throw new InvalidOperationException("Table sync is not enabled. Set EnableTableSync=true in configuration.");
        }

        connectionId ??= _config?.ConnectionId ?? "Default";
        return await _tableSyncService.SyncTableAsync<T>(connectionId);
    }

    /// <summary>
    /// Gets a repository instance for the specified model type.
    /// </summary>
    public Repository<T> GetRepository<T>(string? connectionId = null) where T : class, new()
    {
        if (_connectionManager == null)
        {
            throw new InvalidOperationException("MySQL2 library not initialized");
        }

        connectionId ??= _config?.ConnectionId ?? "Default";
        return new Repository<T>(_connectionManager, _context?.Logger, connectionId, _cache);
    }

    /// <summary>
    /// Gets the connection manager for advanced operations.
    /// Use this for transaction support and direct database access.
    /// </summary>
    public ConnectionManager GetConnectionManager()
    {
        if (_connectionManager == null)
        {
            throw new InvalidOperationException("MySQL2 library not initialized");
        }

        return _connectionManager;
    }

    /// <summary>
    /// Gets the migration tracker for manual migration management.
    /// </summary>
    public MigrationTracker? GetMigrationTracker() => _migrationTracker;

    /// <summary>
    /// Gets the backup manager for manual backup operations.
    /// </summary>
    public BackupManager? GetBackupManager() => _backupManager;
}
