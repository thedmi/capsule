namespace Senja;

public interface IActorHost
{
    Task RegisterAsync(IActorEventLoop actorEventLoop);
}
