namespace Capsule;

/// <summary>
/// A thread-safe class that provides information from the invocation loop back to the synchronizer.
/// </summary>
#pragma warning disable CS0618 // Kept until IInvocationLoopStatus is removed in v6
public class InvocationLoopStatus : IInvocationLoopStatus
#pragma warning restore CS0618
{
    private readonly TaskCompletionSource<object?> _termination = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public bool Terminated => _termination.Task.IsCompleted;

    /// <summary>
    /// A task that completes when the invocation loop has terminated. The loop does not process any invocations after
    /// that point.
    /// </summary>
    public Task Termination => _termination.Task;

    public void SetTerminated()
    {
        _termination.TrySetResult(null);
    }
}
