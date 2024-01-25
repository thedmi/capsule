using Microsoft.CodeAnalysis;

namespace Capsule.Generator;

internal static class AttributeDataExtensions
{
    internal static bool HasNameAndNamespace(this AttributeData attr, string attributeName, string attributeNamespace)
    {
        return attr.AttributeClass?.Name == attributeName &&
               attr.AttributeClass?.ContainingNamespace.ToDisplayString() == attributeNamespace;
    }

    internal static TypedConstant? GetProperty(this AttributeData attr, string propertyName)
    {
        var properties = attr.NamedArguments.Where(a => a.Key == propertyName).ToList();
        return properties.Any() ? properties.Single().Value : null;
    }
}
