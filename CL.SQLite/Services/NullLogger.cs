using CodeLogic.Logging;

namespace CL.SQLite.Services;

internal sealed class NullLogger : ILogger
{
    public void Trace(string message) { }
    public void Debug(string message) { }
    public void Info(string message) { }
    public void Warning(string message) { }
    public void Error(string message, Exception? exception = null) { }
    public void Critical(string message, Exception? exception = null) { }
}
