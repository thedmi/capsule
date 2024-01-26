namespace Capsule;

public interface ICapsuleInvocationLoop
{
    Task RunAsync(CancellationToken cancellationToken);
}
