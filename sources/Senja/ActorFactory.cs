using System.Threading.Channels;

namespace Senja;

public abstract class ActorFactory<T, TImpl> : IActorFactory<T> where TImpl : IActor
{
    private readonly IActorHost _actorHost;

    protected ActorFactory(IActorHost actorHost)
    {
        _actorHost = actorHost;
    }
    
    public T CreateActor()
    {
        var channel = Channel.CreateUnbounded<Func<Task>>();

        var implementation = CreateImplementation();

        // Ensure the event loop first calls InitializeAsync
        channel.Writer.TryWrite(implementation.InitializeAsync);
        
        _actorHost.RegisterAsync(new ActorEventLoop(channel.Reader));
        return CreateFacade(implementation, new ActorProxy(channel.Writer));
    }

    protected abstract TImpl CreateImplementation();

    protected abstract T CreateFacade(TImpl implementation, IActorProxy actorProxy);
}
