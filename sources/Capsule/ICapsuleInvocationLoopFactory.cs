using System.Threading.Channels;

namespace Capsule;

public interface ICapsuleInvocationLoopFactory
{
    ICapsuleInvocationLoop Create(ChannelReader<Func<Task>> reader);
}
