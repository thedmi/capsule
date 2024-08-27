using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace Capsule;

public class CapsuleHost(ILogger<CapsuleHost> logger) : ICapsuleHost
{
    // The task channel contains Func<Task> instead of Task to ensure the tasks are started as part of RunAsync(), not before
    private readonly Channel<Func<Task>> _taskChannel = Channel.CreateBounded<Func<Task>>(
        new BoundedChannelOptions(1023)
        {
            SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait
        });

    private readonly CancellationTokenSource _shutdownCts = new();

    private readonly TaskHandlingCollection<Task> _invocationLoopTasks = new(t => t);

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _shutdownCts.Cancel());

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
            
            while (_taskChannel.Reader.TryRead(out var taskFactory))
            {
                // Add newly received tasks one by one
                _invocationLoopTasks.Add(taskFactory());
            }
        }

        // Shutting down, so await all tasks
        await Task.WhenAll(_invocationLoopTasks).ConfigureAwait(false);
        
        logger.LogDebug("Capsule host terminated");
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
        await task.ConfigureAwait(false);
    }

    public void Register(ICapsuleInvocationLoop capsuleInvocationLoop)
    {
        // Wrap the run method in a local function to force async processing
        async Task RunInvocationLoopAsync()
        {
            await capsuleInvocationLoop.RunAsync(_shutdownCts.Token).ConfigureAwait(false);
        }

        var success = _taskChannel.Writer.TryWrite(RunInvocationLoopAsync);

        if (!success)
        {
            throw new CapsuleEncapsulationException($"Unable to register invocation loop.");
        }
    }
}
