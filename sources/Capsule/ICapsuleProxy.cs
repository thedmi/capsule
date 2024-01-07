namespace Capsule;

public interface ICapsuleProxy
{
    Task EnqueueAwaitResult(Func<Task> impl);
    Task<TResult> EnqueueAwaitResult<TResult>(Func<Task<TResult>> impl);

    Task EnqueueAwaitReception(Func<Task> impl);

    void EnqueueReturn(Func<Task> impl);
    T PassThrough<T>(Func<T> impl);
}
