using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Capsule;

public class CapsuleInvocationLoopFactory : ICapsuleInvocationLoopFactory
{
    private readonly ILogger<CapsuleInvocationLoop> _logger;

    public CapsuleInvocationLoopFactory(ILogger<CapsuleInvocationLoop> logger)
    {
        _logger = logger;
    }

    public ICapsuleInvocationLoop Create(ChannelReader<Func<Task>> reader)
    {
        return new CapsuleInvocationLoop(reader, _logger);
    }
}
