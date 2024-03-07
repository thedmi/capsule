namespace Capsule.Attribution;

public enum CapsuleSynchronization 
{
    /// <summary>
    /// Await completion of the invocation and pass return values (or exceptions, should there be any) back to the
    /// caller.
    /// </summary>
    AwaitCompletion,
    
    /// <summary>
    /// Await reception of the invocation by the invocation loop. Returns when the invocation is dequeued by the
    /// invocation loop. Exceptions thrown by the invocation end up in the invocation loop.
    /// </summary>
    AwaitReception,
    
    /// <summary>
    /// Enqueue the invocation and return immediately. Exceptions thrown by the invocation end up in the invocation
    /// loop.
    /// </summary>
    AwaitEnqueueing,
    
    /// <summary>
    /// Directly invoke the capsule implementation, thus bypassing the thread-safe queue. This synchronization mode
    /// is only safe on immutable properties.
    /// </summary>
    PassThrough,
    
    /// <summary>
    /// Synchronization mode depends on the invocation queue status. If the queue is live and ready to receive
    /// invocations, <see cref="AwaitCompletion"/> is used. Otherwise, <see cref="PassThrough"/> is used. In the latter
    /// case, make sure the method is not used concurrently, as there are no thread-safety guarantees with
    /// <see cref="PassThrough"/>.
    /// </summary>
    AwaitCompletionOrPassThroughIfQueueClosed
}
