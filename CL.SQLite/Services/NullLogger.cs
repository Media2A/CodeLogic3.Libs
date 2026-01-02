using CodeLogic.Logging;

namespace CL.SQLite.Services;

internal sealed class NullLogger : ILogger
{
    /// <summary>
    /// No-op trace logger.
    /// </summary>
    public void Trace(string message) { }

    /// <summary>
    /// No-op debug logger.
    /// </summary>
    public void Debug(string message) { }

    /// <summary>
    /// No-op info logger.
    /// </summary>
    public void Info(string message) { }

    /// <summary>
    /// No-op warning logger.
    /// </summary>
    public void Warning(string message) { }

    /// <summary>
    /// No-op error logger.
    /// </summary>
    public void Error(string message, Exception? exception = null) { }

    /// <summary>
    /// No-op critical logger.
    /// </summary>
    public void Critical(string message, Exception? exception = null) { }
}
