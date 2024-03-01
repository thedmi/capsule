namespace Capsule.Testing;

public class FakeSynchronizer : ICapsuleSynchronizer
{
    public Task EnqueueAwaitResult(Func<Task> impl, bool passThroughIfQueueClosed = false) => impl();

    public Task<T> EnqueueAwaitResult<T>(Func<Task<T>> impl, bool passThroughIfQueueClosed = false) => impl();

    public Task EnqueueAwaitReception(Func<Task> impl) => impl();

    public Task EnqueueReturn(Func<Task> impl) => impl();

    public T PassThrough<T>(Func<T> impl) => impl();
}
