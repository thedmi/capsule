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
    /// <para>
    /// This overload lives in the <i>monotonic</i> clock domain: The timer is guaranteed not to fire before
    /// <paramref name="timeout"/> has elapsed on a monotonic clock, and it is unaffected by wall clock adjustments
    /// (NTP steps, manual clock changes, time zone or DST transitions). Use it to express "call back after N".
    /// To schedule against a wall clock instant instead, use
    /// <see cref="StartSingleShot(DateTimeOffset,Func{Task},string)"/>.
    /// </para>
    /// <para>
    /// In cases where only one timer of a specific kind is needed (e.g. a retry timer), set a discriminator. The timer
    /// service will then cancel any existing timer with the same discriminator. This ensures that only one such timer
    /// exists at any time and timers don't accumulate.
    /// </para>
    /// </remarks>
    /// <param name="timeout">The time after which <paramref name="callback"/> will be called</param>
    /// <param name="callback">The callback to execute when the timer expires</param>
    /// <param name="discriminator">
    /// If set, the timer service will cancel pre-existing timers with the same discriminator
    /// </param>
    /// <returns>A reference to the timer that allows cancelling the timer before it expires.</returns>
    TimerReference StartSingleShot(TimeSpan timeout, Func<Task> callback, string? discriminator = null);

    /// <summary>
    /// Register a new timer and start it immediately. The timer will fire when the wall clock has reached
    /// <paramref name="deadline"/>. Then, the <paramref name="callback"/> will be enqueued for execution. The callback
    /// will run in the context of the capsule to guarantee thread-safety in timer callbacks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This overload lives in the <i>wall clock</i> domain: The timer is guaranteed not to fire before the wall clock
    /// has reached <paramref name="deadline"/>, even if the clock is stepped while the timer is pending. Use it to
    /// express "call back at instant T". To schedule a relative delay instead, use
    /// <see cref="StartSingleShot(TimeSpan,Func{Task},string)"/>.
    /// </para>
    /// <para>
    /// The guarantee is <i>never early</i> only. A backward clock step costs one additional short wait. A forward clock
    /// step means the deadline has already passed, so the timer is late and fires as soon as it wakes up. Deadlines
    /// that lie in the past when the timer is started fire immediately, just like a timeout of
    /// <see cref="TimeSpan.Zero"/> does.
    /// </para>
    /// <para>
    /// In cases where only one timer of a specific kind is needed (e.g. a retry timer), set a discriminator. The timer
    /// service will then cancel any existing timer with the same discriminator. This ensures that only one such timer
    /// exists at any time and timers don't accumulate.
    /// </para>
    /// </remarks>
    /// <param name="deadline">The wall clock instant at which <paramref name="callback"/> will be called</param>
    /// <param name="callback">The callback to execute when the timer expires</param>
    /// <param name="discriminator">
    /// If set, the timer service will cancel pre-existing timers with the same discriminator
    /// </param>
    /// <returns>A reference to the timer that allows cancelling the timer before it expires.</returns>
    TimerReference StartSingleShot(DateTimeOffset deadline, Func<Task> callback, string? discriminator = null);

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
