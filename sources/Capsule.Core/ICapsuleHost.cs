namespace Capsule;

/// <summary>
/// The capsule host owns the invocation loops and manages their lifetime.
/// </summary>
public interface ICapsuleHost
{
    void Register(ICapsuleInvocationLoop capsuleInvocationLoop);
}
