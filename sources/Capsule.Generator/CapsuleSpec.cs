using Microsoft.CodeAnalysis;

namespace Capsule.Generator;

internal record CapsuleSpec(INamedTypeSymbol CapsuleClass, CapsuleSpec.ICapsuleInterface Interface)
{
    internal interface ICapsuleInterface;

    internal record ProvidedInterface(string Name) : ICapsuleInterface;

    internal record GeneratedInterface(string Name) : ICapsuleInterface;

    internal record ResolvedInterface(INamedTypeSymbol Symbol) : ICapsuleInterface;
}
