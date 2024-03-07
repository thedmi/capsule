namespace Capsule;

/// <summary>
/// A thread-safe class that provides information from the invocation loop back to the synchronizer.
/// </summary>
public class InvocationLoopStatus : IInvocationLoopStatus
{
    private int _terminated;

    public bool Terminated => Interlocked.CompareExchange(ref _terminated, 1, 1) == 1;

    public void SetTerminated()
    {
        Interlocked.Exchange(ref _terminated, 1);
    }
}
