using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Capsule;

/// <summary>
/// Runs the invocation loops of all capsules that have been registered.
/// </summary>
/// <param name="logger">The logger</param>
/// <param name="faultShutdownTimeout">
/// How long the host waits for the remaining invocation loops to terminate when an invocation loop faulted, before the
/// fault is rethrown from <see cref="RunAsync"/>. Must be <see cref="Timeout.InfiniteTimeSpan"/> or between zero and
/// <see cref="int.MaxValue"/> milliseconds.
/// </param>
public class CapsuleHost(ILogger<CapsuleHost> logger, TimeSpan faultShutdownTimeout) : ICapsuleHost
{
    public static readonly TimeSpan DefaultFaultShutdownTimeout = TimeSpan.FromSeconds(5);

    public CapsuleHost(ILogger<CapsuleHost> logger)
        : this(logger, DefaultFaultShutdownTimeout) { }

    // Validated on construction, because an invalid timeout would otherwise make Task.Delay() throw while handling a
    // loop fault, hiding the actual fault
    private readonly TimeSpan _faultShutdownTimeout = ValidateFaultShutdownTimeout(faultShutdownTimeout);

    // The task channel contains Func<Task> instead of Task to ensure the tasks are started as part of RunAsync(), not before
    private readonly Channel<Func<Task>> _taskChannel = Channel.CreateBounded<Func<Task>>(
        new BoundedChannelOptions(1023)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        }
    );

    private readonly CancellationTokenSource _shutdownCts = new();

    private volatile bool _closed;

#pragma warning disable VSTHRD003
    private readonly TaskHandlingCollection<Task> _invocationLoopTasks = new(t => t);
