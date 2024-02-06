namespace Capsule;

public class CapsuleSynchronizerFactory(
    ICapsuleQueueFactory queueFactory,
    ICapsuleInvocationLoopFactory invocationLoopFactory) : ICapsuleSynchronizerFactory
{
    public ICapsuleSynchronizer Create(object capsuleImpl, ICapsuleHost host)
    {
        var queue = queueFactory.CreateSynchronizerQueue();
        var synchronizer = new CapsuleSynchronizer(queue.Writer, capsuleImpl.GetType());

        ApplyFeatures(capsuleImpl, synchronizer);

        host.Register(invocationLoopFactory.Create(queue.Reader));

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
