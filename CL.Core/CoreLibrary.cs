using CodeLogic.Abstractions;
using CodeLogic.Models;

namespace CL.Core;

/// <summary>
/// CL.Core library providing core utilities and helpers for CodeLogic 3.0 framework
/// </summary>
public class CoreLibrary : ILibrary
{
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "cl.core",
        Name = "Core Utilities Library",
        Version = "3.0.0",
        Description = "Core utilities and helpers library for CodeLogic framework",
        Author = "Media2A.com",
        Dependencies = Array.Empty<LibraryDependency>()
    };

    private LibraryContext? _context;

    /// <summary>
    /// Phase 1: Configure - Called to configure the library (generate config files, etc.)
    /// </summary>
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Configuring {Manifest.Name} v{Manifest.Version}");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 2: Initialize - Called to initialize the library after configuration
    /// </summary>
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Initializing {Manifest.Name}");

        // Core library has no dependencies to initialize
        _context.Logger.Info($"{Manifest.Name} initialized successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 3: Start - Called to start the library's services
    /// </summary>
    public async Task OnStartAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Starting {Manifest.Name}");

        // Core library is now operational
        _context.Logger.Info($"{Manifest.Name} started and ready");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 4: Stop - Called when the library is being stopped
    /// </summary>
    public async Task OnStopAsync()
    {
        _context?.Logger.Info($"Stopping {Manifest.Name}");

        // Cleanup resources if needed
        _context?.Logger.Info($"{Manifest.Name} stopped successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Health check - Returns current health status of the library
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_context == null)
            return HealthStatus.Unhealthy("Library not initialized");

        // Core library has no external dependencies, so it's always healthy if initialized
        return await Task.FromResult(HealthStatus.Healthy($"{Manifest.Name} is operational"));
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        // No resources to dispose
    }

    /// <summary>
    /// Gets the library context for accessing utilities
    /// </summary>
    public LibraryContext GetContext()
    {
        if (_context == null)
            throw new InvalidOperationException("Library not initialized. Call OnInitializeAsync first.");
        return _context;
    }
}
