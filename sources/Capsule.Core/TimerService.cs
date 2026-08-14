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
/// <param name="logger">The logger for this service.</param>
/// <param name="delayProvider">
/// Optional delay provider for testing purposes. If left null, the default delay implementation will be used, which
/// is based on Task.Delay but ensures timers don't fire early.
/// </param>
/// <param name="timeProvider">
/// Optional wall clock provider for testing purposes. If left null, <see cref="TimeProvider.System"/> is used. This
/// clock is only relevant for deadline-based timers.
/// </param>
internal class TimerService(
    ICapsuleSynchronizer synchronizer,
    ILogger<TimerService> logger,
    Func<TimeSpan, CancellationToken, Task>? delayProvider = null,
    TimeProvider? timeProvider = null
) : ITimerService
{
    /// <summary>
    /// The maximum length of an individual delay. Waits for a deadline are split into chunks of at most this length.
    /// This keeps individual delays within the range that Task.Delay supports and ensures the wall clock is re-checked
    /// regularly.
    /// </summary>
    private static readonly TimeSpan MaxSingleDelay = TimeSpan.FromDays(1);

    /// <summary>
    /// Upper bound for the backoff exponent, which limits the backoff to about one second.
    /// </summary>
    private const int MaxBackoffExponent = 10;

    private readonly Func<TimeSpan, CancellationToken, Task> _delayProvider = delayProvider ?? DelayAtLeastAsync;

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    // Internal for unit test access
    internal readonly TaskHandlingCollection<TimerReference> Timers = new(tr => tr.TimerTask);

    public int Count => Timers.Count;

    public TimerReference StartSingleShot(TimeSpan timeout, Func<Task> callback, string? discriminator = null)
    {
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be >= 0.");
        }

        return StartSingleShotCore(
            (timerTask, cts) => new TimerReference(timeout, timerTask, cts, discriminator),
            $"timeout {timeout}",
            ct => _delayProvider(timeout, ct),
            callback,
            discriminator
        );
    }

    public TimerReference StartSingleShot(DateTimeOffset deadline, Func<Task> callback, string? discriminator = null) =>
        StartSingleShotCore(
            (timerTask, cts) => new TimerReference(deadline, timerTask, cts, discriminator),
            $"deadline {deadline:O}",
            ct => DelayUntilAsync(deadline, ct),
            callback,
            discriminator
        );

    /// <summary>
    /// Registers and starts a timer. The two flavors of timers (timeout- and deadline-based) differ only in how they
    /// wait (<paramref name="waitAsync"/>) and in the reference they hand out
    /// (<paramref name="timerReferenceFactory"/>), everything else is common.
    /// </summary>
    private TimerReference StartSingleShotCore(
        Func<Task, CancellationTokenSource, TimerReference> timerReferenceFactory,
        string timerDescription,
        Func<CancellationToken, Task> waitAsync,
        Func<Task> callback,
        string? discriminator
    )
    {
        var cts = new CancellationTokenSource();

        var timerTask = EnqueueCallbackDelayedAsync();
        var timerReference = timerReferenceFactory(timerTask, cts);

        if (discriminator != null)
        {
            foreach (var existing in Timers.Where(t => t.Discriminator == discriminator))
            {
                logger.LogDebug(
                    "Existing timer with matching discriminator '{Discriminator}' found ({ExistingTimer}), cancelling existing timer...",
                    discriminator,
                    existing
                );

                existing.Cancel();
            }
        }

        Timers.Add(timerReference);

        logger.LogDebug(
            "Timer with {Timer} started & registered, {TimerCount} timers are now pending",
            timerDescription,
            Timers.Count
        );

        return timerReference;

        async Task EnqueueCallbackDelayedAsync()
        {
            var ct = cts.Token;

            try
            {
                await waitAsync(ct).ConfigureAwait(false);

                synchronizer.EnqueueReturn(async () =>
                {
                    if (ct.IsCancellationRequested)
                    {
                        logger.LogDebug(
                            "Timer with {Timer} was cancelled, the callback will not be executed",
                            timerDescription
                        );
                    }
                    else
                    {
                        await callback();
                    }
                });

                logger.LogDebug("Timer with {Timer} has fired and its callback been enqueued", timerDescription);
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
            var furtherDelay = delay - stopwatch.Elapsed + TimeSpan.FromMilliseconds(Math.Pow(2, i));
            await Task.Delay(furtherDelay, cancellationToken).ConfigureAwait(false);

            i++;
        }

        stopwatch.Stop();
    }

    /// <summary>
    /// The wall clock twin of <see cref="DelayAtLeastAsync"/>: Delays until the wall clock has reached
    /// <paramref name="deadline"/>. On every wake-up the clock is re-read, and if the deadline has not been reached
    /// yet (e.g. because the clock was stepped backwards), the remaining time is awaited as well.
    /// </summary>
    private async Task DelayUntilAsync(DateTimeOffset deadline, CancellationToken cancellationToken)
    {
        var i = 0;
        var isFirstDelay = true;

        do
        {
            var remaining = deadline - _timeProvider.GetUtcNow();

            TimeSpan delay;

            if (remaining <= TimeSpan.Zero)
            {
                // The deadline has passed already, delay once nonetheless so that arming a timer with an elapsed
                // deadline behaves exactly like arming one with a timeout of TimeSpan.Zero.
                delay = TimeSpan.Zero;
            }
            else
            {
                if (!isFirstDelay)
                {
                    // We woke up short of the deadline, so add a backoff to avoid busy-waiting in case the wall clock
                    // is stepped backwards repeatedly.
                    remaining += TimeSpan.FromMilliseconds(Math.Pow(2, Math.Min(i, MaxBackoffExponent)));
                    i++;
                }

                delay = remaining < MaxSingleDelay ? remaining : MaxSingleDelay;
            }

            await _delayProvider(delay, cancellationToken).ConfigureAwait(false);

            isFirstDelay = false;
        } while (_timeProvider.GetUtcNow() < deadline);
    }
}
