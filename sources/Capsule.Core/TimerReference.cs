namespace Capsule;

/// <summary>
/// A reference to a timer that has been registered for delayed execution. The main purpose of this reference is to
/// allow cancellation of pending timers (see <paramref name="CancellationTokenSource"/>).
/// </summary>
public record TimerReference(Task TimerTask, CancellationTokenSource CancellationTokenSource);
