namespace Capsule;

public interface ICapsuleEventLoop
{
    Task RunAsync(CancellationToken cancellationToken);
}
