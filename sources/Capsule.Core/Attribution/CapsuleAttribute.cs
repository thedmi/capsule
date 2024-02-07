
namespace Capsule.Attribution;

/// <summary>
/// Enables encapsulation for a class. By adding this attribute, the Capsule generator will generate an extension class
/// containing an `Encapsulate()` method to turn instances of this class into a thread-safe capsule.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CapsuleAttribute : Attribute
{
    /// <summary>
    /// Customize the capsule interface name. This interface will be implemented by the capsule hull.
    /// </summary>
    public string? InterfaceName { get; init; } = null;

    /// <summary>
    /// Whether or not to generate the capsule interface.
    /// </summary>
    public bool GenerateInterface { get; init; } = true;
}
