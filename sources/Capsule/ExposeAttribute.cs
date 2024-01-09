namespace Capsule;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ExposeAttribute : Attribute 
{
    public CapsuleSynchronization Synchronization { get; init; } = CapsuleSynchronization.AwaitCompletion;
}

public enum CapsuleSynchronization 
{
    AwaitCompletion,
    AwaitReception,
    AwaitEnqueueing,
    PassThrough
}
