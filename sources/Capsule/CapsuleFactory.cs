using System.Threading.Channels;

namespace Capsule;

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

        var invoker = new CapsuleSynchronizer(channel.Writer, typeof(TImpl));
        
        // Ensure the first item in the event loop is a call to InitializeAsync
        invoker.EnqueueReturn(implementation.InitializeAsync);
        
        _capsuleHost.RegisterAsync(new CapsuleInvocationLoop(channel.Reader));
        return CreateFacade(implementation, invoker);
    }

    protected abstract TImpl CreateImplementation();

    protected abstract T CreateFacade(TImpl implementation, ICapsuleSynchronizer synchronizer);
}
