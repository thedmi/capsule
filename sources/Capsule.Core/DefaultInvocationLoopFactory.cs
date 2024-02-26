using System.Threading.Channels;

namespace Capsule;

public class DefaultInvocationLoopFactory(ICapsuleLogger<ICapsuleInvocationLoop> logger) : ICapsuleInvocationLoopFactory
{
    public ICapsuleInvocationLoop Create(ChannelReader<Func<Task>> reader, Type capsuleType)
    {
        return new InvocationLoop(reader, capsuleType, logger);
    }
}
