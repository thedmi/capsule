using Capsule;

namespace CapsuleGen.Sample;

[Capsule]
public class SomeCapsule : ICapsule
{
    [Expose]
    public async Task DoIt()
    {
        await Task.Yield();
    }

    public async Task InitializeAsync()
    {
    }
}
