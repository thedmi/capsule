using Microsoft.CodeAnalysis;

namespace Capsule.Generator;

internal class ExposeSpecResolver
{
    private const string ExposeAttributeName = "ExposeAttribute";

    private const string SynchronizationPropertyName = "Synchronization";

#pragma warning disable RS2008
    private static readonly DiagnosticDescriptor UnexposableMethodError = new(
        id: "CAPSULEGEN0001",
        title: "Method cannot be exposed",
        messageFormat: "Cannot expose method '{0}': {1}",
        category: "CapsuleGen",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor UnexposablePropertyError = new(
        id: "CAPSULEGEN0002",
        title: "Property cannot be exposed",
        messageFormat: "Cannot expose property '{0}': {1}",
        category: "CapsuleGen",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
#pragma warning restore RS2008

    private readonly HashSet<INamedTypeSymbol> _taskTypeSymbols;

    public ExposeSpecResolver(Compilation compilation)
    {
        _taskTypeSymbols =
        [
            .. compilation.GetTypesByMetadataName("System.Threading.Tasks.Task"),
            .. compilation.GetTypesByMetadataName("System.Threading.Tasks.ValueTask"),
        ];
    }

    public IEnumerable<ExposeSpec> GetExposeSpecs(SourceProductionContext context, INamedTypeSymbol classSymbol)
    {
        var exposedSymbols = classSymbol
            .GetMembers()
            .Where(s =>
                s.GetAttributes().Any(a => a.HasNameAndNamespace(ExposeAttributeName, SymbolNames.AttributionNamespace))
            )
            .Select(GetExposeSpec)
            .ToList();

        return
        [
            .. exposedSymbols.Where(s =>
                s.MemberSymbol is IPropertySymbol p && IsExposable(context, p, s.Synchronization)
            ),
            .. exposedSymbols.Where(s => s.MemberSymbol is IMethodSymbol m && IsExposable(context, m)),
        ];
    }

    private ExposeSpec GetExposeSpec(ISymbol symbol)
    {
        var attr = symbol.GetAttributes().Single(a => a.AttributeClass!.Name == ExposeAttributeName);

        var synchronizationPropertyValue = attr.GetProperty(SynchronizationPropertyName)?.Value as int?;

        var synchronization = SynchronizerMethod(synchronizationPropertyValue);
        var fallbackToPassThrough = PassThroughAsFallback(synchronizationPropertyValue);

        // Determine asyncness based on return type (there seems to be no better way)
        var isAsync = symbol is IMethodSymbol { ReturnType: INamedTypeSymbol t } && _taskTypeSymbols.Contains(t);

        return new(symbol, synchronization, fallbackToPassThrough, isAsync);
    }

    private static bool IsExposable(SourceProductionContext context, IMethodSymbol method)
    {
        var exposable = true;

        if (method.MethodKind != MethodKind.Ordinary)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnexposableMethodError,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(),
                    "Only ordinary methods can be exposed."
                )
            );

            exposable = false;
        }

        if (method.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnexposableMethodError,
                    method.Locations.FirstOrDefault(),
                    method.ToDisplayString(),
                    "Exposed methods must have public or internal accessibility."
                )
            );

            exposable = false;
        }

        return exposable;
    }

    private static bool IsExposable(
        SourceProductionContext context,
        IPropertySymbol property,
        Synchronization synchronization
    )
    {
        var exposable = true;

        if (property.GetMethod == null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnexposablePropertyError,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    "Exposed properties must have a getter."
                )
            );

            exposable = false;
        }

        if (property.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Internal)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnexposablePropertyError,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    "Exposed properties must have public or internal accessibility."
                )
            );

            exposable = false;
        }

        if (synchronization != Synchronization.PassThrough)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    UnexposablePropertyError,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    "Only Synchronization.PassThrough synchronization mode is supported for properties."
                )
            );

            exposable = false;
        }

        return exposable;
    }

    private static Synchronization SynchronizerMethod(int? enumValue) =>
        enumValue switch
        {
            0 => Synchronization.EnqueueAwaitResult,
            1 => Synchronization.EnqueueAwaitReception,
            2 => Synchronization.EnqueueReturn,
            3 => Synchronization.PassThrough,
            4 => Synchronization.EnqueueAwaitResult, // CapsuleSynchronization.AwaitCompletionOrPassThroughIfQueueClosed
            _ => Synchronization.EnqueueAwaitResult,
        };

    private static bool PassThroughAsFallback(int? enumValue) =>
        enumValue switch
        {
            4 => true, // Corresponds to CapsuleSynchronization.AwaitCompletionOrPassThroughIfQueueClosed
            _ => false,
        };
}
