using Microsoft.CodeAnalysis;

namespace Capsule.Generator;

internal class CapsuleSpecResolver
{
    private const string InterfaceNamePropertyName = "InterfaceName";

    private const string InterfaceGenerationPropertyName = "InterfaceGeneration";

    public CapsuleSpec GetCapsuleDefinition(INamedTypeSymbol classSymbol)
    {
        var capsuleAttribute = classSymbol
            .GetAttributes()
            .Single(a => a.HasNameAndNamespace(SymbolNames.CapsuleAttributeName, SymbolNames.AttributionNamespace));

        var relevantInterfaces = classSymbol.Interfaces.Where(i =>
            !i.GetAttributes()
                .Any(a =>
                    a.HasNameAndNamespace(SymbolNames.CapsuleIgnoreAttributeName, SymbolNames.AttributionNamespace)
                )
        );

        var firstRelevantInterface = relevantInterfaces.FirstOrDefault();

        var interfaceGeneration =
            DetermineInterfaceGeneration(capsuleAttribute)
            ?? (firstRelevantInterface == null ? InterfaceGeneration.Enable : InterfaceGeneration.Disable);

        var interfaceName =
            capsuleAttribute.GetProperty(InterfaceNamePropertyName)?.Value as string
            ?? firstRelevantInterface?.Name
            ?? "I" + classSymbol.Name;

        var generateInterface =
            interfaceGeneration == InterfaceGeneration.Enable
            || (interfaceGeneration == InterfaceGeneration.Auto && firstRelevantInterface == null);

        return new(
            classSymbol,
            generateInterface ? new CapsuleSpec.GeneratedInterface(interfaceName)
                : firstRelevantInterface == null ? new CapsuleSpec.ProvidedInterface(interfaceName)
                : new CapsuleSpec.ResolvedInterface(firstRelevantInterface)
        );
    }

    private static InterfaceGeneration? DetermineInterfaceGeneration(AttributeData capsuleAttribute) =>
        capsuleAttribute.GetProperty(InterfaceGenerationPropertyName)?.Value is int intVal
            ? (InterfaceGeneration)intVal
            : null;
}
