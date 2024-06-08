using Microsoft.CodeAnalysis;

namespace Capsule.Generator;

internal record ExposeSpec(
    ISymbol MemberSymbol,
    Synchronization Synchronization,
    bool PassThroughIfQueueClosed);
