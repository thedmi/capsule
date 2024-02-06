using System.Threading.Channels;

namespace Capsule;

public class DefaultInvocationLoopFactory(ICapsuleLogger<ICapsuleInvocationLoop> logger) : ICapsuleInvocationLoopFactory
{
    public ICapsuleInvocationLoop Create(ChannelReader<Func<Task>> reader)
    {
        return new InvocationLoop(reader, logger);
    }
}
