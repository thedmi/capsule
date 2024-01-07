using System.Collections.Immutable;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Senja;

public class CapsuleHost : BackgroundService, ICapsuleHost
{
    private readonly ILogger<CapsuleHost> _logger;
    
    private ImmutableList<Task> _eventLoopTasks = ImmutableList<Task>.Empty;

    private readonly CancellationTokenSource _shutdownCts = new();

    private readonly Channel<Task> _taskChannel;

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
            var anyEventLoopTask = _eventLoopTasks.Any() ? Task.WhenAny(_eventLoopTasks) : Task.Delay(-1, stoppingToken);

            _logger.LogDebug("Capsule host awaiting event loop termination or new event loop task...");
            await Task.WhenAny(_taskChannel.Reader.WaitToReadAsync(stoppingToken).AsTask(), anyEventLoopTask);

            if (anyEventLoopTask.IsCompleted)
            {
                var completed = _eventLoopTasks.Where(t => t.IsCompleted);
                var running = _eventLoopTasks.Where(t => !t.IsCompleted);
                
                foreach (var completedTask in completed)
                {
                    await HandleCompletedTaskAsync(completedTask);
                }

                _eventLoopTasks = running.ToImmutableList();
            }
            
            if (_taskChannel.Reader.TryRead(out var newTask))
            {
                _eventLoopTasks = _eventLoopTasks.Add(newTask);
            }
        }

        foreach (var task in _eventLoopTasks)
        {
            await task;
        }
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

    public async Task RegisterAsync(ICapsuleEventLoop capsuleEventLoop)
    {
        _taskChannel.Writer.TryWrite(capsuleEventLoop.RunAsync(_shutdownCts.Token));
    }
}
