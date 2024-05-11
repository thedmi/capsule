using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Capsule.Generator;

[Generator]
public class CapsuleSourceGenerator : IIncrementalGenerator
{
    private readonly CapsuleDefinitionResolver _capsuleDefinitionResolver = new();

    private readonly ExposeDefinitionResolver _exposeDefinitionResolver = new();
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Only filtered Syntax Nodes can trigger code generation
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Any(),
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(static m => m is not null);

        // Generate the source code
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            (ctx, t) => GenerateCode(ctx, t.Left, t.Right!));
    }

    /// <summary>
    /// Checks whether the Node is annotated with the [Capsule] attribute and maps syntax context to the specific node type (ClassDeclarationSyntax).
    /// </summary>
    /// <param name="context">Syntax context, based on CreateSyntaxProvider predicate</param>
    /// <returns>The specific cast and whether the attribute was found.</returns>
    private static ClassDeclarationSyntax? GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // Go through all attributes of the class.
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        foreach (var attributeSyntax in attributeListSyntax.Attributes)
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is IMethodSymbol attributeSymbol)
            {
                var attributeName = attributeSymbol.ContainingType.ToDisplayString();

                // Check the full name of the Capsule attribute.
                if (attributeName == $"{SymbolNames.AttributionNamespace}.{SymbolNames.CapsuleAttributeName}")
                {
                    return classDeclarationSyntax;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Generate code action.
    /// It will be executed on specific nodes (ClassDeclarationSyntax annotated with the [Report] attribute) changed by the user.
    /// </summary>
    /// <param name="context">Source generation context used to add source files.</param>
    /// <param name="compilation">Compilation used to provide access to the Semantic Model.</param>
    /// <param name="classDeclarations">Nodes annotated with the [Report] attribute that trigger the generate action.</param>
    private void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations)
    {
        // Go through all filtered class declarations.
        foreach (var classDeclarationSyntax in classDeclarations)
        {
            // We need to get semantic model of the class to retrieve metadata.
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

            // Symbols allow us to get the compile-time information.
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is INamedTypeSymbol classSymbol)
            {
                var spec = _capsuleDefinitionResolver.GetCapsuleDefinition(classSymbol);

                var exposeDefinitions = _exposeDefinitionResolver.GetExposeDefinitions(context, classSymbol)
                    .ToImmutableArray();

                var renderer = new CodeRenderer(context, spec, classSymbol, exposeDefinitions);
                
                renderer.RenderCapsuleInterface();
                renderer.RenderExtensions();
            }
        }
    }

}
