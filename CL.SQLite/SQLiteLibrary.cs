using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.SQLite.Models;
using CL.SQLite.Services;

namespace CL.SQLite;

/// <summary>
/// SQLite library implementation for CodeLogic framework
/// </summary>
public class SQLiteLibrary : ILibrary
{
    private ConnectionManager? _connectionManager;
    private TableSyncService? _tableSyncService;
    private LibraryContext? _context;
    private SQLiteConfiguration? _config;

    /// <summary>
    /// Library manifest metadata for CL.SQLite.
    /// </summary>
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "cl.sqlite",
        Name = "CL.SQLite",
        Version = "3.0.0",
        Description = "SQLite database library with model-based operations and connection pooling",
        Author = "Media2A.com",
        Dependencies = Array.Empty<LibraryDependency>()
    };

    /// <summary>
    /// Gets the TableSyncService for schema synchronization.
    /// </summary>
    public TableSyncService? TableSyncService => _tableSyncService;

    /// <summary>
    /// Phase 1: Configure
    /// Registers configuration models.
    /// </summary>
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;

        // Register main configuration -> config.json
        context.Configuration.Register<SQLiteConfiguration>();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 2: Initialize
    /// Sets up services based on loaded configuration.
    /// </summary>
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;

        // Get loaded configuration
        _config = context.Configuration.Get<SQLiteConfiguration>();

        _context.Logger.Info($"Initializing {Manifest.Name}");

        // Validate configuration
        var validation = _config.Validate();
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors);
            _context.Logger.Error($"SQLite configuration is invalid: {errors}");
            throw new InvalidOperationException($"SQLite configuration is invalid: {errors}");
        }

        // Check if library is enabled
        if (!_config.Enabled)
        {
            _context.Logger.Info("SQLite library is disabled in configuration");
            return;
        }

        // Initialize ConnectionManager
        _connectionManager = new ConnectionManager(_config, _context.Logger, _context.DataDirectory);

        // Initialize TableSyncService if enabled
        if (!_config.SkipTableSync)
        {
            _tableSyncService = new TableSyncService(_connectionManager, _context.DataDirectory, _context.Logger);
        }

        _context.Logger.Info($"{Manifest.Name} initialized successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 3: Start
    /// Starts library services.
    /// </summary>
    public async Task OnStartAsync(LibraryContext context)
    {
        if (_config == null || !_config.Enabled)
            return;

        _context = context;
        _context.Logger.Info($"Starting {Manifest.Name}");
        _context.Logger.Info($"{Manifest.Name} started and ready");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 4: Stop
    /// Stops services gracefully.
    /// </summary>
    public async Task OnStopAsync()
    {
        if (_config == null || !_config.Enabled)
            return;

        _context?.Logger.Info($"Stopping {Manifest.Name}");

        _connectionManager?.Dispose();
        _connectionManager = null;

        _context?.Logger.Info($"{Manifest.Name} stopped successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Health check implementation.
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_config == null || !_config.Enabled)
        {
            return HealthStatus.Healthy("SQLite library is disabled");
        }

        if (_connectionManager == null)
        {
            return HealthStatus.Unhealthy("SQLite library not initialized");
        }

        // Try to get a connection as a health check
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var conn = await _connectionManager.GetConnectionAsync(cts.Token);
            await _connectionManager.ReleaseConnectionAsync(conn);

            return HealthStatus.Healthy($"{Manifest.Name} is operational");
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Health check failed: {ex.Message}", ex);
            return HealthStatus.Unhealthy($"Failed to get database connection: {ex.Message}");
        }
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
    /// Gets the connection manager (must be initialized first)
    /// </summary>
    public ConnectionManager GetConnectionManager()
    {
        if (_connectionManager == null)
            throw new InvalidOperationException("Library not initialized");

        return _connectionManager;
    }

    /// <summary>
    /// Creates a repository for the specified model type
    /// </summary>
    public Repository<T> CreateRepository<T>() where T : class, new()
    {
        if (_connectionManager == null || _context == null)
            throw new InvalidOperationException("Library not initialized");

        return new Repository<T>(_connectionManager, _context.Logger);
    }

    /// <summary>
    /// Creates a type-safe LINQ query builder for the specified model type
    /// </summary>
    public QueryBuilder<T> CreateQueryBuilder<T>() where T : class, new()
    {
        if (_connectionManager == null || _context == null)
            throw new InvalidOperationException("Library not initialized");

        return new QueryBuilder<T>(_connectionManager, _context.Logger);
    }
}
