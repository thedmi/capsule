using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Capsule.Generator;

/// <summary>
/// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
/// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
/// </summary>
[Generator]
public class CapsuleSourceGenerator : IIncrementalGenerator
{
    private const string AttributionNamespace = "Capsule.Attribution";

    private const string CapsuleAttributeName = "CapsuleAttribute";

    private const string ExposeAttributeName = "ExposeAttribute";

    private const string SynchronizationPropertyName = "Synchronization";
    
    private const string InterfaceNamePropertyName = "InterfaceName";
    
    private const string GenerateInterfacePropertyName = "GenerateInterface";

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
                if (attributeName == $"{AttributionNamespace}.{CapsuleAttributeName}")
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
                var spec = GetCapsuleSpec(classSymbol);

                var exposeSpecs = GetExposeSpecs(classSymbol).ToImmutableArray();
                
                RenderCapsuleInterface(context, spec, classSymbol, exposeSpecs);
                RenderExtensions(context, spec, classSymbol, exposeSpecs);
            }
        }
    }

    private CapsuleSpec GetCapsuleSpec(INamedTypeSymbol classSymbol)
    {
        var capsuleAttribute = classSymbol.GetAttributes().Single(a => AttributeHasName(a, CapsuleAttributeName));

        var interfaceName = GetAttributeProperty(capsuleAttribute, InterfaceNamePropertyName)?.Value as string ??
                            "I" + classSymbol.Name;

        var generateInterface = GetAttributeProperty(capsuleAttribute, GenerateInterfacePropertyName)?.Value as bool? ??
                                true;

        return new(interfaceName, generateInterface);
    }

    private static void RenderCapsuleInterface(
        SourceProductionContext context,
        CapsuleSpec spec,
        INamedTypeSymbol classSymbol,
        ImmutableArray<ExposeSpec> exposedMethods)
    {
        if (!spec.GenerateInterface)
        {
            return;
        }
        
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        var methods = exposedMethods.Select(symbol => RenderFacadeMethodOrProperty(symbol, false));
        
        var code =
            $$"""
              // <auto-generated/>

              namespace {{namespaceName}};

              public interface {{spec.InterfaceName}}
              {
              {{string.Join("\n\n", methods)}}
              }
              """;
        
        context.AddSource($"{spec.InterfaceName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static void RenderExtensions(
        SourceProductionContext context,
        CapsuleSpec spec,
        INamedTypeSymbol classSymbol,
        ImmutableArray<ExposeSpec> exposeSpecs)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        var extensionsClassName = classSymbol.Name + "CapsuleExtensions";
        
        var methods = exposeSpecs
            .Select(symbol => RenderFacadeMethodOrProperty(symbol, true));
        
        var code =
            $$"""
              // <auto-generated/>
              
              using Capsule;
              
              namespace {{namespaceName}};

              public static class {{extensionsClassName}}
              {
                  public static {{spec.InterfaceName}} Encapsulate(this {{classSymbol.Name}} impl, CapsuleRuntimeContext context) =>
                      new Facade(impl, context.SynchronizerFactory.Create(impl, context));
              
                  public class Facade : {{spec.InterfaceName}} 
                  {
                      private readonly {{classSymbol.Name}} _impl;
                      
                      private readonly ICapsuleSynchronizer _synchronizer;
                      
                      public Facade({{classSymbol.Name}} impl, ICapsuleSynchronizer synchronizer)
                      {
                          _impl = impl;
                          _synchronizer = synchronizer;
                      }
                      
                  {{string.Join("\n\n    ", methods)}}
                  }
              }
              """;

        // Add the source code to the compilation.
        context.AddSource($"{extensionsClassName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }
    
    private static IEnumerable<ExposeSpec> GetExposeSpecs(INamedTypeSymbol classSymbol)
    {
        var exposedSymbols = classSymbol.GetMembers()
            .Where(s => s.GetAttributes().Any(attr => AttributeHasName(attr, ExposeAttributeName)))
            .Select(GetExposeSpec)
            .ToList();

        return
        [
            ..exposedSymbols.Where(
                s => s.Symbol is IPropertySymbol
                     {
                         GetMethod: not null, DeclaredAccessibility: Accessibility.Public
                     } &&
                     s.Synchronization == Synchronization.PassThrough),
            ..exposedSymbols.Where(
                s => s.Symbol is IMethodSymbol
                {
                    MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Public
                })
        ];
    }

    private static bool AttributeHasName(AttributeData attr, string attributeName) =>
        attr.AttributeClass?.Name == attributeName &&
        attr.AttributeClass?.ContainingNamespace.ToDisplayString() == AttributionNamespace;

    private static string RenderFacadeMethodOrProperty(ExposeSpec exposeSpec, bool renderImplementation)
    {
        var attr = exposeSpec.Symbol.GetAttributes().Single(a => a.AttributeClass!.Name == ExposeAttributeName);
        
        var synchronization = GetAttributeProperty(attr, SynchronizationPropertyName);
        var proxyMethod = SynchronizerMethod(synchronization?.Value as int?);

        return exposeSpec.Symbol switch
        {
            IMethodSymbol m => RenderFacadeMethod(m, proxyMethod, renderImplementation),
            IPropertySymbol p => RenderFacadeProperty(p, exposeSpec.Synchronization, renderImplementation),
            _ => throw new ArgumentOutOfRangeException(nameof(exposeSpec.Symbol))
        };
    }

    private static string RenderFacadeMethod(
        IMethodSymbol method,
        Synchronization proxyMethod,
        bool renderImplementation)
    {
        var parameterDeclarations = string.Join(
            ", ",
            method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

        var arguments = string.Join(", ", method.Parameters.Select(p => $"{p.Name}"));

        return $$"""
                     public {{method.ReturnType}} {{method.Name}}({{parameterDeclarations}})
                 """ +
               (renderImplementation
                   ? $$"""
                        =>
                                   _synchronizer.{{proxyMethod}}(() => _impl.{{method.Name}}({{arguments}}));
                       """
                   : ";");
    }
    
    private static string RenderFacadeProperty(
        IPropertySymbol property,
        Synchronization synchronization,
        bool renderImplementation)
    {
        return $$"""
                     public {{property.GetMethod!.ReturnType}} {{property.Name}}
                 """ +
               (renderImplementation
                   ? $$"""
                        =>
                                   _synchronizer.{{synchronization}}(() => _impl.{{property.Name}});
                       """
                   : " { get; }");
    }

    private static ExposeSpec GetExposeSpec(ISymbol symbol)
    {
        var attr = symbol.GetAttributes().Single(a => a.AttributeClass!.Name == ExposeAttributeName);

        var synchronization =
            SynchronizerMethod(GetAttributeProperty(attr, SynchronizationPropertyName)?.Value as int?);

        return new (symbol, synchronization);
    }

    private static Synchronization SynchronizerMethod(int? enumValue) =>
        enumValue switch
        {
            0 => Synchronization.EnqueueAwaitResult,
            1 => Synchronization.EnqueueAwaitReception,
            2 => Synchronization.EnqueueReturn,
            3 => Synchronization.PassThrough,
            _ => Synchronization.EnqueueAwaitResult
        };

    private static TypedConstant? GetAttributeProperty(AttributeData attributeData, string propertyName)
    {
        var properties = attributeData.NamedArguments.Where(a => a.Key == propertyName).ToList();
        return properties.Any() ? properties.Single().Value : null;
    }

    private record CapsuleSpec(string InterfaceName, bool GenerateInterface);

    private record ExposeSpec(ISymbol Symbol, Synchronization Synchronization);
}
