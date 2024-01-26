using System.Threading.Channels;

namespace Capsule;

public class CapsuleHost(ICapsuleLogger<CapsuleHost> logger) : ICapsuleHost
{
    private readonly Channel<Task> _taskChannel = Channel.CreateBounded<Task>(
        new BoundedChannelOptions(1023)
        {
            SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait
        });

    private readonly CancellationTokenSource _shutdownCts = new();
    
    private IList<Task> _invocationLoopTasks = new List<Task>();

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _shutdownCts.Cancel());

        while (!stoppingToken.IsCancellationRequested)
        {
            var anyEventLoopTask = _invocationLoopTasks.Any()
                ? Task.WhenAny(_invocationLoopTasks)
                : Task.Delay(-1, stoppingToken);

            logger.LogDebug("Capsule host awaiting event loop termination or new event loop task...");
            
            try
            {
                await Task.WhenAny(_taskChannel.Reader.WaitToReadAsync(stoppingToken).AsTask(), anyEventLoopTask);
            }
            catch (OperationCanceledException)
            {    
            }

            if (anyEventLoopTask.IsCompleted)
            {
                // Handle completed tasks and remove them from the list, keep the still running ones
                await SeparateCompletedTasksAsync();
            }
            
            while (_taskChannel.Reader.TryRead(out var newTask))
            {
                // Add newly received tasks one by one
                _invocationLoopTasks.Add(newTask);
            }
        }

        foreach (var task in _invocationLoopTasks)
        {
            await task;
        }
    }

    private async Task SeparateCompletedTasksAsync()
    {
        var completed = _invocationLoopTasks.Where(t => t.IsCompleted);
        var running = _invocationLoopTasks.Where(t => !t.IsCompleted);
                
        foreach (var completedTask in completed)
        {
            await HandleCompletedTaskAsync(completedTask);
        }

        _invocationLoopTasks = running.ToList();
    }

    private async Task HandleCompletedTaskAsync(Task task)
    {
        try
        {
            // The invocation loop task should always complete successfully, otherwise there is an implementation error
            // in CapsuleInvocationLoop.
            await task;
        }
        catch (Exception e)
        {
            throw new ArgumentException("Capsule invocation loop terminated abnormally", e);
        }
    }

    public async Task RegisterAsync(ICapsuleInvocationLoop capsuleInvocationLoop)
    {
        // Wrap the run method in a local function to force async processing
        async Task RunInvocationLoopAsync()
        {
            await capsuleInvocationLoop.RunAsync(_shutdownCts.Token);
        }

        _taskChannel.Writer.TryWrite(RunInvocationLoopAsync());
    }
}
