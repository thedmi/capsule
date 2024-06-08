using Microsoft.CodeAnalysis;

namespace Capsule.Generator;

internal static class NamedTypeSymbolExtensions
{
    internal static string NestedTypeQualifiedName(this INamedTypeSymbol symbol) =>
        symbol.ContainingType == null ? symbol.Name : $"{NestedTypeQualifiedName(symbol.ContainingType)}.{symbol.Name}";
}
