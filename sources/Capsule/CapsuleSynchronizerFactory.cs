using System.Threading.Channels;

namespace Capsule;

public class CapsuleSynchronizerFactory : ICapsuleSynchronizerFactory
{
    private readonly ICapsuleInvocationLoopFactory _invocationLoopFactory;

    public CapsuleSynchronizerFactory(ICapsuleInvocationLoopFactory invocationLoopFactory)
    {
        _invocationLoopFactory = invocationLoopFactory;
    }

    public ICapsuleSynchronizer Create(ICapsule capsule, CapsuleRuntimeContext context)
    {
        var channel = Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(1023)
            { SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait });

        var synchronizer = new CapsuleSynchronizer(channel.Writer, capsule.GetType());
        
        // Ensure the first item in the event loop is a call to InitializeAsync
        synchronizer.EnqueueReturnInternal(capsule.InitializeAsync);
        
        context.Host.RegisterAsync(_invocationLoopFactory.Create(channel.Reader));
        
        return synchronizer;
    }
}
