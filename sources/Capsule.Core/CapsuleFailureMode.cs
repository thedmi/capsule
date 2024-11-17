using Capsule.GenericHosting;

namespace Capsule;

/// <summary>
/// How capsule invocation loops should handle invocation failures, i.e. uncaught exceptions for loop-owned invocations.
/// </summary>
public enum CapsuleFailureMode
{
    /// <summary>
    /// Log the failed invocation, then continue with the next one.
    /// </summary>
    Continue,

    /// <summary>
    /// Escalate and abort the invocation loop. The loop will throw the uncaught exception, pending invocations will
    /// not be processed anymore.
    /// </summary>
    /// <remarks>
    /// When working with this mode in default Capsule setups, uncaught exceptions will terminate the owning
    /// <see cref="InvocationLoop"/> and its <see cref="CapsuleHost"/>. The exception then propagates to the
    /// <see cref="CapsuleBackgroundService"/>, where it will crash the app in .NET 6 and newer.
    /// </remarks>
    Abort,
}
