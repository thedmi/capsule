namespace Capsule;

public interface ICapsuleHost
{
    Task RegisterAsync(ICapsuleInvocationLoop capsuleInvocationLoop);
}
