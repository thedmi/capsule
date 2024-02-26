namespace Capsule;

public class DefaultSynchronizerFactory(
    ICapsuleQueueFactory queueFactory,
    ICapsuleInvocationLoopFactory invocationLoopFactory) : ICapsuleSynchronizerFactory
{
    public ICapsuleSynchronizer Create(object capsuleImpl, ICapsuleHost host)
    {
        var capsuleType = capsuleImpl.GetType();
        
        var queue = queueFactory.CreateSynchronizerQueue();
        var synchronizer = new CapsuleSynchronizer(queue.Writer, capsuleType);

        ApplyFeatures(capsuleImpl, synchronizer);

        host.Register(invocationLoopFactory.Create(queue.Reader, capsuleType));

        return synchronizer;
    }

    private static void ApplyFeatures(object capsuleImpl, CapsuleSynchronizer synchronizer)
    {
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
    }
}
