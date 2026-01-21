namespace CL.SystemStats;

using CodeLogic.Abstractions;
using CodeLogic.Models;
using Models;
using Services;

/// <summary>
/// CL.SystemStats library for retrieving cross-platform system statistics
/// </summary>
public class SystemStatsLibrary : ILibrary
{
    private SystemStatsService? _systemStatsService;
    private LibraryContext? _context;
    private SystemStatsConfiguration? _config;

    /// <summary>
    /// Gets the library manifest
    /// </summary>
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "cl.systemstats",
        Name = "CL.SystemStats",
        Version = "3.0.0",
        Description = "Cross-platform system statistics library supporting Windows and Linux",
        Author = "Media2A.com",
        Dependencies = Array.Empty<LibraryDependency>()
    };

    /// <summary>
    /// Phase 1: Configure - Load configuration
    /// </summary>
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Configuring {Manifest.Name} v{Manifest.Version}");

        // Register configuration -> config/systemstats.json
        context.Configuration.Register<SystemStatsConfiguration>();

        _context.Logger.Info($"{Manifest.Name} configured successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 2: Initialize - Initialize system stats service
    /// </summary>
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Initializing {Manifest.Name}");

        // Get loaded configuration
        _config = context.Configuration.Get<SystemStatsConfiguration>();

        // Validate configuration
        var validation = _config.Validate();
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors);
            _context.Logger.Error($"Configuration validation failed: {errors}");
            throw new InvalidOperationException($"SystemStats configuration is invalid: {errors}");
        }

        _systemStatsService = new SystemStatsService(_config);
        var initResult = await _systemStatsService.InitializeAsync();

        if (!initResult.IsSuccess)
            throw new InvalidOperationException($"Failed to initialize system stats service: {initResult.ErrorMessage}");

        _context.Logger.Info($"{Manifest.Name} initialized successfully - Platform: {_systemStatsService.GetPlatformInfo()}");
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

        if (_systemStatsService != null)
            await _systemStatsService.DisposeAsync();

        _systemStatsService = null;

        _context?.Logger.Info($"{Manifest.Name} stopped successfully");
    }

    /// <summary>
    /// Health check - Checks system stats service availability
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_systemStatsService == null || !_systemStatsService.IsInitialized)
            return HealthStatus.Unhealthy("System stats service not initialized");

        try
        {
            return HealthStatus.Healthy($"{Manifest.Name} is operational");
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
        if (_systemStatsService != null)
        {
            _systemStatsService.DisposeAsync().AsTask().Wait();
            _systemStatsService = null;
        }
    }

    /// <summary>
    /// Gets the system statistics service
    /// </summary>
    public SystemStatsService GetSystemStatsService()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return _systemStatsService;
    }

    /// <summary>
    /// Gets CPU information
    /// </summary>
    public async Task<SystemStatsResult<CpuInfo>> GetCpuInfoAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetCpuInfoAsync();
    }

    /// <summary>
    /// Gets current CPU statistics
    /// </summary>
    public async Task<SystemStatsResult<CpuStats>> GetCpuStatsAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetCpuStatsAsync();
    }

    /// <summary>
    /// Gets memory information
    /// </summary>
    public async Task<SystemStatsResult<MemoryInfo>> GetMemoryInfoAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetMemoryInfoAsync();
    }

    /// <summary>
    /// Gets current memory statistics
    /// </summary>
    public async Task<SystemStatsResult<MemoryStats>> GetMemoryStatsAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetMemoryStatsAsync();
    }

    /// <summary>
    /// Gets system uptime
    /// </summary>
    public async Task<SystemStatsResult<TimeSpan>> GetSystemUptimeAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetSystemUptimeAsync();
    }

    /// <summary>
    /// Gets statistics for a specific process
    /// </summary>
    public async Task<SystemStatsResult<ProcessStats>> GetProcessStatsAsync(int processId)
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetProcessStatsAsync(processId);
    }

    /// <summary>
    /// Gets all running processes
    /// </summary>
    public async Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetAllProcessesAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetAllProcessesAsync();
    }

    /// <summary>
    /// Gets top processes by CPU usage
    /// </summary>
    public async Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByCpuAsync(int topCount = 10)
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetTopProcessesByCpuAsync(topCount);
    }

    /// <summary>
    /// Gets top processes by memory usage
    /// </summary>
    public async Task<SystemStatsResult<IReadOnlyList<ProcessStats>>> GetTopProcessesByMemoryAsync(int topCount = 10)
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetTopProcessesByMemoryAsync(topCount);
    }

    /// <summary>
    /// Gets a complete system snapshot
    /// </summary>
    public async Task<SystemStatsResult<SystemSnapshot>> GetSystemSnapshotAsync()
    {
        if (_systemStatsService == null)
            throw new InvalidOperationException("Library not initialized");

        return await _systemStatsService.GetSystemSnapshotAsync();
    }
}
