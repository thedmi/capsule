using System.Diagnostics;

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
/// <param name="delayProvider">
/// Optional delay provider for testing purposes. If left null, the default delay implementation will be used, which
/// is based on Task.Delay but ensures timers don't fire early. 
/// </param>
internal class TimerService(
    ICapsuleSynchronizer synchronizer,
    ILogger<TimerService> logger,
    Func<TimeSpan, CancellationToken, Task>? delayProvider = null) : ITimerService
{
    private readonly Func<TimeSpan, CancellationToken, Task> _delayProvider = delayProvider ?? DelayAtLeastAsync;

    // Internal for unit test access
    internal readonly TaskHandlingCollection<TimerReference> Timers = new(tr => tr.TimerTask);

    public int Count => Timers.Count;
    
    public TimerReference StartSingleShot(TimeSpan timeout, Func<Task> callback)
    {
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be >= 0.");
        }
        
        var cts = new CancellationTokenSource();

        var timerTask = EnqueueCallbackDelayed();
        var timerReference = new TimerReference(timeout, timerTask, cts);
        
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
                    synchronizer.EnqueueReturn(callback);
                    
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
                synchronizer.EnqueueReturn(ClearElapsedTimersAsync);
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
        var completed = Timers.RemoveCompleted();
        await Task.WhenAll(completed.Select(tr => tr.TimerTask)).ConfigureAwait(false);
    }

    private static async Task DelayAtLeastAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

        var i = 0;
        
        while (stopwatch.Elapsed < delay)
        {
            var furtherDelay = delay - stopwatch.Elapsed + TimeSpan.FromMilliseconds(2 ^ i);
            await Task.Delay(furtherDelay, cancellationToken).ConfigureAwait(false);

            i++;
        }
        
        stopwatch.Stop();
    }
}
