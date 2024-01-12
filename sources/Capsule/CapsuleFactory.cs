using System.Threading.Channels;

namespace Capsule;

public abstract class CapsuleFactory<T, TImpl> : ICapsuleFactory<T> where TImpl : ICapsule
{
    private readonly CapsuleRuntimeContext _runtimeContext;
    
    protected CapsuleFactory(CapsuleRuntimeContext runtimeContext)
    {
        _runtimeContext = runtimeContext;
    }
    
    public T CreateCapsule()
    {
        var channel = Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(1023)
            { SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait });

        var implementation = CreateImplementation();

        var invoker = new CapsuleSynchronizer(channel.Writer, typeof(TImpl));
        
        // Ensure the first item in the event loop is a call to InitializeAsync
        invoker.EnqueueReturn(implementation.InitializeAsync);
        
        _runtimeContext.Host.RegisterAsync(_runtimeContext.InvocationLoopFactory.Create(channel.Reader));
        return CreateFacade(implementation, invoker);
    }

    protected abstract TImpl CreateImplementation();

    protected abstract T CreateFacade(TImpl implementation, ICapsuleSynchronizer synchronizer);
}
