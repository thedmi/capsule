namespace Capsule;

public interface ICapsuleSynchronizer
{
    Task EnqueueAwaitResult(Func<Task> impl);
    Task<TResult> EnqueueAwaitResult<TResult>(Func<Task<TResult>> impl);

    Task EnqueueAwaitReception(Func<Task> impl);

    Task EnqueueReturn(Func<Task> impl);
    T PassThrough<T>(Func<T> impl);
}
