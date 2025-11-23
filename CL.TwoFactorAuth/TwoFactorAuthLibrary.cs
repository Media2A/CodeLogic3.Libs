using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.TwoFactorAuth.Models;
using CL.TwoFactorAuth.Services;

namespace CL.TwoFactorAuth;

/// <summary>
/// Two-Factor Authentication Library for CodeLogic 3.0 Framework
/// </summary>
public class TwoFactorAuthLibrary : ILibrary
{
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "twofactorauth",
        Name = "Two-Factor Authentication Library",
        Version = "3.0.0",
        Description = "Two-factor authentication library with TOTP and QR code generation",
        Author = "Media2A.com",
        Dependencies = Array.Empty<LibraryDependency>()
    };

    private LibraryContext? _context;
    private TwoFactorAuthenticator? _authenticator;
    private QrCodeGenerator? _qrGenerator;
    private TwoFactorAuthConfiguration? _config;

    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Configuring {Manifest.Name} v{Manifest.Version}");

        // Register configuration -> config.json
        context.Configuration.Register<TwoFactorAuthConfiguration>();

        _context.Logger.Info($"{Manifest.Name} configured successfully");
        await Task.CompletedTask;
    }

    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Initializing {Manifest.Name}");

        // Get loaded configuration
        _config = context.Configuration.Get<TwoFactorAuthConfiguration>();

        _authenticator = new TwoFactorAuthenticator(_config, _context.Logger);
        _qrGenerator = new QrCodeGenerator(_config, _context.Logger);

        _context.Logger.Info($"{Manifest.Name} initialized successfully");
        await Task.CompletedTask;
    }

    public async Task OnStartAsync(LibraryContext context)
    {
        _context = context;
        _context.Logger.Info($"Starting {Manifest.Name}");
        _context.Logger.Info($"{Manifest.Name} started and ready");
        await Task.CompletedTask;
    }

    public async Task OnStopAsync()
    {
        _context?.Logger.Info($"Stopping {Manifest.Name}");
        _authenticator = null;
        _qrGenerator = null;
        _context?.Logger.Info($"{Manifest.Name} stopped successfully");
        await Task.CompletedTask;
    }

    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_authenticator == null || _qrGenerator == null)
            return HealthStatus.Unhealthy("2FA services not initialized");

        try
        {
            var testKey = _authenticator.GenerateSecretKey();
            if (string.IsNullOrEmpty(testKey))
                return HealthStatus.Unhealthy("Failed to generate secret key");

            return HealthStatus.Healthy("2FA services operational");
        }
        catch (Exception ex)
        {
            _context?.Logger.Error($"Health check failed: {ex.Message}", ex);
            return HealthStatus.Unhealthy($"Health check error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _authenticator = null;
        _qrGenerator = null;
    }

    public TwoFactorAuthenticator GetAuthenticator() =>
        _authenticator ?? throw new InvalidOperationException("Library not initialized");

    public QrCodeGenerator GetQrCodeGenerator() =>
        _qrGenerator ?? throw new InvalidOperationException("Library not initialized");
}
