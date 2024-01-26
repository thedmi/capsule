using Capsule.Attribution;

namespace CapsuleGen.Sample;

[Capsule]
public class SomeCapsule
{
    [Expose]
    public async Task DoIt()
    {
        await Task.Yield();
    }
}
