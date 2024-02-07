namespace Capsule;

/// <summary>
/// The invocation loop reads and executes invocations and is thus responsible for the run-to-completion semantics that
/// Capsule provides.
/// </summary>
public interface ICapsuleInvocationLoop
{
    Task RunAsync(CancellationToken cancellationToken);
}
