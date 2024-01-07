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
        var channel = Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(1023)
            { SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait });

        var implementation = CreateImplementation();

        var capsuleProxy = new CapsuleProxy(channel.Writer, typeof(TImpl));
        
        // Ensure the first item in the event loop is a call to InitializeAsync
        capsuleProxy.EnqueueReturn(implementation.InitializeAsync);
        
        _capsuleHost.RegisterAsync(new CapsuleEventLoop(channel.Reader));
        return CreateFacade(implementation, capsuleProxy);
    }

    protected abstract TImpl CreateImplementation();

    protected abstract T CreateFacade(TImpl implementation, ICapsuleProxy capsuleProxy);
}
