namespace Capsule;

public class CapsuleSynchronizerFactory(ICapsuleQueueFactory queueFactory, ICapsuleInvocationLoopFactory invocationLoopFactory)
    : ICapsuleSynchronizerFactory
{
    public ICapsuleSynchronizer Create(object capsuleImpl, CapsuleRuntimeContext context)
    {
        return CreateSynchronizer(capsuleImpl, context.Host);
    }

    private ICapsuleSynchronizer CreateSynchronizer(object capsuleImpl, ICapsuleHost host)
    {
        var queue = queueFactory.CreateSynchronizerQueue();

        var synchronizer = new CapsuleSynchronizer(queue.Writer, capsuleImpl.GetType());

        // If the implementation uses timers, inject the timer service
        if (capsuleImpl is CapsuleFeature.ITimers t)
        {
            t.Timers = new TimerService(synchronizer);
        }
        
        // If the implementation requires initialization, ensure this is the first call in the invocation queue
        if (capsuleImpl is CapsuleFeature.IInitializer i)
        {
            synchronizer.EnqueueReturnInternal(i.InitializeAsync);
        }
        
        host.Register(invocationLoopFactory.Create(queue.Reader));
        
        return synchronizer;
    }
}
