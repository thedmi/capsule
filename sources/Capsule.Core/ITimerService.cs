namespace Capsule;

/// <summary>
/// Service that provides timers for capsule implementations. Enable timers by implementing
/// <see cref="CapsuleFeature.ITimers"/> in the capsule implementation. 
/// </summary>
/// <remarks>This service is not meant to be used outside of capsule implementations.</remarks>
public interface ITimerService
{
    /// <summary>
    /// Register a new timer and start it immediately. The timer will fire after <paramref name="timeout"/> has expired.
    /// Then, the <paramref name="callback"/> will be enqueued for execution. The callback will run in the context of
    /// the capsule to guarantee thread-safety in timer callbacks.
    /// </summary>
    /// <returns>A reference to the timer that allows cancelling the timer before it expires.</returns>
    TimerReference StartNew(TimeSpan timeout, Func<Task> callback);
}
