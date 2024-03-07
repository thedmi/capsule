using Microsoft.CodeAnalysis;

namespace Capsule.Generator;

internal record ExposeDefinition(ISymbol Symbol, Synchronization Synchronization, bool PassThroughIfQueueClosed);
