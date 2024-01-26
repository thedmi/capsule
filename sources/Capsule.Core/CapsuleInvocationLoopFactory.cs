using System.Threading.Channels;

namespace Capsule;

public class CapsuleInvocationLoopFactory : ICapsuleInvocationLoopFactory
{
    private readonly ICapsuleLogger<ICapsuleInvocationLoop> _logger;

    public CapsuleInvocationLoopFactory(ICapsuleLogger<ICapsuleInvocationLoop> logger)
    {
        _logger = logger;
    }

    public ICapsuleInvocationLoop Create(ChannelReader<Func<Task>> reader)
    {
        return new CapsuleInvocationLoop(reader, _logger);
    }
}
