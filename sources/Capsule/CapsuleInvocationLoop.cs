using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Capsule;

public class CapsuleInvocationLoop : ICapsuleInvocationLoop
{
    private readonly ChannelReader<Func<Task>> _messageReader;
    private readonly ILogger<CapsuleInvocationLoop> _logger;

    public CapsuleInvocationLoop(ChannelReader<Func<Task>> messageReader, ILogger<CapsuleInvocationLoop> logger)
    {
        _messageReader = messageReader;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _messageReader.WaitToReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }

            // Try to consume all outstanding invocations even if a shutdown is in progress. We expect the caller
            // to use a timeout to guarantee cancellation even if one of the functions take a long time (which
            // BackgroundService does).
            while (_messageReader.TryRead(out var f))
            {
                try
                {
                    var task = f();
                    await task;
                }
                catch (OperationCanceledException e)
                {
                    _logger.LogWarning(e, "An invocation loop task was cancelled.");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception during capsule invocation loop processing.");
                }
            }
        }
    }
}
