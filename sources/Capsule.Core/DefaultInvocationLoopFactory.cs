using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Capsule;

/// <summary>
/// A factory for invocation loops. Used when encapsulating capsules.
/// </summary>
/// <param name="loggerFactory">The logger factory</param>
/// <param name="failureMode">
/// How the invocation loop shall treat uncaught exceptions from loop-owned invocations
/// </param>
public class DefaultInvocationLoopFactory(
    ILoggerFactory loggerFactory,
    CapsuleFailureMode failureMode = CapsuleFailureMode.Continue
) : ICapsuleInvocationLoopFactory
{
    public ICapsuleInvocationLoop Create(
        ChannelReader<Func<Task>> reader,
        InvocationLoopStatus status,
        Type capsuleType
    )
    {
        return new InvocationLoop(
            reader,
            status,
            capsuleType,
            loggerFactory.CreateLogger<InvocationLoop>(),
            failureMode
        );
    }
}
