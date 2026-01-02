using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.PostgreSQL.Models;
using CL.PostgreSQL.Services;

namespace CL.PostgreSQL;

/// <summary>
/// CL.PostgreSQL - A fully integrated PostgreSQL library for the CodeLogic framework.
/// Provides high-performance database operations with comprehensive logging, configuration, and connection management.
/// </summary>
public class PostgreSQL2Library : ILibrary
{
    private LibraryContext? _context;
    private PostgreSQLConfiguration? _config;
    private ConnectionManager? _connectionManager;
    private TableSyncService? _tableSyncService;
    private readonly Dictionary<string, DatabaseConfiguration> _databaseConfigs = new();

    /// <summary>
    /// Library manifest metadata for CL.PostgreSQL.
    /// </summary>
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "cl.postgresql",
        Name = "CL.PostgreSQL",
        Version = "3.0.0",
        Description = "Fully integrated PostgreSQL database library with connection pooling, caching, and comprehensive ORM support",
        Author = "Media2A",
        Tags = new[] { "database", "postgresql", "orm", "sql" }
    };

    /// <summary>
    /// Phase 1: Configure - Register configuration model
    /// </summary>
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Configuring {Manifest.Name} v{Manifest.Version}");

        // Register PostgreSQL configuration with centralized system
        context.Configuration.Register<PostgreSQLConfiguration>();

        _context.Logger.Info($"{Manifest.Name} configured successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 2: Initialize - Initialize PostgreSQL connection manager and test connections
    /// </summary>
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Initializing {Manifest.Name}");

        // Get loaded configuration from centralized system
        _config = context.Configuration.Get<PostgreSQLConfiguration>();

        // Validate configuration
        var validation = _config.Validate();
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors);
            _context.Logger.Error($"PostgreSQL configuration is invalid: {errors}");
            throw new InvalidOperationException($"PostgreSQL configuration is invalid: {errors}");
        }

        // Initialize connection manager with logger
        _connectionManager = new ConnectionManager(_context.Logger);

        // Register all enabled database configurations
        foreach (var kvp in _config.Databases)
        {
            var dbConfig = kvp.Value;

            if (!dbConfig.Enabled)
            {
                _context.Logger.Info($"Skipping disabled database configuration: {dbConfig.ConnectionId}");
                continue;
            }

            try
            {
                RegisterDatabase(dbConfig.ConnectionId, dbConfig);
                _context.Logger.Info($"Registered PostgreSQL configuration: {dbConfig.ConnectionId} -> {dbConfig.Database}");
            }
            catch (Exception ex)
            {
                _context.Logger.Error($"Failed to register PostgreSQL configuration '{dbConfig.ConnectionId}': {ex.Message}", ex);
            }
        }

        // Test connections for all registered databases
        await TestDatabaseConnectionsAsync();

        // Initialize TableSyncService
        if (!string.IsNullOrEmpty(_context.DataDirectory))
        {
            _tableSyncService = new TableSyncService(_connectionManager, _context.DataDirectory, _context.Logger);
            _context.Logger.Info("TableSyncService initialized successfully");
        }

        _context.Logger.Info($"{Manifest.Name} initialized successfully with {_databaseConfigs.Count} database(s)");
    }

    /// <summary>
    /// Phase 3: Start - Start library services
    /// </summary>
    public async Task OnStartAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Starting {Manifest.Name}");
        _context.Logger.Info($"{Manifest.Name} started and ready");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 4: Stop - Stop library services and cleanup
    /// </summary>
    public async Task OnStopAsync()
    {
        _context?.Logger.Info($"Stopping {Manifest.Name}");

        _connectionManager?.Dispose();

        _context?.Logger.Info($"{Manifest.Name} stopped successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Health check - Checks PostgreSQL database connections
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_connectionManager == null)
            return HealthStatus.Unhealthy("Connection manager not initialized");

        // Test all database connections
        var connectionIds = _connectionManager.GetConnectionIds().ToList();

        if (!connectionIds.Any())
            return HealthStatus.Unhealthy("No database connections configured");

        var failedConnections = new List<string>();

        foreach (var connectionId in connectionIds)
        {
            var testResult = await _connectionManager.TestConnectionAsync(connectionId);
            if (!testResult)
            {
                failedConnections.Add(connectionId);
            }
        }

        if (failedConnections.Any())
        {
            return HealthStatus.Unhealthy($"Failed to connect to database(s): {string.Join(", ", failedConnections)}");
        }

        return HealthStatus.Healthy($"All {connectionIds.Count} database connection(s) are operational");
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        _connectionManager?.Dispose();
        _connectionManager = null;
    }

    /// <summary>
    /// Gets a repository for the specified model type.
    /// </summary>
    public Repository<T>? GetRepository<T>(string connectionId = "Default") where T : class, new()
    {
        if (_connectionManager == null)
        {
            _context?.Logger.Error("Cannot create repository: ConnectionManager not initialized");
            return null;
        }

        try
        {
            return new Repository<T>(_connectionManager, _context?.Logger, connectionId);
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Failed to create repository for {typeof(T).Name}", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets a query builder for the specified model type.
    /// </summary>
    public QueryBuilder<T>? GetQueryBuilder<T>(string connectionId = "Default") where T : class, new()
    {
        if (_connectionManager == null)
        {
            _context?.Logger.Error("Cannot create query builder: ConnectionManager not initialized");
            return null;
        }

        try
        {
            return new QueryBuilder<T>(_connectionManager, _context?.Logger, connectionId);
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Failed to create query builder for {typeof(T).Name}", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets a non-generic query builder factory.
    /// </summary>
    public QueryBuilder? GetQueryBuilder(string connectionId = "Default")
    {
        if (_connectionManager == null)
        {
            _context?.Logger.Error("Cannot create query builder: ConnectionManager not initialized");
            return null;
        }

        try
        {
            return new QueryBuilder(_connectionManager, _context?.Logger);
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Failed to create query builder", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets the connection manager instance.
    /// </summary>
    public ConnectionManager? GetConnectionManager()
    {
        return _connectionManager;
    }

    /// <summary>
    /// Gets the table sync service for schema synchronization.
    /// </summary>
    public TableSyncService? GetTableSyncService()
    {
        return _tableSyncService;
    }

    /// <summary>
    /// Synchronizes a single model type with its database table.
    /// </summary>
    public async Task<bool> SyncTableAsync<T>(
        string connectionId = "Default",
        bool createBackup = true) where T : class
    {
        if (_tableSyncService == null)
        {
            _context?.Logger.Error("Cannot sync table: TableSyncService not initialized");
            return false;
        }

        try
        {
            return await _tableSyncService.SyncTableAsync<T>(connectionId, createBackup);
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Failed to sync table: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Synchronizes multiple model types at once.
    /// </summary>
    public async Task<Dictionary<string, bool>> SyncTablesAsync(
        Type[] modelTypes,
        string connectionId = "Default",
        bool createBackup = true)
    {
        if (_tableSyncService == null)
        {
            _context?.Logger.Error("Cannot sync tables: TableSyncService not initialized");
            return new Dictionary<string, bool>();
        }

        try
        {
            return await _tableSyncService.SyncTablesAsync(modelTypes, connectionId, createBackup);
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Failed to sync tables: {ex.Message}", ex);
            return new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// Synchronizes all model types in a specified namespace.
    /// </summary>
    public async Task<Dictionary<string, bool>> SyncNamespaceAsync(
        string namespaceName,
        string connectionId = "Default",
        bool createBackup = true,
        bool includeDerivedNamespaces = false)
    {
        if (_tableSyncService == null)
        {
            _context?.Logger.Error("Cannot sync namespace: TableSyncService not initialized");
            return new Dictionary<string, bool>();
        }

        try
        {
            return await _tableSyncService.SyncNamespaceAsync(
                namespaceName,
                connectionId,
                createBackup,
                includeDerivedNamespaces);
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Failed to sync namespace: {ex.Message}", ex);
            return new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// Registers a new database configuration at runtime.
    /// </summary>
    public void RegisterDatabase(string connectionId, DatabaseConfiguration config)
    {
        if (_connectionManager == null)
        {
            throw new InvalidOperationException("ConnectionManager not initialized");
        }

        config.ConnectionId = connectionId;
        _connectionManager.RegisterConfiguration(config);
        _databaseConfigs[connectionId] = config;

        _context?.Logger.Info($"Registered database configuration: {connectionId}");
    }

    private async Task TestDatabaseConnectionsAsync()
    {
        if (_connectionManager == null)
            return;

        var connectionIds = _connectionManager.GetConnectionIds().ToList();

        foreach (var connectionId in connectionIds)
        {
            _context?.Logger.Info($"Testing connection '{connectionId}'...");

            var success = await _connectionManager.TestConnectionAsync(connectionId);

            if (success)
            {
                _context?.Logger.Info($"Connection '{connectionId}' successful");

                // Get server info
                var serverInfo = await _connectionManager.GetServerInfoAsync(connectionId);
                if (serverInfo.HasValue)
                {
                    _context?.Logger.Info($"  Server: {serverInfo.Value.ServerInfo} v{serverInfo.Value.Version}");
                }
            }
            else
            {
                _context?.Logger.Warning($"Failed to connect to database '{connectionId}'");
            }
        }
    }
}
