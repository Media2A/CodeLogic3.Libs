using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.SocialConnect.Models;
using CL.SocialConnect.Services.Discord;
using CL.SocialConnect.Services.Steam;

namespace CL.SocialConnect;

/// <summary>
/// Social platform integration library for CodeLogic framework
/// </summary>
public class SocialConnectLibrary : ILibrary
{
    private DiscordWebhookService? _discordWebhookService;
    private SteamProfileService? _steamProfileService;
    private SteamAuthenticationService? _steamAuthService;
    private LibraryContext? _context;
    private SocialConnectConfiguration? _config;

    /// <summary>
    /// Gets the library manifest
    /// </summary>
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "cl.socialconnect",
        Name = "CL.SocialConnect",
        Version = "3.0.0",
        Description = "Social platform integration with Discord and Steam support",
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

        // Register configuration -> config.json
        context.Configuration.Register<SocialConnectConfiguration>();

        _context.Logger.Info($"{Manifest.Name} configured successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 2: Initialize - Initialize social services
    /// </summary>
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Initializing {Manifest.Name}");

        // Get loaded configuration
        _config = context.Configuration.Get<SocialConnectConfiguration>();

        // Validate configuration
        var validation = _config.Validate();
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors);
            context.Logger.Error($"SocialConnect configuration is invalid: {errors}");
            throw new InvalidOperationException($"SocialConnect configuration is invalid: {errors}");
        }

        // Check if library is enabled
        if (!_config.Enabled)
        {
            context.Logger.Info("SocialConnect library is disabled in configuration");
            return;
        }

        // Initialize Discord webhook service
        _discordWebhookService = new DiscordWebhookService(_context.Logger);

        // Initialize Steam profile service
        if (!string.IsNullOrWhiteSpace(_config.Steam.ApiKey))
        {
            _steamProfileService = new SteamProfileService(
                _config.Steam.ApiKey,
                _config.Steam.ApiBaseUrl,
                _config.Steam.CacheDurationSeconds,
                _config.Steam.EnableCaching,
                _context.Logger);
        }

        // Initialize Steam authentication service
        if (!string.IsNullOrWhiteSpace(_config.Steam.ReturnUrl))
        {
            _steamAuthService = new SteamAuthenticationService(
                _config.Steam.OpenIdBaseUrl,
                _config.Steam.ReturnUrl,
                _context.Logger);
        }

        _context.Logger.Info($"{Manifest.Name} initialized successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Phase 3: Start - Start library services
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
    /// Phase 4: Stop - Stop library services and cleanup
    /// </summary>
    public async Task OnStopAsync()
    {
        if (_config == null || !_config.Enabled)
            return;

        _context?.Logger.Info($"Stopping {Manifest.Name}");

        _discordWebhookService = null;
        _steamProfileService = null;
        _steamAuthService?.Dispose();
        _steamAuthService = null;

        _context?.Logger.Info($"{Manifest.Name} stopped successfully");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Health check - Checks social services availability
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_config == null || !_config.Enabled)
        {
            return HealthStatus.Healthy("SocialConnect library is disabled");
        }

        if (_discordWebhookService == null)
            return HealthStatus.Unhealthy("Social services not initialized");

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
        _discordWebhookService = null;
        _steamProfileService = null;
        _steamAuthService?.Dispose();
        _steamAuthService = null;
    }

    /// <summary>
    /// Gets the Discord webhook service
    /// </summary>
    public DiscordWebhookService GetDiscordWebhookService()
    {
        if (_discordWebhookService == null)
            throw new InvalidOperationException("Library not initialized");

        return _discordWebhookService;
    }

    /// <summary>
    /// Gets the Steam profile service
    /// </summary>
    public SteamProfileService GetSteamProfileService()
    {
        if (_steamProfileService == null)
            throw new InvalidOperationException("Steam API key not configured");

        return _steamProfileService;
    }

    /// <summary>
    /// Gets the Steam authentication service
    /// </summary>
    public SteamAuthenticationService GetSteamAuthenticationService()
    {
        if (_steamAuthService == null)
            throw new InvalidOperationException("Steam return URL not configured");

        return _steamAuthService;
    }
}
