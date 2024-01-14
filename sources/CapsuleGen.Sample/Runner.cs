using Capsule;

using Microsoft.Extensions.Logging;

namespace CapsuleGen.Sample;

public class Runner
{
    public async Task RunAsync()
    {
        var loggerFactory = LoggerFactory.Create(c => c.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var host = new CapsuleHost(loggerFactory.CreateLogger<CapsuleHost>());

        var runtimeContext = new CapsuleRuntimeContext(
            host,
            new CapsuleInvocationLoopFactory(loggerFactory.CreateLogger<CapsuleInvocationLoop>()));
        
        var factory = new SomeCapsuleCapsuleFactory(() => new SomeCapsule(), runtimeContext);

        Console.WriteLine(factory);
    }
}
