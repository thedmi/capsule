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

        var relevantInterfaces = classSymbol.Interfaces.Where(
            i => !i.GetAttributes()
                .Any(
                    a => a.HasNameAndNamespace(
                        SymbolNames.CapsuleIgnoreAttributeName,
                        SymbolNames.AttributionNamespace)));
        
        var singleInterface = relevantInterfaces.SingleOrDefault();

        var interfaceName = capsuleAttribute.GetProperty(InterfaceNamePropertyName)?.Value as string ??
                            singleInterface?.Name ?? "I" + classSymbol.Name;

        var generateInterface = capsuleAttribute.GetProperty(GenerateInterfacePropertyName)?.Value as bool? ??
                                singleInterface == null;

        return new(interfaceName, generateInterface);
    }
}
