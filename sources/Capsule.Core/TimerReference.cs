namespace Capsule;

/// <summary>
/// A reference to a timer that has been registered for delayed execution. The main purpose of this reference is to
/// allow cancellation of pending timers (<see cref="Cancel"/>).
/// </summary>
public class TimerReference
{
    private readonly CancellationTokenSource _cancellationTokenSource;

    internal TimerReference(
        TimeSpan timeout,
        Task timerTask,
        CancellationTokenSource cancellationTokenSource,
        string? discriminator = null
    )
        : this(timerTask, cancellationTokenSource, discriminator)
    {
        Timeout = timeout;
    }

    internal TimerReference(
        DateTimeOffset deadline,
        Task timerTask,
        CancellationTokenSource cancellationTokenSource,
        string? discriminator = null
    )
        : this(timerTask, cancellationTokenSource, discriminator)
    {
        Deadline = deadline;
    }

    private TimerReference(Task timerTask, CancellationTokenSource cancellationTokenSource, string? discriminator)
    {
        _cancellationTokenSource = cancellationTokenSource;
        TimerTask = timerTask;
        Discriminator = discriminator;
    }

    /// <summary>
    /// The task that represents the delay of the timer. This task completes when the timer's callback has been
    /// enqueued.
    /// </summary>
    internal Task TimerTask { get; }

    /// <summary>
    /// The timeout value that the timer uses, or null if the timer was started with a deadline (see
    /// <see cref="Deadline"/>).
    /// </summary>
    /// <remarks>
    /// Exactly one of <see cref="Timeout"/> and <see cref="Deadline"/> is non-null.
    /// </remarks>
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// The deadline that the timer uses, or null if the timer was started with a timeout (see <see cref="Timeout"/>).
    /// </summary>
    /// <remarks>
    /// Exactly one of <see cref="Timeout"/> and <see cref="Deadline"/> is non-null.
    /// </remarks>
    public DateTimeOffset? Deadline { get; }

    /// <summary>
    /// The discriminator that is used to identify timer duplicates. Duplicate detection is disabled if this is null.
    /// </summary>
    public string? Discriminator { get; }

    /// <summary>
    /// Cancel this timer. If the timer has already fired and the callback enqueued, this is a no-op.
    /// </summary>
    public void Cancel() => _cancellationTokenSource.Cancel();

    /// <summary>
    /// The cancellation token that is associated with this timer reference.
    /// </summary>
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public override string ToString() => Deadline.HasValue ? $"deadline {Deadline.Value:O}" : $"timeout {Timeout}";
}
