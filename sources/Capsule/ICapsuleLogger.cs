namespace Capsule;

// ReSharper disable once UnusedTypeParameter
public interface ICapsuleLogger<T>
{
    void LogDebug(string message);

    void LogWarning(Exception exception, string message);

    void LogError(Exception exception, string message);
}
