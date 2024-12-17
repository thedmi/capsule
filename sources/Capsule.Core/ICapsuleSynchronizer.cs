namespace Capsule;

#pragma warning disable VSTHRD200

/// <summary>
/// The synchronizer takes the invocations and enqueues them in the thread-safe queue.
/// </summary>
public interface ICapsuleSynchronizer
{
    /// <summary>
    /// Enqueues an invocation and awaits its completion. Exceptions are routed back to the caller.
    /// </summary>
    /// <param name="impl">The invocation</param>
    /// <param name="passThroughIfQueueClosed">
    /// If true and the queue has been terminated, the invocation will not be enqueued, but executed directly (same
    /// behavior as <see cref="PassThrough{T}"/>). This means thread-safety cannot be guaranteed in that case.
    /// Only use this if you're sure the method will not be called concurrently.
    /// </param>
    Task EnqueueAwaitResult(Func<Task> impl, bool passThroughIfQueueClosed = false);

    /// <summary>
    /// Enqueues an invocation and awaits its completion, returning the result of the invocation. Exceptions are routed
    /// back to the caller.
    /// </summary>
    /// <param name="impl">The invocation</param>
    /// <param name="passThroughIfQueueClosed">
    /// If true and the queue has been terminated, the invocation will not be enqueued, but executed directly (same
    /// behavior as <see cref="PassThrough{T}"/>). This means thread-safety cannot be guaranteed in that case.
    /// Only use this if you're sure the method will not be called concurrently.
    /// </param>
    Task<T> EnqueueAwaitResult<T>(Func<Task<T>> impl, bool passThroughIfQueueClosed = false);

    /// <summary>
    /// Enqueues an invocation and awaits reception of the invocation by the capsule implementation. Completion of the
    /// invocation is not awaited, so exceptions (if any) will be routed to the invocation loop.
    /// </summary>
    Task EnqueueAwaitReception(Func<Task> impl);

    /// <summary>
    /// Enqueues an invocation and returns immediately. Completion of the invocation is not awaited, so exceptions (if
    /// any) will be routed to the invocation loop.
    /// </summary>
    void EnqueueReturn(Func<Task> impl);

    /// <summary>
    /// Enqueues an invocation and returns immediately. Completion of the invocation is not awaited, so exceptions (if
    /// any) will be routed to the invocation loop.
    /// </summary>
    void EnqueueReturn(Action impl);

    /// <summary>
    /// Directly invokes the underlying implementation, bypassing any thread-safety mechanisms. This is only safe
    /// for properties that return immutable state.
    /// </summary>
    T PassThrough<T>(Func<T> impl);
}

#pragma warning restore VSTHRD200
