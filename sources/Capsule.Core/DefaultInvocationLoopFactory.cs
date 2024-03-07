using System.Threading.Channels;

using Microsoft.Extensions.Logging;

namespace Capsule;

public class DefaultInvocationLoopFactory(ILogger<ICapsuleInvocationLoop> logger) : ICapsuleInvocationLoopFactory
{
    public ICapsuleInvocationLoop Create(
        ChannelReader<Func<Task>> reader,
        InvocationLoopStatus status,
        Type capsuleType)
    {
        return new InvocationLoop(reader, status, capsuleType, logger);
    }
}
