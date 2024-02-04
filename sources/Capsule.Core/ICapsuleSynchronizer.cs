namespace Capsule;

public interface ICapsuleSynchronizer
{
    Task EnqueueAwaitResult(Func<Task> impl);
    Task<TResult> EnqueueAwaitResult<TResult>(Func<Task<TResult>> impl);

    Task EnqueueAwaitReception(Func<Task> impl);

    Task EnqueueReturn(Func<Task> impl);
    
    T PassThrough<T>(Func<T> impl);

    /// <summary>
    /// Closes the underlying invocation queue to indicate that this synchronizer will not be used anymore. This allows
    /// the reader-side of the queue to be cleaned up.
    /// </summary>
    void Close();
}
