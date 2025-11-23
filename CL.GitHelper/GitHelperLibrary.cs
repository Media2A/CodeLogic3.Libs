using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.GitHelper.Models;
using CL.GitHelper.Services;

namespace CL.GitHelper;

/// <summary>
/// Git Helper Library for CodeLogic 3.0 Framework
/// Provides Git repository operations with LibGit2Sharp integration
/// </summary>
public class GitHelperLibrary : ILibrary
{
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "githelper",
        Name = "Git Helper Library",
        Version = "3.0.0",
        Description = "Git repository management library with LibGit2Sharp integration",
        Author = "Media2A",
        Dependencies = Array.Empty<LibraryDependency>()
    };

    private LibraryContext? _context;
    private GitManager? _gitManager;
    private GitHelperConfiguration? _config;

    #region CodeLogic 3.0 Lifecycle

    /// <summary>
    /// Phase 1: Configure - Register configuration models
    /// </summary>
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Configuring {Manifest.Name} v{Manifest.Version}");

        // Register configuration -> config.json
        context.Configuration.Register<GitHelperConfiguration>();

        _context.Logger.Info($"{Manifest.Name} configured successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 2: Initialize - Initialize Git manager and test repositories
    /// </summary>
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Initializing {Manifest.Name}");

        // Get loaded configuration
        _config = context.Configuration.Get<GitHelperConfiguration>();

        if (_config == null)
        {
            _context.Logger.Warning("No Git configuration loaded");
            return;
        }

        // Initialize Git manager
        _gitManager = new GitManager(_config, _context.Logger);

        _context.Logger.Info($"Initialized GitManager with {_config.Repositories.Count} repository configuration(s)");

        // Test repository connections
        await TestRepositoryConnectionsAsync();

        _context.Logger.Info($"{Manifest.Name} initialized successfully");
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

        // Dispose Git manager
        _gitManager?.Dispose();
        _gitManager = null;

        _context?.Logger.Info($"{Manifest.Name} stopped successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Health check - Checks all registered Git repositories
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_gitManager == null)
            return HealthStatus.Unhealthy("Git manager not initialized");

        try
        {
            var results = await _gitManager.HealthCheckAsync();

            if (results.Count == 0)
                return HealthStatus.Degraded("No Git repositories configured");

            var healthyCount = results.Values.Count(h => h);
            var allHealthy = healthyCount == results.Count;

            var message = $"{healthyCount}/{results.Count} repositories healthy";

            if (allHealthy)
                return HealthStatus.Healthy(message);
            else if (healthyCount > 0)
                return HealthStatus.Degraded(message);
            else
                return HealthStatus.Unhealthy(message);
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
        _gitManager?.Dispose();
    }

    #endregion

    #region Repository Testing

    /// <summary>
    /// Tests all registered Git repository connections
    /// </summary>
    private async Task TestRepositoryConnectionsAsync()
    {
        if (_gitManager == null || _config == null)
        {
            _context?.Logger.Warning("Git manager not initialized");
            return;
        }

        var repositoryIds = _gitManager.GetRepositoryIds();

        if (repositoryIds.Count == 0)
        {
            _context?.Logger.Warning("No Git repositories configured");
            return;
        }

        _context?.Logger.Info($"Testing {repositoryIds.Count} Git repository(ies)...");

        foreach (var repositoryId in repositoryIds)
        {
            try
            {
                var config = _gitManager.GetConfiguration(repositoryId);
                if (config == null)
                {
                    _context?.Logger.Error($"Repository configuration not found: {repositoryId}");
                    continue;
                }

                // Get or create repository instance
                var repository = await _gitManager.GetRepositoryAsync(repositoryId);

                // Check if repository exists locally
                var localPath = config.UseAppDataDir
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "CodeLogic", config.LocalPath)
                    : config.LocalPath;

                if (!Directory.Exists(localPath) || !Directory.Exists(Path.Combine(localPath, ".git")))
                {
                    _context?.Logger.Warning(
                        $"Repository '{repositoryId}' not cloned yet - Local path: {localPath}");
                    _context?.Logger.Info(
                        $"  To clone, call CloneAsync() on the repository instance");
                }
                else
                {
                    // Try to get repository info
                    var info = await repository.GetRepositoryInfoAsync();

                    if (info.Success)
                    {
                        _context?.Logger.Info(
                            $"Repository '{repositoryId}' accessible - Branch: {info.Data?.CurrentBranch}, Path: {localPath}");

                        if (info.Data?.IsDirty == true)
                        {
                            _context?.Logger.Warning(
                                $"  Repository has uncommitted changes ({info.Data.ModifiedFiles} modified, {info.Data.UntrackedFiles} untracked)");
                        }
                    }
                    else
                    {
                        _context?.Logger.Error(
                            $"Failed to access repository '{repositoryId}': {info.ErrorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                _context?.Logger.Error(
                    $"Error testing repository '{repositoryId}': {ex.Message}", ex);
            }
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets a Git repository instance
    /// </summary>
    public async Task<GitRepository?> GetRepositoryAsync(string repositoryId = "Default")
    {
        if (_gitManager == null)
        {
            _context?.Logger.Error("Git manager not initialized");
            return null;
        }

        try
        {
            return await _gitManager.GetRepositoryAsync(repositoryId);
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Failed to get repository '{repositoryId}': {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Gets the Git manager instance
    /// </summary>
    public GitManager? GetGitManager()
    {
        return _gitManager;
    }

    /// <summary>
    /// Registers a new Git repository configuration
    /// </summary>
    public void RegisterRepository(RepositoryConfiguration configuration)
    {
        if (_gitManager == null)
            throw new InvalidOperationException("Git manager not initialized");

        _gitManager.RegisterRepository(configuration);
    }

    /// <summary>
    /// Gets all registered repository IDs
    /// </summary>
    public List<string> GetRepositoryIds()
    {
        return _gitManager?.GetRepositoryIds() ?? new List<string>();
    }

    /// <summary>
    /// Gets cache statistics from the Git manager
    /// </summary>
    public CacheStatistics? GetCacheStatistics()
    {
        return _gitManager?.GetCacheStatistics();
    }

    /// <summary>
    /// Clears the repository cache
    /// </summary>
    public async Task ClearCacheAsync()
    {
        if (_gitManager != null)
        {
            await _gitManager.ClearCacheAsync();
            _context?.Logger.Info("Repository cache cleared");
        }
    }

    /// <summary>
    /// Fetches updates for all configured repositories
    /// </summary>
    public async Task<Dictionary<string, OperationResult>?> FetchAllAsync(
        FetchOptions? fetchOptions = null,
        int maxConcurrency = 0)
    {
        if (_gitManager == null)
        {
            _context?.Logger.Error("Git manager not initialized");
            return null;
        }

        return await _gitManager.FetchAllAsync(fetchOptions, maxConcurrency);
    }

    /// <summary>
    /// Gets status for all configured repositories
    /// </summary>
    public async Task<Dictionary<string, GitOperationResult<RepositoryStatus>>?> GetAllStatusAsync(
        int maxConcurrency = 0)
    {
        if (_gitManager == null)
        {
            _context?.Logger.Error("Git manager not initialized");
            return null;
        }

        return await _gitManager.GetAllStatusAsync(maxConcurrency);
    }

    #endregion
}
