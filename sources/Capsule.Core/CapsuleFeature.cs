using Capsule.Attribution;

namespace Capsule;

public static class CapsuleFeature
{
    [CapsuleIgnore]
    public interface IInitializer
    {
        Task InitializeAsync();
    }
    
    [CapsuleIgnore]
    public interface ITimers
    {
        ICapsuleTimerService Timers { set; }
    }
}
