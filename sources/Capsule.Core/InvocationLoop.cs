using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace Capsule;

internal class InvocationLoop(
    ChannelReader<Func<Task>> reader,
    InvocationLoopStatus status,
    Type capsuleType,
    ILogger<ICapsuleInvocationLoop> logger)
    : ICapsuleInvocationLoop
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Starting invocation loop for capsule {CapsuleType}...", capsuleType.FullName);

        try
        {
            await RunLoopAsync(cancellationToken);
        }
        finally
        {
            status.SetTerminated();
            logger.LogDebug("Invocation loop for capsule {CapsuleType} terminated", capsuleType.FullName);
        }
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var channelStillOpen = await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false);

                // The synchronizer will close the channel when it is finalized. This avoids keeping invocation loop
                // instances around for capsules that already have been GCed.
                if (!channelStillOpen)
                {
                    logger.LogDebug("Invocation queue has been closed, terminating invocation loop...");
                    return;
                }
            }
            catch (OperationCanceledException)
            {
            }

            // Try to consume all outstanding invocations even if we've been cancelled. We expect the caller
            // to use a timeout to guarantee cancellation even if one of the functions take a long time
            // (BackgroundService does employ such a timeout).
            while (reader.TryRead(out var f))
            {
                await ExecuteAsync(f).ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteAsync(Func<Task> invocation)
    {
        try
        {
            await invocation().ConfigureAwait(false);
        }
        catch (OperationCanceledException e)
        {
            logger.LogWarning(e, "A loop-owned invocation was cancelled.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Exception during capsule invocation loop processing.");
        }
    }
}
