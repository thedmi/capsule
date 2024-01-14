namespace Capsule.Attribution;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ExposeAttribute : Attribute 
{
    public CapsuleSynchronization Synchronization { get; init; } = CapsuleSynchronization.AwaitCompletion;
}
