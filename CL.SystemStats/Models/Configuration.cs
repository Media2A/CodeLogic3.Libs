using CodeLogic.Configuration;
using System.ComponentModel.DataAnnotations;

namespace CL.SystemStats.Models;

/// <summary>
/// Configuration for the SystemStats library.
/// This model is auto-generated as config/systemstats.json when missing.
/// </summary>
[ConfigSection("systemstats")]
public class SystemStatsConfiguration : ConfigModelBase
{
    /// <summary>
    /// Gets or sets whether to enable caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Gets or sets the cache duration in seconds
    /// </summary>
    [Range(1, 3600)]
    public int CacheDurationSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the CPU sampling interval in milliseconds
    /// </summary>
    [Range(10, 10000)]
    public int CpuSamplingIntervalMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of CPU samples to average
    /// </summary>
    [Range(1, 100)]
    public int CpuSamplesForAverage { get; set; } = 3;

    /// <summary>
    /// Gets or sets whether to enable CPU temperature monitoring
    /// </summary>
    public bool EnableTemperatureMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to monitor individual process statistics
    /// </summary>
    public bool EnableProcessMonitoring { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of top processes to track
    /// </summary>
    [Range(1, 1000)]
    public int MaxTopProcesses { get; set; } = 10;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    public override ConfigValidationResult Validate()
    {
        var errors = new List<string>();

        if (CacheDurationSeconds < 1 || CacheDurationSeconds > 3600)
            errors.Add("CacheDurationSeconds must be between 1 and 3600");

        if (CpuSamplingIntervalMs < 10 || CpuSamplingIntervalMs > 10000)
            errors.Add("CpuSamplingIntervalMs must be between 10 and 10000");

        if (CpuSamplesForAverage < 1 || CpuSamplesForAverage > 100)
            errors.Add("CpuSamplesForAverage must be between 1 and 100");

        if (MaxTopProcesses < 1 || MaxTopProcesses > 1000)
            errors.Add("MaxTopProcesses must be between 1 and 1000");

        if (errors.Any())
            return ConfigValidationResult.Invalid(errors);

        return ConfigValidationResult.Valid();
    }
}
