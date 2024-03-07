using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace Capsule;

public class CapsuleHost(ILogger<CapsuleHost> logger) : ICapsuleHost
{
    private readonly Channel<Task> _taskChannel = Channel.CreateBounded<Task>(
        new BoundedChannelOptions(1023)
        {
            SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait
        });

    private readonly CancellationTokenSource _shutdownCts = new();

    private readonly TaskCollection _invocationLoopTasks = [];

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
            
            while (_taskChannel.Reader.TryRead(out var newTask))
            {
                // Add newly received tasks one by one
                _invocationLoopTasks.Add(newTask);
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
        try
        {
            // The invocation loop task should always complete successfully, otherwise there is an implementation error
            // in CapsuleInvocationLoop.
            await task.ConfigureAwait(false);
        }
        catch (Exception e)
        {
            throw new ArgumentException("Capsule invocation loop terminated abnormally", e);
        }
    }

    public void Register(ICapsuleInvocationLoop capsuleInvocationLoop)
    {
        // Wrap the run method in a local function to force async processing
        async Task RunInvocationLoopAsync()
        {
            await capsuleInvocationLoop.RunAsync(_shutdownCts.Token).ConfigureAwait(false);
        }

        var success = _taskChannel.Writer.TryWrite(RunInvocationLoopAsync());

        if (!success)
        {
            throw new CapsuleEncapsulationException($"Unable to register invocation loop.");
        }
    }
}
