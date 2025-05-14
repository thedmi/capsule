namespace Capsule;

public sealed class SelfInvocationEnqueuer(ICapsuleSynchronizer synchronizer) : ISelfInvocationEnqueuer
{
    public void Enqueue(Func<Task> impl) => synchronizer.EnqueueReturn(impl);

    public void Enqueue(Action impl) => synchronizer.EnqueueReturn(impl);
}
