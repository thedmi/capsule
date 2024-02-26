namespace Capsule;

// ReSharper disable once UnusedTypeParameter
public interface ICapsuleLogger<T>
{
    void LogDebug(string message, params object?[] args);

    void LogWarning(Exception exception, string message);

    void LogError(Exception exception, string message);
}
