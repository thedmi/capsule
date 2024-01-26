using System.ComponentModel;

// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable RedundantTypeDeclarationBody

namespace System.Runtime.CompilerServices;

// Workaround for missing IsExternalInit on .NET standard, see https://github.com/dotnet/roslyn/issues/45510
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IsExternalInit { }
