namespace Capsule.Attribution;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public sealed class ExposeAttribute : Attribute
{
    public CapsuleSynchronization Synchronization { get; init; } = CapsuleSynchronization.AwaitCompletion;
}
