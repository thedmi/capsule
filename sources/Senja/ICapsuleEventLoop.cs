namespace Senja;

public interface ICapsuleEventLoop
{
    Task RunAsync(CancellationToken cancellationToken);
}
