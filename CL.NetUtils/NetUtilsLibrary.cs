using CodeLogic.Abstractions;
using CL.NetUtils.Models;
using CL.NetUtils.Services;

namespace CL.NetUtils;

/// <summary>
/// Network Utilities Library for CodeLogic 3.0 Framework
/// Provides DNS blacklist checking and IP geolocation services
/// </summary>
public class NetUtilsLibrary : ILibrary
{
    /// <summary>
    /// Gets the library manifest
    /// </summary>
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "netutils",
        Name = "Network Utilities Library",
        Version = "3.0.0",
        Description = "Network utilities library with DNS blacklist checking and IP geolocation",
        Author = "Media2A.com",
        Dependencies = Array.Empty<LibraryDependency>()
    };

    private LibraryContext? _context;
    private DnsblChecker? _dnsblChecker;
    private Ip2LocationService? _ipLocationService;
    private NetUtilsConfiguration? _config;

    #region CodeLogic 3.0 Lifecycle

    /// <summary>
    /// Phase 1: Configure
    /// Registers configuration models.
    /// </summary>
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;

        // Register main configuration -> config.json
        context.Configuration.Register<NetUtilsConfiguration>();

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
        _config = context.Configuration.Get<NetUtilsConfiguration>();

        context.Logger.Info($"Initializing {Manifest.Name}");

        // Validate configuration
        var validation = _config.Validate();
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors);
            context.Logger.Error($"NetUtils configuration is invalid: {errors}");
            throw new InvalidOperationException($"NetUtils configuration is invalid: {errors}");
        }

        // Check if library is enabled
        if (!_config.Enabled)
        {
            context.Logger.Info("NetUtils library is disabled in configuration");
            return;
        }

        // Initialize DNSBL checker
        _dnsblChecker = new DnsblChecker(_config.Dnsbl, context.Logger);
        context.Logger.Info($"DNSBL checker initialized with {_config.Dnsbl.DnsblServers.Count} blacklist servers");

        // Initialize IP location service
        _ipLocationService = new Ip2LocationService(_config.Ip2Location, context.Logger);

        try
        {
            await _ipLocationService.InitializeAsync();
            context.Logger.Info("IP geolocation service initialized successfully");
        }
        catch (Exception ex)
        {
            context.Logger.Warning($"IP geolocation service initialization failed: {ex.Message}");
            // Don't fail initialization - DNSBL can still work
        }

        context.Logger.Info($"{Manifest.Name} initialized successfully");
    }

    /// <summary>
    /// Phase 3: Start
    /// Starts services, connections, and background workers.
    /// </summary>
    public async Task OnStartAsync(LibraryContext context)
    {
        if (_config == null || !_config.Enabled)
            return;

        context.Logger.Info($"Starting {Manifest.Name}");

        context.Logger.Info($"{Manifest.Name} started and ready");
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

        _dnsblChecker = null;
        _ipLocationService?.Dispose();
        _ipLocationService = null;

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
            return HealthStatus.Healthy("NetUtils library is disabled");
        }

        if (_dnsblChecker == null)
        {
            return HealthStatus.Unhealthy("NetUtils library not initialized");
        }

        try
        {
            // Check if both services are available
            bool dnsblAvailable = _dnsblChecker != null;
            bool ipLocationAvailable = _ipLocationService != null;

            if (dnsblAvailable && ipLocationAvailable)
                return HealthStatus.Healthy("All network services operational");
            else if (dnsblAvailable)
                return HealthStatus.Degraded("DNSBL available, IP geolocation unavailable");
            else
                return HealthStatus.Unhealthy("Network services not available");
        }
        catch (Exception ex)
        {
            return HealthStatus.Unhealthy($"Health check failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Dispose resources.
    /// </summary>
    public void Dispose()
    {
        _dnsblChecker = null;
        _ipLocationService?.Dispose();
        _ipLocationService = null;

        _context?.Logger.Info($"{Manifest.Name} disposed");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets the DNSBL checker service
    /// </summary>
    public DnsblChecker GetDnsblChecker()
    {
        if (_dnsblChecker == null)
            throw new InvalidOperationException("Network utilities library not initialized");

        return _dnsblChecker;
    }

    /// <summary>
    /// Gets the IP location service
    /// </summary>
    public Ip2LocationService GetIpLocationService()
    {
        if (_ipLocationService == null)
            throw new InvalidOperationException("IP location service not initialized");

        return _ipLocationService;
    }

    #endregion
}
