using Capsule;

namespace CapsuleGen.Sample;

public class Runner
{
    public async Task RunAsync()
    {
        var factory = new SomeCapsuleCapsuleFactory(() => new SomeCapsule(), new CapsuleHost(null!));

        Console.WriteLine(factory);
    }
}
