namespace Capsule.DependencyInjection;

/// <summary>
/// Options for customizing Capsule.
/// </summary>
public record CapsuleOptions
{
    /// <summary>
    /// The failure mode that instantiated control loops should use. Defaults to
    /// <see cref="CapsuleFailureMode.Abort"/> to provide consistent behavior with
    /// .NET background services.
    /// </summary>
    public CapsuleFailureMode FailureMode { get; set; } = CapsuleFailureMode.Abort;
}
