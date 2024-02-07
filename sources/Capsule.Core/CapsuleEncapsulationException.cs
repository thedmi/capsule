namespace Capsule;

/// <summary>
/// Exception thrown when wrapping an object into a thread-safe capsule fails.
/// </summary>
public class CapsuleEncapsulationException(string? message) : Exception(message);
