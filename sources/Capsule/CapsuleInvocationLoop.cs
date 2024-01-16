using System.Threading.Channels;
namespace Capsule;

public class CapsuleInvocationLoop : ICapsuleInvocationLoop
{
    private readonly ChannelReader<Func<Task>> _reader;
    private readonly ICapsuleLogger<CapsuleInvocationLoop> _logger;

    public CapsuleInvocationLoop(ChannelReader<Func<Task>> reader, ICapsuleLogger<CapsuleInvocationLoop> logger)
    {
        _reader = reader;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _reader.WaitToReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }

            // Try to consume all outstanding invocations even if we've been cancelled. We expect the caller
            // to use a timeout to guarantee cancellation even if one of the functions take a long time
            // (BackgroundService does employ such a timeout).
            while (_reader.TryRead(out var f))
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
