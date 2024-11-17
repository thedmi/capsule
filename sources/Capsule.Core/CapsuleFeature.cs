using Capsule.Attribution;

namespace Capsule;

/// <summary>
/// Opt-in features for Capsule implementations.
/// </summary>
public static class CapsuleFeature
{
    /// <summary>
    /// Initializer feature. <see cref="InitializeAsync"/> will be the first invocation that is executed by the
    /// capsule's invocation loop. This can be used when async initialization is needed, or when other features
    /// (e.g. timers must be configured).
    /// </summary>
    /// <remarks>
    /// At the time initialization runs, all other features are guaranteed to have been injected/activated.
    /// </remarks>
    [CapsuleIgnore]
    public interface IInitializer
    {
        Task InitializeAsync();
    }

    /// <summary>
    /// Timer feature. By implementing this interface the capsule implementation receives a timer service during
    /// encapsulation.
    /// </summary>
    /// <remarks>
    /// The timer service is not yet available when the capsule implementation's constructor runs. Use the
    /// <see cref="IInitializer"/> feature if you need to set up timers at initialization time.
    /// </remarks>
    [CapsuleIgnore]
    public interface ITimers
    {
        ITimerService Timers { set; }
    }
}
