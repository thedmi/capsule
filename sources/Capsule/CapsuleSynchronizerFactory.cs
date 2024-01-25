using System.Threading.Channels;

namespace Capsule;

public class CapsuleSynchronizerFactory(ICapsuleInvocationLoopFactory invocationLoopFactory)
    : ICapsuleSynchronizerFactory
{
    public ICapsuleSynchronizer Create(object capsuleImpl, CapsuleRuntimeContext context)
    {
        var channel = Channel.CreateBounded<Func<Task>>(new BoundedChannelOptions(1023)
            { SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait });

        var synchronizer = new CapsuleSynchronizer(channel.Writer, capsuleImpl.GetType());

        // If the implementation requires initialization, ensure this is the first call in the invocation queue
        if (capsuleImpl is ICapsuleInitialization c)
        {
            synchronizer.EnqueueReturnInternal(c.InitializeAsync);
        }
        
        context.Host.RegisterAsync(invocationLoopFactory.Create(channel.Reader));
        
        return synchronizer;
    }
}
