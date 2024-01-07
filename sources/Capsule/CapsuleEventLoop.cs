using System.Threading.Channels;

namespace Capsule;

public class CapsuleEventLoop : ICapsuleEventLoop
{
    private readonly ChannelReader<Func<Task>> _messageReader;

    public CapsuleEventLoop(ChannelReader<Func<Task>> messageReader)
    {
        _messageReader = messageReader;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _messageReader.WaitToReadAsync(cancellationToken);

            if (!cancellationToken.IsCancellationRequested)
            {
                _messageReader.TryRead(out var f);
                await f!();
            }
        }
    }
}
