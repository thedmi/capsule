namespace Capsule;

public interface ICapsuleSynchronizer
{
    /// <summary>
    /// Enqueues an invocation and awaits its completion. Exceptions are routed back to the caller.
    /// </summary>
    Task EnqueueAwaitResult(Func<Task> impl);
    
    /// <summary>
    /// Enqueues an invocation and awaits its completion, returning the result of the invocation. Exceptions are routed
    /// back to the caller.
    /// </summary>
    Task<T> EnqueueAwaitResult<T>(Func<Task<T>> impl);

    /// <summary>
    /// Enqueues an invocation and awaits reception of the invocation by the capsule implementation. Completion of the
    /// invocation is not awaited, so exceptions (if any) will be routed to the invocation loop. 
    /// </summary>
    Task EnqueueAwaitReception(Func<Task> impl);

    /// <summary>
    /// Enqueues an invocation and returns immediately. Completion of the invocation is not awaited, so exceptions (if
    /// any) will be routed to the invocation loop. 
    /// </summary>
    Task EnqueueReturn(Func<Task> impl);
    
    /// <summary>
    /// Directly invokes the underlying implementation, bypassing any thread-safety mechanisms. This is only safe
    /// for properties that return immutable state.
    /// </summary>
    T PassThrough<T>(Func<T> impl);
}
