using Capsule.Attribution;

namespace Capsule;

[CapsuleIgnore]
public interface ICapsuleInitialization
{
    Task InitializeAsync();
}
