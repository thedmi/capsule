using Microsoft.Extensions.Logging;

namespace Capsule.Extensions.DependencyInjection;

public class LoggingAdapter<T> : ICapsuleLogger<T>
{
    private readonly ILogger<T> _logger;

    public LoggingAdapter(ILogger<T> logger)
    {
        _logger = logger;
    }

    public void LogDebug(string message)
    {
        _logger.LogDebug(message);
    }

    public void LogWarning(Exception exception, string message)
    {
        _logger.LogWarning(exception, message);
    }

    public void LogError(Exception exception, string message)
    {
        _logger.LogError(exception, message);
    }
}
