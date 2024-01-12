using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Capsule;

public class CapsuleHost : BackgroundService, ICapsuleHost
{
    private readonly ILogger<CapsuleHost> _logger;

    private readonly Channel<Task> _taskChannel;

    private readonly CancellationTokenSource _shutdownCts = new();
    
    private IList<Task> _invocationLoopTasks = new List<Task>();

    public CapsuleHost(ILogger<CapsuleHost> logger)
    {
        _logger = logger;
        
        _taskChannel = Channel.CreateUnbounded<Task>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _shutdownCts.Cancel());

        while (!stoppingToken.IsCancellationRequested)
        {
            var anyEventLoopTask = _invocationLoopTasks.Any() ? Task.WhenAny(_invocationLoopTasks) : Task.Delay(-1, stoppingToken);

            _logger.LogDebug("Capsule host awaiting event loop termination or new event loop task...");
            
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
        if (task.IsFaulted)
        {
            try
            {
                await task;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception during capsule event loop processing");
            }
        }
    }

    public async Task RegisterAsync(ICapsuleInvocationLoop capsuleInvocationLoop)
    {
        // Wrap the run method in a local function to force async processing
        async Task RunAsync()
        {
            await capsuleInvocationLoop.RunAsync(_shutdownCts.Token);
        }

        _taskChannel.Writer.TryWrite(RunAsync());
    }
}
