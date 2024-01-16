using System.Threading.Channels;

namespace Capsule;

public class CapsuleInvocationLoopFactory : ICapsuleInvocationLoopFactory
{
    private readonly ICapsuleLogger<CapsuleInvocationLoop> _logger;

    public CapsuleInvocationLoopFactory(ICapsuleLogger<CapsuleInvocationLoop> logger)
    {
        _logger = logger;
    }

    public ICapsuleInvocationLoop Create(ChannelReader<Func<Task>> reader)
    {
        return new CapsuleInvocationLoop(reader, _logger);
    }
}
