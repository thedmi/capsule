namespace Capsule;

public interface ICapsuleHost
{
    Task RegisterAsync(ICapsuleEventLoop capsuleEventLoop);
}
