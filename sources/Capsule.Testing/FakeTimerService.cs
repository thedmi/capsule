namespace Capsule.Testing;

/// <summary>
/// An <see cref="ITimerService"/> implementation that executes timers on request instead of according to their timeout.
/// This is useful for unit testing capsules.
/// </summary>
/// <remarks>
/// To simulate timer expiration and execute their callbacks, use <see cref="ExecuteAsync"/> or
/// <see cref="ExecuteAllAsync"/>. Timers will not fire by themselves.
/// </remarks>
public class FakeTimerService : ITimerService
{
    private readonly Dictionary<TimerReference, Func<Task>> _callbacks = new();

    public IReadOnlyList<TimerReference> Timers => _callbacks.Keys.ToList();
    
    public TimerReference StartSingleShot(TimeSpan timeout, Func<Task> callback)
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var timerReference = new TimerReference(Task.CompletedTask, cancellationTokenSource);

        _callbacks.Add(timerReference, callback);
        
        // Remove timers when they are cancelled. The same would happen with the real timer service.
        cancellationTokenSource.Token.Register(() => _callbacks.Remove(timerReference));
        
        return timerReference;
    }

    public void CancelAll()
    {
        _callbacks.Clear();
    }

    /// <summary>
    /// Simulates timer expiration for the timer represented by <paramref name="timerReference"/> and invokes its
    /// callback.
    /// </summary>
    /// <remarks>
    /// With this being a fake timer service, callbacks will be invoked directly and thus thread-safety is not
    /// guaranteed.
    /// </remarks>
    public async Task ExecuteAsync(TimerReference timerReference)
    {
        await _callbacks[timerReference]();
        _callbacks.Remove(timerReference);
    }

    /// <summary>
    /// Simulates timer expiration for all pending timers and invokes their callback.
    /// </summary>
    /// <remarks>
    /// With this being a fake timer service, callbacks will be invoked directly and thus thread-safety is not
    /// guaranteed.
    /// </remarks>
    public async Task ExecuteAllAsync()
    {
        foreach (var timerReference in _callbacks.Keys.ToList())
        {
            await ExecuteAsync(timerReference);
        }
    }
}
