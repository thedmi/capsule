namespace Capsule.Generator;

internal enum Synchronization
{
    // Enum literals must match method names in CapsuleSynchronizer, and int values
    // must match those in CapsulSynchronization.
    
    EnqueueAwaitResult,
    EnqueueAwaitReception,
    EnqueueReturn,
    PassThrough
}
