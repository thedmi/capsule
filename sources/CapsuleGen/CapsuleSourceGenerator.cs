using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace CapsuleGen;

/// <summary>
/// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
/// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
/// </summary>
[Generator]
public class CapsuleSourceGenerator : IIncrementalGenerator
{
    private const string Namespace = "Capsule.Generated";

    private const string CapsuleAttributeName = "CapsuleAttribute";

    private const string ExposeAttributeName = "ExposeAttribute";
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Only filtered Syntax Nodes can trigger code generation
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Any(),
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(static m => m is not null)!;

        // Generate the source code
        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            (ctx, t) => GenerateCode(ctx, t.Left, t.Right!));
    }

    private string AttributeSource(string targetNamespace, string attributeName, string usage)
    {
        return
            $$"""
              // <auto-generated/>

              namespace {{targetNamespace}};

              [System.AttributeUsage(System.AttributeTargets.{{usage}})]
              public class {{attributeName}} : System.Attribute { }
              """;
    }
    
    private string ExposeAttributeSource(string targetNamespace)
    {
        return
            $$"""
              // <auto-generated/>

              namespace {{targetNamespace}};

              [System.AttributeUsage(System.AttributeTargets.Method)]
              public class {{ExposeAttributeName}} : System.Attribute 
              {
                  public CapsuleSynchronization Synchronization { get; init; } = CapsuleSynchronization.AwaitCompletion;
              }
              
              public enum CapsuleSynchronization 
              {
                  AwaitCompletion,
                  AwaitReception,
                  AwaitEnqueueing,
                  PassThrough
              }
              """;
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

                // Check the full name of the [Capsule] attribute.
                if (attributeName == $"Capsule.{CapsuleAttributeName}")
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
                var exposedMethods = GetExposedMethods(classSymbol).ToImmutableArray();
                RenderCapsuleInterface(context, classSymbol, exposedMethods);
                RenderFactory(context, classSymbol);
                RenderFacade(context, classSymbol, exposedMethods);
            }
        }
    }

    private static void RenderCapsuleInterface(SourceProductionContext context, INamedTypeSymbol classSymbol,
        ImmutableArray<IMethodSymbol> exposedMethods)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        var methods = GetExposedMethods(classSymbol)
            .Select(symbol => RenderFacadeMethod(symbol, false));
        
        var interfaceName = "I" + classSymbol.Name;
        
        var code =
            $$"""
              // <auto-generated/>

              namespace {{namespaceName}};

              public interface {{interfaceName}}
              {
              {{string.Join("\n\n", methods)}}
              }
              """;
        
        context.AddSource($"{interfaceName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static void RenderFactory(SourceProductionContext context, INamedTypeSymbol classSymbol)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        var factoryClassName = classSymbol.Name + "CapsuleFactory";
        var interfaceName = "I" + classSymbol.Name;
        
        var code =
            $$"""
              // <auto-generated/>
              
              using Capsule;

              namespace {{namespaceName}};

              public class {{factoryClassName}} : CapsuleFactory<{{interfaceName}}, {{classSymbol.Name}}>
              {
                  private readonly Func<{{classSymbol.Name}}> _implementationFactory;
              
                  public {{factoryClassName}}(Func<{{classSymbol.Name}}> implementationFactory, CapsuleRuntimeContext ctx) : base(ctx)
                  {
                      _implementationFactory = implementationFactory;
                  }
              
                  protected override {{classSymbol.Name}} CreateImplementation() => _implementationFactory();
              
                  protected override {{interfaceName}} CreateFacade({{classSymbol.Name}} implementation, ICapsuleSynchronizer synchronizer) =>
                      new {{classSymbol.Name}}CapsuleFacade(implementation, synchronizer);
              }
              """;

        // Add the source code to the compilation.
        context.AddSource($"{factoryClassName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static void RenderFacade(SourceProductionContext context, INamedTypeSymbol classSymbol, ImmutableArray<IMethodSymbol> exposedMethods)
    {
        var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        var methods = exposedMethods
            .Select(symbol => RenderFacadeMethod(symbol, true));

        var facadeClassName = classSymbol.Name + "CapsuleFacade";
        var interfaceName = "I" + classSymbol.Name;
                
        var code =
            $$"""
              // <auto-generated/>
              
              using Capsule;

              namespace {{namespaceName}};

              public class {{facadeClassName}} : {{interfaceName}}
              {
                  private readonly {{classSymbol.Name}} _impl;
              
                  private readonly ICapsuleSynchronizer _synchronizer;
              
                  public {{facadeClassName}}({{classSymbol.Name}} impl, ICapsuleSynchronizer synchronizer)
                  {
                      _impl = impl;
                      _synchronizer = synchronizer;
                  }

              {{string.Join("\n\n", methods)}}
              }
              """;

        context.AddSource($"{facadeClassName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static IEnumerable<IMethodSymbol> GetExposedMethods(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m is { MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Public } && m.GetAttributes().Any(a => a.AttributeClass?.Name == ExposeAttributeName));
    }

    private static string RenderFacadeMethod(IMethodSymbol method, bool renderImplementation)
    {
        var attr = method.GetAttributes().Single(a => a.AttributeClass!.Name == ExposeAttributeName);
        
        var synchronization = attr.NamedArguments.Where(a => a.Key == "Synchronization").ToList();
        var proxyMethod = ProxyMethod(synchronization.Any() ? synchronization.Single().Value.Value as int? : null);
       
        var parameterDeclarations = string.Join(", ", method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}"));

        var arguments = string.Join(", ", method.Parameters.Select(p => $"{p.Name}"));

        return
            $$"""
                  public {{method.ReturnType}} {{method.Name}}({{parameterDeclarations}})
              """ +
            (renderImplementation
                ? $$"""
                     =>
                            _synchronizer.{{proxyMethod}}(() => _impl.{{method.Name}}({{arguments}}));    
                    """
                : ";");
    }

    private static string ProxyMethod(int? enumValue) =>
        enumValue switch
        {
            0 => "EnqueueAwaitResult",
            1 => "EnqueueAwaitReception",
            2 => "EnqueueReturn",
            3 => "PassThrough",
            _ => "EnqueueAwaitResult"
        };
}
