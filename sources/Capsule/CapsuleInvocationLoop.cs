using System.Threading.Channels;

namespace Capsule;

public class CapsuleInvocationLoop : ICapsuleInvocationLoop
{
    private readonly ChannelReader<Func<Task>> _messageReader;

    public CapsuleInvocationLoop(ChannelReader<Func<Task>> messageReader)
    {
        _messageReader = messageReader;
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
            // to use a timeout to guarantee cancellation (which BackgroundService does).
            while (_messageReader.TryRead(out var f))
            {
                var task = f();
                await task;
            }
        }
    }
}
