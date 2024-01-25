using Microsoft.Extensions.Logging;

namespace Capsule.Extensions.DependencyInjection;

public class LoggingAdapter<T>(ILogger<T> logger) : ICapsuleLogger<T>
{
    public void LogDebug(string message)
    {
        logger.LogDebug(message);
    }

    public void LogWarning(Exception exception, string message)
    {
        logger.LogWarning(exception, message);
    }

    public void LogError(Exception exception, string message)
    {
        logger.LogError(exception, message);
    }
}
