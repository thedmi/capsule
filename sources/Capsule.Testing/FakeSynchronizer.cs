namespace Capsule.Testing;

public class FakeSynchronizer : ICapsuleSynchronizer
{
    public Task EnqueueAwaitResult(Func<Task> impl, bool passThroughIfQueueClosed = false) => impl();

    public Task<T> EnqueueAwaitResult<T>(Func<Task<T>> impl, bool passThroughIfQueueClosed = false) => impl();

    public Task EnqueueAwaitReception(Func<Task> impl) => impl();

#pragma warning disable VSTHRD110
    public void EnqueueReturn(Func<Task> impl) => impl();
#pragma warning restore VSTHRD110

    public void EnqueueReturn(Action impl) => impl();

    public T PassThrough<T>(Func<T> impl) => impl();
}
