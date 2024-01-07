using System.Threading.Channels;

namespace Senja;

public abstract class CapsuleFactory<T, TImpl> : ICapsuleFactory<T> where TImpl : ICapsule
{
    private readonly ICapsuleHost _capsuleHost;

    protected CapsuleFactory(ICapsuleHost capsuleHost)
    {
        _capsuleHost = capsuleHost;
    }
    
    public T CreateCapsule()
    {
        var channel = Channel.CreateBounded<Func<Task>>(1);

        var implementation = CreateImplementation();

        // Ensure the event loop first calls InitializeAsync
        channel.Writer.TryWrite(implementation.InitializeAsync);
        
        _capsuleHost.RegisterAsync(new CapsuleEventLoop(channel.Reader));
        return CreateFacade(implementation, new CapsuleProxy(channel.Writer));
    }

    protected abstract TImpl CreateImplementation();

    protected abstract T CreateFacade(TImpl implementation, ICapsuleProxy capsuleProxy);
}
