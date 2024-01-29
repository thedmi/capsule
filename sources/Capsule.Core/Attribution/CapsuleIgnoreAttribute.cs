namespace Capsule.Attribution;

/// <summary>
/// Ignore attributed interfaces during Capsule code generation. Capsule generator considers all interfaces on a capsule
/// implementation as candidates for hull interfaces, unless this attribute is specified.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class CapsuleIgnoreAttribute : Attribute;
