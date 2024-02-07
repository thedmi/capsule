namespace Capsule;

/// <summary>
/// An error occurred during capsule invocation.
/// </summary>
public class CapsuleInvocationException(string? message) : Exception(message);
