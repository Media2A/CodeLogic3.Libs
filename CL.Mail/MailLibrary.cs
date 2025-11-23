using CodeLogic.Abstractions;
using CodeLogic.Models;
using CL.Mail.Models;
using CL.Mail.Services;

namespace CL.Mail;

/// <summary>
/// Mail Library for CodeLogic 3.0 Framework
/// Provides email sending capabilities with SMTP and template system
/// </summary>
public class MailLibrary : ILibrary
{
    /// <summary>
    /// Gets the library manifest
    /// </summary>
    public LibraryManifest Manifest { get; } = new LibraryManifest
    {
        Id = "mail",
        Name = "Mail Library",
        Version = "3.0.0",
        Description = "Modern email library with advanced template system and SMTP support",
        Author = "Media2A.com",
        Dependencies = Array.Empty<LibraryDependency>()
    };

    private LibraryContext? _context;
    private SmtpService? _smtpService;
    private IMailTemplateProvider? _templateProvider;
    private IMailTemplateEngine? _templateEngine;
    private MailConfiguration? _config;

    #region CodeLogic 3.0 Lifecycle

    /// <summary>
    /// Phase 1: Configure
    /// Registers configuration models.
    /// </summary>
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;

        // Register main configuration -> config.json
        context.Configuration.Register<MailConfiguration>();

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
        _config = context.Configuration.Get<MailConfiguration>();

        context.Logger.Info($"Initializing {Manifest.Name}");

        // Validate configuration
        var validation = _config.Validate();
        if (!validation.IsValid)
        {
            var errors = string.Join(", ", validation.Errors);
            context.Logger.Error($"Mail configuration is invalid: {errors}");
            throw new InvalidOperationException($"Mail configuration is invalid: {errors}");
        }

        // Check if library is enabled
        if (!_config.Enabled)
        {
            context.Logger.Info("Mail library is disabled in configuration");
            return;
        }

        // Initialize SMTP service
        _smtpService = new SmtpService(_config.Smtp, context.Logger);

        // Initialize template provider
        _templateProvider = new FileMailTemplateProvider(_config.TemplateDirectory, context.Logger);

        // Initialize template engine
        _templateEngine = new SimpleTemplateEngine(context.Logger);

        context.Logger.Info($"Initialized SMTP service (Host: {_config.Smtp.Host}:{_config.Smtp.Port})");
        context.Logger.Info($"Template directory: {_config.TemplateDirectory}");

        context.Logger.Info("Mail library initialized successfully");
    }

    /// <summary>
    /// Phase 3: Start
    /// Starts services and verifies configuration.
    /// </summary>
    public async Task OnStartAsync(LibraryContext context)
    {
        if (_config == null || !_config.Enabled)
            return;

        context.Logger.Info("Starting Mail library...");

        context.Logger.Info("Mail library started and ready");
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

        _context?.Logger.Info("Stopping Mail library...");

        _smtpService = null;
        _templateProvider = null;
        _templateEngine = null;

        _context?.Logger.Info("Mail library stopped");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Health check implementation.
    /// </summary>
    public async Task<HealthStatus> HealthCheckAsync()
    {
        if (_config == null || !_config.Enabled)
        {
            return HealthStatus.Healthy("Mail library is disabled");
        }

        if (_smtpService == null || _templateProvider == null || _templateEngine == null)
        {
            return HealthStatus.Unhealthy("Mail library not initialized");
        }

        try
        {
            // Basic health check - verify services are instantiated
            var smtpConfigValid = !string.IsNullOrWhiteSpace(_config.Smtp.Host);

            if (smtpConfigValid)
                return HealthStatus.Healthy($"Mail services operational (SMTP: {_config.Smtp.Host}:{_config.Smtp.Port})");
            else
                return HealthStatus.Degraded("SMTP configuration incomplete");
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
        _smtpService = null;
        _templateProvider = null;
        _templateEngine = null;

        _context?.Logger.Info("Mail library disposed");
    }

    #endregion

    #region Public API

    /// <summary>
    /// Gets the SMTP service for sending emails
    /// </summary>
    public SmtpService GetSmtpService()
    {
        if (_smtpService == null)
            throw new InvalidOperationException("Mail library not initialized");

        return _smtpService;
    }

    /// <summary>
    /// Gets the template provider for loading templates
    /// </summary>
    public IMailTemplateProvider GetTemplateProvider()
    {
        if (_templateProvider == null)
            throw new InvalidOperationException("Mail library not initialized");

        return _templateProvider;
    }

    /// <summary>
    /// Gets the template engine for rendering templates
    /// </summary>
    public IMailTemplateEngine GetTemplateEngine()
    {
        if (_templateEngine == null)
            throw new InvalidOperationException("Mail library not initialized");

        return _templateEngine;
    }

    /// <summary>
    /// Creates a new mail builder
    /// </summary>
    public MailBuilder CreateMailBuilder() => new MailBuilder();

    /// <summary>
    /// Creates a new template builder
    /// </summary>
    public TemplateBuilder CreateTemplateBuilder() => new TemplateBuilder();

    #endregion
}
