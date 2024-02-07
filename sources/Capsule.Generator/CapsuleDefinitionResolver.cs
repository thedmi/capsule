using Microsoft.CodeAnalysis;

namespace Capsule.Generator;

internal class CapsuleDefinitionResolver
{
    private const string InterfaceNamePropertyName = "InterfaceName";

    private const string InterfaceGenerationPropertyName = "InterfaceGeneration";

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

        var interfaceGeneration = DetermineInterfaceGeneration(capsuleAttribute) ??
                                  (singleInterface == null
                                      ? InterfaceGeneration.Enable
                                      : InterfaceGeneration.Disable);

        var generateInterface = interfaceGeneration == InterfaceGeneration.Enable ||
                                (interfaceGeneration == InterfaceGeneration.Auto && singleInterface == null);

        return new(interfaceName, generateInterface);
    }

    private static InterfaceGeneration? DetermineInterfaceGeneration(AttributeData capsuleAttribute) =>
        capsuleAttribute.GetProperty(InterfaceGenerationPropertyName)?.Value is int intVal
            ? (InterfaceGeneration)intVal
            : null;
}
