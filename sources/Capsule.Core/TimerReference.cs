namespace Capsule;

/// <summary>
/// A reference to a timer that has been registered for delayed execution. The main purpose of this reference is to
/// allow cancellation of pending timers (<see cref="Cancel"/>).
/// </summary>
public class TimerReference
{
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal TimerReference(TimeSpan timeout, Task timerTask, CancellationTokenSource cancellationTokenSource)
    {
        _cancellationTokenSource = cancellationTokenSource;
        TimerTask = timerTask;
        Timeout = timeout;
    }

    /// <summary>
    /// The task that represents the delay of the timer. This task completes when the timer's callback has been
    /// enqueued.
    /// </summary>
    internal Task TimerTask { get; }

    /// <summary>
    /// The timeout value that the timer uses.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Cancel this timer. If the timer has already fired and the callback enqueued, this is a no-op.
    /// </summary>
    public void Cancel() => _cancellationTokenSource.Cancel();

    /// <summary>
    /// The cancellation token that is associated with this timer reference.
    /// </summary>
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;
}
