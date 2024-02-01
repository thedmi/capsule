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
internal class CapsuleTimerService(
    ICapsuleSynchronizer synchronizer,
    Func<TimeSpan, CancellationToken, Task>? delayProvider = null) : ICapsuleTimerService
{
    // Internal for unit test access
    internal readonly TaskCollection TimerTasks = [];

    private readonly Func<TimeSpan, CancellationToken, Task> _delayProvider = delayProvider ?? Task.Delay;

    public TimerReference StartNew(TimeSpan timeout, Func<Task> callback)
    {
        var cts = new CancellationTokenSource();

        var timerTask = EnqueueCallbackDelayed();
        TimerTasks.Add(timerTask);

        return new TimerReference(timerTask, cts);

        async Task EnqueueCallbackDelayed()
        {
            try
            {
                await _delayProvider(timeout, cts.Token);

                if (!cts.IsCancellationRequested)
                {
                    // We're in a "free floating" task, so awaiting the result doesn't make sense here. Instead,
                    // we ensure exceptions are handled by the invocation loop.
                    await synchronizer.EnqueueReturn(callback);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Enqueue a second invocation for timer management. We can't do this directly, as the callback
                // (enqueued above) will likely not have completed yet. This is done in a finally block to ensure timers
                // are cleaned up even when they are cancelled.
                await synchronizer.EnqueueReturn(ClearElapsedTimersAsync);
            }
        }
    }

    private async Task ClearElapsedTimersAsync()
    {
        var completed = TimerTasks.RemoveCompleted();
        await Task.WhenAll(completed);
    }
}
