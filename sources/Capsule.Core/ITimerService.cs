namespace Capsule;

/// <summary>
/// Service that provides timers for capsule implementations. Enable timers by implementing
/// <see cref="CapsuleFeature.ITimers"/> in the capsule implementation. 
/// </summary>
/// <remarks>
/// This service is specifically designed for use in Capsule implementations and is not meant to be used outside of them.
/// </remarks>
public interface ITimerService
{
    /// <summary>
    /// Register a new timer and start it immediately. The timer will fire after <paramref name="timeout"/> has expired.
    /// Then, the <paramref name="callback"/> will be enqueued for execution. The callback will run in the context of
    /// the capsule to guarantee thread-safety in timer callbacks.
    /// </summary>
    /// <remarks>
    /// In cases where only one timer of a specific kind is needed (e.g. a retry timer), set a discriminator. The timer
    /// service will then cancel any existing timer with the same discriminator. This ensures that only one such timer
    /// exists at any time and timers don't accumulate.
    /// </remarks>
    /// <param name="timeout">The time after which <paramref name="callback"/> will be called</param>
    /// <param name="callback">The callback to execute when the timer expires</param>
    /// <param name="discriminator">
    /// If set, the timer service will cancel pre-existing timers with the same discriminator
    /// </param>
    /// <returns>A reference to the timer that allows cancelling the timer before it expires.</returns>
    TimerReference StartSingleShot(TimeSpan timeout, Func<Task> callback, string? discriminator = null);

    /// <summary>
    /// Cancel all pending timers. 
    /// </summary>
    /// <remarks>
    /// Callbacks that have already been enqueued because their timer has elapsed will remain enqueued.
    /// </remarks>
    void CancelAll();
    
    /// <summary>
    /// The number of timers that this service currently manages. This may include timers that just expired.
    /// </summary>
    int Count { get; }
}
