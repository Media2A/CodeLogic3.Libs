using CodeLogic.Configuration;

namespace CL.SocialConnect.Models;

/// <summary>
/// Configuration for the SocialConnect library
/// </summary>
[ConfigSection("socialconnect")]
public class SocialConnectConfiguration : ConfigModelBase
{
    /// <summary>
    /// Whether the SocialConnect library is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the Discord configuration
    /// </summary>
    public DiscordConfiguration Discord { get; set; } = new();

    /// <summary>
    /// Gets or sets the Steam configuration
    /// </summary>
    public SteamConfiguration Steam { get; set; } = new();

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public override ConfigValidationResult Validate()
    {
        var errors = new List<string>();

        // Validate Discord configuration if any OAuth fields are set
        if (!string.IsNullOrWhiteSpace(Discord.ClientId) ||
            !string.IsNullOrWhiteSpace(Discord.ClientSecret) ||
            !string.IsNullOrWhiteSpace(Discord.RedirectUri))
        {
            if (string.IsNullOrWhiteSpace(Discord.ClientId))
                errors.Add("Discord ClientId is required when Discord OAuth is configured");

            if (string.IsNullOrWhiteSpace(Discord.ClientSecret))
                errors.Add("Discord ClientSecret is required when Discord OAuth is configured");

            if (string.IsNullOrWhiteSpace(Discord.RedirectUri))
                errors.Add("Discord RedirectUri is required when Discord OAuth is configured");
        }

        // Validate timeout values
        if (Discord.TimeoutSeconds < 1 || Discord.TimeoutSeconds > 300)
            errors.Add("Discord TimeoutSeconds must be between 1 and 300");

        if (Discord.MaxRetries < 0 || Discord.MaxRetries > 10)
            errors.Add("Discord MaxRetries must be between 0 and 10");

        if (Steam.TimeoutSeconds < 1 || Steam.TimeoutSeconds > 300)
            errors.Add("Steam TimeoutSeconds must be between 1 and 300");

        if (Steam.CacheDurationSeconds < 0)
            errors.Add("Steam CacheDurationSeconds must be non-negative");

        if (errors.Any())
            return ConfigValidationResult.Invalid(errors);

        return ConfigValidationResult.Valid();
    }
}

/// <summary>
/// Discord configuration
/// </summary>
public class DiscordConfiguration
{
    /// <summary>
    /// Gets or sets the Discord OAuth2 Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Discord OAuth2 Client Secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Discord OAuth2 Redirect URI
    /// </summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Discord API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://discord.com/api/v10";

    /// <summary>
    /// Gets or sets the OAuth2 authorization endpoint
    /// </summary>
    public string AuthorizationEndpoint { get; set; } = "https://discord.com/api/oauth2/authorize";

    /// <summary>
    /// Gets or sets the OAuth2 token endpoint
    /// </summary>
    public string TokenEndpoint { get; set; } = "https://discord.com/api/oauth2/token";

    /// <summary>
    /// Gets or sets the API request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;
}

/// <summary>
/// Steam configuration
/// </summary>
public class SteamConfiguration
{
    /// <summary>
    /// Gets or sets the Steam Web API key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Steam API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.steampowered.com";

    /// <summary>
    /// Gets or sets the Steam OpenID base URL
    /// </summary>
    public string OpenIdBaseUrl { get; set; } = "https://steamcommunity.com/openid";

    /// <summary>
    /// Gets or sets the return URL after Steam authentication
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API request timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the cache duration for profile data in seconds
    /// </summary>
    public int CacheDurationSeconds { get; set; } = 300; // 5 minutes

    /// <summary>
    /// Gets or sets whether to enable caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;
}