#pragma warning restore VSTHRD003

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        using var stoppingRegistration = stoppingToken.Register(() => _shutdownCts.Cancel());

        try
        {
            await RunLoopsAsync(stoppingToken).ConfigureAwait(false);
        }
        catch
        {
            // The fault itself has already been logged by the invocation loop, and will be logged again by the caller
            logger.LogWarning("Capsule invocation loop faulted, shutting down remaining invocation loops...");

            // Shut down the remaining loops before the fault surfaces, so that callers (e.g. DisposeAsync() during DI
            // disposal) find terminated loops, the same as after a regular shutdown. The wait is bounded, because the
            // fault must surface even if a loop is blocked by a long-running invocation.
            ShutDown();
            await AwaitRemainingLoopsAsync().ConfigureAwait(false);

            throw;
        }

        // Shutting down, so await all tasks
        ShutDown();
        await Task.WhenAll(_invocationLoopTasks).ConfigureAwait(false);

        logger.LogDebug("Capsule host terminated");
    }

    /// <summary>
    /// Cancels all invocation loops and stops accepting registrations through the task channel. Loops that have
    /// already been registered are started with the cancelled shutdown token, so they terminate right away and release
    /// their callers.
    /// </summary>
    private void ShutDown()
    {
        // Cancel explicitly instead of relying on the stopping token registration, which may not have run yet.
        // Cancel() runs the cancellation callbacks synchronously, which is the same as the registration does.
#pragma warning disable VSTHRD103
        _shutdownCts.Cancel();
#pragma warning restore VSTHRD103

        _closed = true;
        _taskChannel.Writer.TryComplete();

        // Loops may have been registered after RunLoopsAsync() last drained the task channel, e.g. for capsules created
        // during shutdown. If they were never started, their InvocationLoopStatus would never become terminated, and
        // callers awaiting invocations on these capsules would hang forever. Starting them with the cancelled shutdown
        // token makes them terminate right away, which releases their callers. Draining after TryComplete() ensures
        // no registration is missed: earlier ones are still in the channel, later ones are handled by Register().
        // The started loops are also added to _invocationLoopTasks, so they are awaited like all other loops.
        StartRegisteredLoops();
    }

    private async Task RunLoopsAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var anyEventLoopTask = _invocationLoopTasks.Any()
                ? Task.WhenAny(_invocationLoopTasks) // these tasks are already connected to the shutdown token
                : Task.Delay(-1, stoppingToken);

            logger.LogDebug("Capsule host awaiting event loop termination or new event loop task...");

            try
            {
                await Task.WhenAny(_taskChannel.Reader.WaitToReadAsync(stoppingToken).AsTask(), anyEventLoopTask)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }

            if (anyEventLoopTask.IsCompleted)
            {
                // Handle completed tasks and remove them from the list, keep the still running ones
                await SeparateCompletedTasksAsync().ConfigureAwait(false);
            }

            StartRegisteredLoops();
        }
    }

    private void StartRegisteredLoops()
    {
        while (_taskChannel.Reader.TryRead(out var taskFactory))
        {
            // Add newly received tasks one by one
            _invocationLoopTasks.Add(taskFactory());
        }
    }

    private async Task AwaitRemainingLoopsAsync()
    {
#pragma warning disable VSTHRD003
        var remainingLoops = Task.WhenAll(_invocationLoopTasks);
        var completed = await Task.WhenAny(remainingLoops, Task.Delay(_faultShutdownTimeout)).ConfigureAwait(false);
#pragma warning restore VSTHRD003

        if (completed != remainingLoops)
        {
            logger.LogWarning(
                "Capsule invocation loops did not terminate within {Timeout}, not awaiting them any longer",
                _faultShutdownTimeout
            );
        }

        // Faults of the remaining loops have already been logged by the loops themselves, and the original fault
        // takes precedence, so only observe them here
        _ = remainingLoops.ContinueWith(
            t => t.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default
        );
    }

    private async Task SeparateCompletedTasksAsync()
    {
        var completed = _invocationLoopTasks.RemoveCompleted();

        foreach (var completedTask in completed)
        {
            await HandleCompletedTaskAsync(completedTask).ConfigureAwait(false);
        }
    }

    private static async Task HandleCompletedTaskAsync(Task task)
    {
        // Await task to unwrap exceptions, if any. The invocation loop task will throw in case a loop-owned invocation
        // produced an exception and CapsuleFailureMode.Abort has been specified. Consequently, the capsule host will
        // throw here and, if the capsule host is managed by a hosted service (the default), crash the app.
        // This failure behavior is intentional and is consistent with how uncaught exceptions in hosted services are
        // handled in .NET 6 and newer. The rationale behind this logic is that uncaught exceptions are a major issue
        // and should never go unnoticed.
#pragma warning disable VSTHRD003
        await task.ConfigureAwait(false);
#pragma warning restore VSTHRD003
    }

    private static TimeSpan ValidateFaultShutdownTimeout(TimeSpan timeout)
    {
        if (
            timeout != Timeout.InfiniteTimeSpan
            && (timeout < TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue)
        )
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout,
                "Fault shutdown timeout must be Timeout.InfiniteTimeSpan or between zero and int.MaxValue milliseconds."
            );
        }

        return timeout;
    }

    public void Register(ICapsuleInvocationLoop capsuleInvocationLoop)
    {
        // Wrap the run method in a local function to force async processing
        async Task RunInvocationLoopAsync()
        {
            await capsuleInvocationLoop.RunAsync(_shutdownCts.Token).ConfigureAwait(false);
        }

        var success = _taskChannel.Writer.TryWrite(RunInvocationLoopAsync);

        if (!success && _closed)
        {
            // The host has already terminated. Run the loop with the cancelled shutdown token, so it terminates right
            // away and invocations fail or pass through instead of waiting for a loop that never starts. The loop
            // intentionally does not process its queue, as nothing would supervise the invocations anymore.
            // Since RunAsync() has already returned, a loop fault cannot be rethrown, so at least log it. The default
            // invocation loop does not fault here, but custom loops might.
            _ = RunInvocationLoopAsync()
                .ContinueWith(
                    t =>
                        logger.LogError(
                            t.Exception?.GetBaseException(),
                            "Invocation loop registered after capsule host termination faulted"
                        ),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default
                );
            return;
        }

        if (!success)
        {
            throw new CapsuleEncapsulationException($"Unable to register invocation loop.");
        }
    }
}
