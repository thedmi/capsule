
namespace Capsule.Attribution;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CapsuleAttribute : Attribute
{
    public string? InterfaceName { get; init; } = null;

    public bool GenerateInterface { get; init; } = true;
}
