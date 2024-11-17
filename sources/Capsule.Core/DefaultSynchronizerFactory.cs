using Microsoft.Extensions.Logging;

namespace Capsule;

public class DefaultSynchronizerFactory(
    ICapsuleQueueFactory queueFactory,
    ICapsuleInvocationLoopFactory invocationLoopFactory,
    ILoggerFactory loggerFactory
) : ICapsuleSynchronizerFactory
{
    public ICapsuleSynchronizer Create(object capsuleImpl, ICapsuleHost host)
    {
        var capsuleType = capsuleImpl.GetType();

        var invocationLoopStatus = new InvocationLoopStatus();

        var queue = queueFactory.CreateSynchronizerQueue();
        var synchronizer = new CapsuleSynchronizer(queue.Writer, invocationLoopStatus, capsuleType);

        ApplyFeatures(capsuleImpl, synchronizer);

        host.Register(invocationLoopFactory.Create(queue.Reader, invocationLoopStatus, capsuleType));

        return synchronizer;
    }

    private void ApplyFeatures(object capsuleImpl, CapsuleSynchronizer synchronizer)
    {
        // If the implementation uses timers, inject the timer service
        if (capsuleImpl is CapsuleFeature.ITimers t)
        {
            t.Timers = new TimerService(synchronizer, loggerFactory.CreateLogger<TimerService>());
        }

        // If the implementation requires initialization, ensure this is the first call in the invocation queue
        if (capsuleImpl is CapsuleFeature.IInitializer i)
        {
            synchronizer.EnqueueReturn(i.InitializeAsync);
        }
    }
}
