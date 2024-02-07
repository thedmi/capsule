using System.Threading.Channels;

namespace Capsule;

public class DefaultQueueFactory(int? capacity = null, BoundedChannelFullMode? fullMode = null) : ICapsuleQueueFactory
{
    private const int DefaultCapacity = 1023;

    public Channel<Func<Task>> CreateSynchronizerQueue()
    {
        return Channel.CreateBounded<Func<Task>>(
            new BoundedChannelOptions(capacity ?? DefaultCapacity)
            {
                SingleReader = true, SingleWriter = false, FullMode = fullMode ?? BoundedChannelFullMode.Wait
            });
    }
}
