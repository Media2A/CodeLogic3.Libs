using CodeLogic.Configuration;
using System.ComponentModel.DataAnnotations;

namespace CL.TwoFactorAuth.Models;

/// <summary>
/// Configuration settings for two-factor authentication
/// Auto-generated as config/twofactorauth.json
/// </summary>
[ConfigSection("twofactorauth")]
public class TwoFactorAuthConfiguration : ConfigModelBase
{
    /// <summary>
    /// Enable or disable the library
    /// </summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// Gets or sets the time step in seconds for TOTP (default: 30)
    /// </summary>
    public int TimeStepSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the window size for code validation tolerance (default: 1)
    /// </summary>
    public int WindowSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the QR code module size in pixels (default: 20)
    /// </summary>
    public int QrCodeModuleSize { get; set; } = 20;

    /// <summary>
    /// Gets or sets the error correction level for QR codes (default: Q)
    /// </summary>
    public QrErrorCorrectionLevel ErrorCorrectionLevel { get; set; } = QrErrorCorrectionLevel.Q;

    public override ConfigValidationResult Validate()
    {
        var errors = new List<string>();

        if (TimeStepSeconds < 1 || TimeStepSeconds > 300)
            errors.Add("TimeStepSeconds must be between 1 and 300");

        if (WindowSize < 0 || WindowSize > 10)
            errors.Add("WindowSize must be between 0 and 10");

        if (QrCodeModuleSize < 1 || QrCodeModuleSize > 100)
            errors.Add("QrCodeModuleSize must be between 1 and 100");

        return errors.Count > 0
            ? ConfigValidationResult.Invalid(errors)
            : ConfigValidationResult.Valid();
    }
}

/// <summary>
/// QR code error correction levels
/// </summary>
public enum QrErrorCorrectionLevel
{
    /// <summary>
    /// Level L - 7% error correction
    /// </summary>
    L,

    /// <summary>
    /// Level M - 15% error correction
    /// </summary>
    M,

    /// <summary>
    /// Level Q - 25% error correction
    /// </summary>
    Q,

    /// <summary>
    /// Level H - 30% error correction
    /// </summary>
    H
}

/// <summary>
/// Result of a 2FA operation
/// </summary>
public record TwoFactorResult
{
    /// <summary>
    /// Gets whether the operation was successful
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the result message
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets any additional data from the operation
    /// </summary>
    public object? Data { get; init; }

    public static TwoFactorResult Succeeded(string? message = null, object? data = null) =>
        new() { Success = true, Message = message, Data = data };

    public static TwoFactorResult Failed(string message) =>
        new() { Success = false, Message = message };
}
