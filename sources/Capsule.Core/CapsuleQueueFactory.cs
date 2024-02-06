using System.Threading.Channels;

namespace Capsule;

public class CapsuleQueueFactory(int? capacity = null, BoundedChannelFullMode? fullMode = null) : ICapsuleQueueFactory
{
    private const int DefaultCapacity = 1023;

    public Channel<Func<Task>> CreateSynchronizerQueue() =>
        Channel.CreateBounded<Func<Task>>(
            new BoundedChannelOptions(capacity ?? DefaultCapacity)
            {
                SingleReader = true, SingleWriter = false, FullMode = fullMode ?? BoundedChannelFullMode.Wait
            });
}
