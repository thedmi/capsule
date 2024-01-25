using Microsoft.CodeAnalysis;

namespace Capsule.Generator;

internal class CapsuleDefinitionResolver
{
    private const string InterfaceNamePropertyName = "InterfaceName";

    private const string GenerateInterfacePropertyName = "GenerateInterface";

    public CapsuleDefinition GetCapsuleDefinition(INamedTypeSymbol classSymbol)
    {
        var capsuleAttribute = classSymbol.GetAttributes()
            .Single(a => a.HasNameAndNamespace(SymbolNames.CapsuleAttributeName, SymbolNames.AttributionNamespace));

        var interfaceName = capsuleAttribute.GetProperty(InterfaceNamePropertyName)?.Value as string ??
                            "I" + classSymbol.Name;

        var generateInterface = capsuleAttribute.GetProperty(GenerateInterfacePropertyName)?.Value as bool? ?? true;

        return new(interfaceName, generateInterface);
    }
}
