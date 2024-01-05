namespace Senja;

public interface IActorEventLoop
{
    Task RunAsync(CancellationToken cancellationToken);
}
