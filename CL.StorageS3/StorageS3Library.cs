using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.StorageS3.Models;
using CL.StorageS3.Services;

namespace CL.StorageS3;

/// <summary>
/// S3 Storage Library for CodeLogic Framework
/// Provides Amazon S3 and S3-compatible storage operations
/// </summary>
public class StorageS3Library : ILibrary
{
    private LibraryContext? _context;
    private StorageS3Configuration? _config;
    private S3ConnectionManager? _connectionManager;

    /// <summary>
    /// Library manifest information
    /// </summary>
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "cl.storages3",
        Name = "CL.StorageS3",
        Version = "3.0.0",
        Description = "Amazon S3 and S3-compatible storage library for CodeLogic Framework",
        Author = "Media2A",
        Dependencies = Array.Empty<LibraryDependency>()
    };

    #region CodeLogic 3.0 Lifecycle

    /// <summary>
    /// Phase 1: Configure - Register configuration model
    /// </summary>
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;

        // Register configuration -> config/storages3.json
        context.Configuration.Register<StorageS3Configuration>();

        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 2: Initialize - Load configuration and initialize S3 connection manager
    /// </summary>
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;

        // Get loaded configuration
        _config = context.Configuration.Get<StorageS3Configuration>();

        context.Logger.Info($"Initializing {Manifest.Name} v{Manifest.Version}");

        // Validate configuration
        var validation = _config.Validate();
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors);
            context.Logger.Error($"S3 Storage configuration is invalid: {errors}");
            throw new InvalidOperationException($"S3 Storage configuration is invalid: {errors}");
        }

        // Check if library is enabled
        if (!_config.Enabled)
        {
            context.Logger.Info("S3 Storage library is disabled in configuration");
            return;
        }

        // Initialize connection manager
        _connectionManager = new S3ConnectionManager(_context.Logger);

        // Register all configurations
        foreach (var s3Config in _config.Connections)
        {
            try
            {
                _connectionManager.RegisterConfiguration(s3Config);
                _context.Logger.Info($"Registered S3 configuration: {s3Config.ConnectionId}");
            }
            catch (Exception ex)
            {
                _context.Logger.Error($"Failed to register S3 configuration '{s3Config.ConnectionId}': {ex.Message}", ex);
            }
        }

        context.Logger.Info($"{Manifest.Name} initialized successfully");
    }

    /// <summary>
    /// Phase 3: Start - Start library services and test connections
    /// </summary>
    public async Task OnStartAsync(LibraryContext context)
    {
        if (_config == null || !_config.Enabled)
            return;

        _context = context;
        _context.Logger.Info($"Starting {Manifest.Name}");

        // Test S3 connections
        await TestS3ConnectionsAsync();

        _context.Logger.Info($"{Manifest.Name} started and ready");
    }

    /// <summary>
    /// Phase 4: Stop - Stop library services and cleanup
    /// </summary>
    public async Task OnStopAsync()
    {
        if (_config == null || !_config.Enabled)
            return;

        _context?.Logger.Info($"Stopping {Manifest.Name}");

        // Dispose connection manager
        _connectionManager?.Dispose();
        _connectionManager = null;

        _context?.Logger.Info($"{Manifest.Name} stopped successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Health check - Checks S3 connections availability
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_connectionManager == null)
            return HealthStatus.Unhealthy("Connection manager not initialized");

        try
        {
            var connectionIds = _connectionManager.GetConnectionIds();

            if (connectionIds.Count == 0)
                return HealthStatus.Unhealthy("No S3 connections configured");

            var results = new List<string>();
            var allHealthy = true;

            foreach (var connectionId in connectionIds)
            {
                var isHealthy = await _connectionManager.TestConnectionAsync(connectionId);

                if (!isHealthy)
                {
                    allHealthy = false;
                    results.Add($"{connectionId}: Failed");
                }
                else
                {
                    results.Add($"{connectionId}: OK");
                }
            }

            var healthyCount = results.Count(r => r.EndsWith("OK"));
            var detailMessage = $"{string.Join(", ", results)} (Total: {connectionIds.Count}, Healthy: {healthyCount})";

            if (allHealthy)
                return HealthStatus.Healthy(detailMessage);
            else if (healthyCount > 0)
                return HealthStatus.Degraded(detailMessage);
            else
                return HealthStatus.Unhealthy(detailMessage);
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Health check failed: {ex.Message}", ex);
            return HealthStatus.Unhealthy($"Health check error: {ex.Message}");
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

    #endregion

    #region Connection Testing

    /// <summary>
    /// Tests all registered S3 connections
    /// </summary>
    private async Task TestS3ConnectionsAsync()
    {
        if (_connectionManager == null)
        {
            _context?.Logger.Warning("Connection manager not initialized");
            return;
        }

        var connectionIds = _connectionManager.GetConnectionIds();

        if (connectionIds.Count == 0)
        {
            _context?.Logger.Warning("No S3 connections configured");
            return;
        }

        _context?.Logger.Info($"Testing {connectionIds.Count} S3 connection(s)...");

        foreach (var connectionId in connectionIds)
        {
            var success = await _connectionManager.TestConnectionAsync(connectionId);

            if (success)
            {
                var config = _connectionManager.GetConfiguration(connectionId);

                _context?.Logger.Info($"Connection '{connectionId}' successful - Service: {config?.ServiceUrl}");

                // Test default bucket access if configured
                if (!string.IsNullOrWhiteSpace(config?.DefaultBucket))
                {
                    var bucketAccessible = await _connectionManager.TestBucketAccessAsync(
                        config.DefaultBucket, connectionId);

                    if (bucketAccessible)
                    {
                        _context?.Logger.Info($"  Default bucket '{config.DefaultBucket}' is accessible");
                    }
                    else
                    {
                        _context?.Logger.Warning($"  Default bucket '{config.DefaultBucket}' is not accessible");
                    }
                }
            }
            else
            {
                _context?.Logger.Error($"Connection '{connectionId}' failed");
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets a storage service for S3 operations
    /// </summary>
    /// <param name="connectionId">Connection identifier (default: "Default")</param>
    /// <returns>S3StorageService instance</returns>
    /// <exception cref="InvalidOperationException">If connection manager is not initialized</exception>
    public S3StorageService? GetStorageService(string connectionId = "Default")
    {
        if (_connectionManager == null)
        {
            _context?.Logger.Error("Connection manager not initialized");
            return null;
        }

        try
        {
            return new S3StorageService(_connectionManager, connectionId, _context?.Logger);
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Failed to create storage service for '{connectionId}': {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Registers a new S3 bucket configuration
    /// </summary>
    /// <param name="configuration">S3 configuration to register</param>
    public void RegisterBucket(S3Configuration configuration)
    {
        if (_connectionManager == null)
        {
            _context?.Logger.Error("Connection manager not initialized");
            throw new InvalidOperationException("Connection manager not initialized");
        }

        _connectionManager.RegisterConfiguration(configuration);
    }

    /// <summary>
    /// Gets the connection manager instance
    /// </summary>
    /// <returns>S3ConnectionManager instance</returns>
    public S3ConnectionManager? GetConnectionManager()
    {
        return _connectionManager;
    }

    /// <summary>
    /// Gets all registered connection IDs
    /// </summary>
    /// <returns>List of connection IDs</returns>
    public List<string> GetConnectionIds()
    {
        return _connectionManager?.GetConnectionIds() ?? new List<string>();
    }

    #endregion
}
