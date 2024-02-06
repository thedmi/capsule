using System.Threading.Channels;

namespace Capsule;

public class CapsuleQueueFactory : ICapsuleQueueFactory
{
    public Channel<Func<Task>> CreateSynchronizerQueue() =>
        Channel.CreateBounded<Func<Task>>(
            new BoundedChannelOptions(1023)
            {
                SingleReader = true, SingleWriter = false, FullMode = BoundedChannelFullMode.Wait
            });
}
