namespace Capsule.Attribution;

public enum CapsuleInterfaceGeneration
{
    /// <summary>
    /// Do not generate Capsule interface if there is a single candidate interface present. Generate otherwise.
    /// </summary>
    Auto,

    /// <summary>
    /// Do not generate Capsule interface.
    /// </summary>
    Disable,

    /// <summary>
    /// Generate Capsule interface.
    /// </summary>
    Enable,
}
