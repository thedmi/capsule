using System.Threading.Channels;

namespace Capsule;

internal class CapsuleInvocationLoop(ChannelReader<Func<Task>> reader, ICapsuleLogger<ICapsuleInvocationLoop> logger)
    : ICapsuleInvocationLoop
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await reader.WaitToReadAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }

            // Try to consume all outstanding invocations even if we've been cancelled. We expect the caller
            // to use a timeout to guarantee cancellation even if one of the functions take a long time
            // (BackgroundService does employ such a timeout).
            while (reader.TryRead(out var f))
            {
                try
                {
                    var task = f();
                    await task;
                }
                catch (OperationCanceledException e)
                {
                    logger.LogWarning(e, "An invocation loop task was cancelled.");
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Exception during capsule invocation loop processing.");
                }
            }
        }
    }
}
