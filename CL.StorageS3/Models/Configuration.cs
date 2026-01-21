using Amazon;
using Amazon.S3;
using CodeLogic.Configuration;
using System.ComponentModel.DataAnnotations;

namespace CL.StorageS3.Models;

/// <summary>
/// Configuration model for S3 storage connections
/// </summary>
public class S3Configuration
{
    /// <summary>
    /// Unique identifier for this S3 configuration
    /// </summary>
    public string ConnectionId { get; set; } = "Default";

    /// <summary>
    /// AWS access key ID or compatible S3 service access key
    /// </summary>
    public string AccessKey { get; set; } = "";

    /// <summary>
    /// AWS secret access key or compatible S3 service secret key
    /// </summary>
    public string SecretKey { get; set; } = "";

    /// <summary>
    /// S3 service endpoint URL (e.g., https://s3.amazonaws.com or custom endpoint)
    /// </summary>
    public string ServiceUrl { get; set; } = "";

    /// <summary>
    /// Public URL for accessing objects (if different from service URL)
    /// </summary>
    public string PublicUrl { get; set; } = "";

    /// <summary>
    /// AWS region (e.g., us-east-1, eu-west-1)
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// Default bucket name for this configuration
    /// </summary>
    public string DefaultBucket { get; set; } = "";

    /// <summary>
    /// Force path-style bucket addressing (required for MinIO and some S3-compatible services)
    /// </summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>
    /// Use SSL/TLS for connections
    /// </summary>
    public bool UseHttps { get; set; } = true;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum number of retry attempts for failed operations
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Enable request/response logging for debugging
    /// </summary>
    public bool EnableLogging { get; set; } = false;

    /// <summary>
    /// Builds and returns a configured AmazonS3Client instance
    /// </summary>
    /// <returns>Configured AmazonS3Client</returns>
    public AmazonS3Client BuildClient()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = ServiceUrl,
            ForcePathStyle = ForcePathStyle,
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds),
            MaxErrorRetry = MaxRetries,
            UseHttp = !UseHttps
        };

        // Set region if provided
        if (!string.IsNullOrWhiteSpace(Region))
        {
            try
            {
                config.RegionEndpoint = RegionEndpoint.GetBySystemName(Region);
            }
            catch
            {
                // If region parsing fails, service URL will be used instead
            }
        }

        // Enable logging if requested
        if (EnableLogging)
        {
            config.LogResponse = true;
            config.LogMetrics = true;
        }

        return new AmazonS3Client(AccessKey, SecretKey, config);
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    /// <returns>True if configuration is valid</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(AccessKey) &&
               !string.IsNullOrWhiteSpace(SecretKey) &&
               !string.IsNullOrWhiteSpace(ServiceUrl);
    }

    /// <summary>
    /// Returns a default configuration template
    /// </summary>
    public static S3Configuration GetDefaultTemplate()
    {
        return new S3Configuration
        {
            ConnectionId = "Default",
            AccessKey = "your-access-key",
            SecretKey = "your-secret-key",
            ServiceUrl = "https://s3.amazonaws.com",
            PublicUrl = "https://s3.amazonaws.com",
            Region = "us-east-1",
            DefaultBucket = "my-bucket",
            ForcePathStyle = false,
            UseHttps = true,
            TimeoutSeconds = 30,
            MaxRetries = 3,
            EnableLogging = false
        };
    }
}

/// <summary>
/// Root configuration container for multiple S3 connections
/// This model is auto-generated as config/storages3.json when missing.
/// </summary>
[ConfigSection("storages3")]
public class StorageS3Configuration : ConfigModelBase
{
    /// <summary>
    /// Whether this S3 storage library is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// List of configured S3 connections
    /// </summary>
    [Required]
    public List<S3Configuration> Connections { get; set; } = new();

    /// <summary>
    /// Gets a configuration by ConnectionId
    /// </summary>
    public S3Configuration? GetConnection(string connectionId)
    {
        return Connections.FirstOrDefault(c =>
            c.ConnectionId.Equals(connectionId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns a default configuration template
    /// </summary>
    public static StorageS3Configuration GetDefaultTemplate()
    {
        return new StorageS3Configuration
        {
            Enabled = true,
            Connections = new List<S3Configuration>
            {
                S3Configuration.GetDefaultTemplate()
            }
        };
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public override ConfigValidationResult Validate()
    {
        var errors = new List<string>();

        if (Connections == null || Connections.Count == 0)
        {
            errors.Add("At least one S3 connection must be configured");
        }
        else
        {
            // Validate each connection
            foreach (var connection in Connections)
            {
                if (!connection.IsValid())
                {
                    errors.Add($"Connection '{connection.ConnectionId}' is invalid: AccessKey, SecretKey, and ServiceUrl are required");
                }
            }

            // Check for duplicate ConnectionIds
            var duplicates = Connections
                .GroupBy(c => c.ConnectionId, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                errors.Add($"Duplicate connection IDs found: {string.Join(", ", duplicates)}");
            }
        }

        if (errors.Any())
            return ConfigValidationResult.Invalid(errors);

        return ConfigValidationResult.Valid();
    }
}
