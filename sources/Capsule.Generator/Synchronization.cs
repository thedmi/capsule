namespace Capsule.Generator;

internal enum Synchronization
{
    // Enum literals must match method names in CapsuleSynchronizer, and int values
    // must match those in CapsuleSynchronization.

    EnqueueAwaitResult,
    EnqueueAwaitReception,
    EnqueueReturn,
    PassThrough,
    EnqueueAwaitResultOrPassThroughIfQueueClosed,
}
