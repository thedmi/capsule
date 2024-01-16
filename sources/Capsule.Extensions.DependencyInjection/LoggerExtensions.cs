using Microsoft.Extensions.Logging;

namespace Capsule.Extensions.DependencyInjection;

public static class LoggerExtensions
{
    public static ICapsuleLogger<T> AsCapsuleLogger<T>(this ILogger<T> logger) => new LoggingAdapter<T>(logger);
}
