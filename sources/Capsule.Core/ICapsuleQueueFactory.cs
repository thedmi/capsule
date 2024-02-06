using System.Threading.Channels;

namespace Capsule;

public interface ICapsuleQueueFactory
{
    /// <summary>
    /// Creates an invocation queue that will be used to pass invocations from the synchronizer to the
    /// invocation loop.
    /// </summary>
    Channel<Func<Task>> CreateSynchronizerQueue();
}
