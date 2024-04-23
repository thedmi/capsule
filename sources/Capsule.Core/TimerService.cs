using Microsoft.Extensions.Logging;

namespace Capsule;

/// <summary>
/// A timer service for use in capsule implementations. Typically injected through <see cref="CapsuleFeature.ITimers"/>.
/// </summary>
/// <remarks>
/// Instances of this service are meant to be run in the context of exactly one capsule. The timer service itself is not
/// thread-safe, so it must be used only from within the owning capsule.
/// </remarks>
/// <param name="synchronizer">The synchronizer of the capsule this service is used with.</param>
/// <param name="delayProvider">Optional delay provider for testing. Should be left null in non-test contexts.</param>
internal class TimerService(
    ICapsuleSynchronizer synchronizer,
    ILogger<TimerService> logger,
    Func<TimeSpan, CancellationToken, Task>? delayProvider = null) : ITimerService
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delayProvider = delayProvider ?? DefaultDelayImpl;

    // Internal for unit test access
    internal readonly TaskCollection TimerTasks = [];

    // Internal for unit test access
    internal readonly List<TimerReference> Timers = [];

    public TimerReference StartSingleShot(TimeSpan timeout, Func<Task> callback)
    {
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be >= 0.");
        }
        
        var cts = new CancellationTokenSource();

        var timerTask = EnqueueCallbackDelayed();
        var timerReference = new TimerReference(timeout, timerTask, cts);
        
        TimerTasks.Add(timerTask);
        Timers.Add(timerReference);

        logger.LogDebug(
            "Timer with timeout {Timeout} started & registered, {TimerCount} timers are now pending",
            timeout,
            Timers.Count);

        return timerReference;

        async Task EnqueueCallbackDelayed()
        {
            try
            {
                await _delayProvider(timeout, cts.Token).ConfigureAwait(false);

                if (cts.IsCancellationRequested)
                {
                    logger.LogDebug(
                        "Timer with timeout {Timeout} was cancelled and the associated callback dropped",
                        timeout);
                }
                else
                {
                    await synchronizer.EnqueueReturn(callback).ConfigureAwait(false);
                    
                    logger.LogDebug(
                        "Timer with timeout {Timeout} has fired and its callback enqueued",
                        timeout);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Enqueue a second invocation for timer management. We can't do this directly, as the callback
                // (enqueued above) will likely not have completed yet. This is done in a finally block to ensure timers
                // are cleaned up even when they are cancelled.
                await synchronizer.EnqueueReturn(ClearElapsedTimersAsync).ConfigureAwait(false);
            }
        }
    }

    public void CancelAll()
    {
        foreach (var timerRef in Timers)
        {
            timerRef.Cancel();
        }
    }

    private async Task ClearElapsedTimersAsync()
    {
        var completed = TimerTasks.RemoveCompleted();
        await Task.WhenAll(completed).ConfigureAwait(false);

        Timers.RemoveAll(t => completed.Contains(t.TimerTask));
    }

    /// <summary>
    /// The default delay implementation uses Task.Delay but adds 1ms to the delay. The added delay ensures that timers
    /// never fire early (under normal circumstances).
    /// </summary>
    private static async Task DefaultDelayImpl(TimeSpan delay, CancellationToken cancellationToken) =>
        await Task.Delay(delay + TimeSpan.FromMilliseconds(1), cancellationToken).ConfigureAwait(false);
}
