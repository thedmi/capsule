using System.Threading.Channels;

namespace Capsule;

public class CapsuleInvocationLoopFactory(ICapsuleLogger<ICapsuleInvocationLoop> logger) : ICapsuleInvocationLoopFactory
{
    public ICapsuleInvocationLoop Create(ChannelReader<Func<Task>> reader)
    {
        return new CapsuleInvocationLoop(reader, logger);
    }
}
