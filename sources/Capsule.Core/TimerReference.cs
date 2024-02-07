namespace Capsule;

/// <summary>
/// A reference to a timer that has been registered for delayed execution. The main purpose of this reference is to
/// allow cancellation of pending timers (<see cref="Cancel"/>).
/// </summary>
public class TimerReference(Task timerTask, CancellationTokenSource cancellationTokenSource)
{
    /// <summary>
    /// The task that represents the delay of the timer. This task completes when the timer's callback has been
    /// enqueued.
    /// </summary>
    public Task TimerTask { get; } = timerTask;

    /// <summary>
    /// Cancel this timer. If the timer has already fired and the callback enqueued, this is a no-op.
    /// </summary>
    public void Cancel() => cancellationTokenSource.Cancel();

    /// <summary>
    /// The cancellation token that is associated with this timer reference.
    /// </summary>
    public CancellationToken CancellationToken => cancellationTokenSource.Token;
}
