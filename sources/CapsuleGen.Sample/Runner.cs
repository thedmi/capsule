using Capsule;
using Capsule.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

namespace CapsuleGen.Sample;

public class Runner
{
    public async Task RunAsync()
    {
        var loggerFactory = LoggerFactory.Create(c => c.AddConsole().SetMinimumLevel(LogLevel.Debug));
        var host = new CapsuleHost(loggerFactory.CreateLogger<CapsuleHost>().AsCapsuleLogger());

        var runtimeContext = new CapsuleRuntimeContext(
            host,
            new CapsuleSynchronizerFactory(
                new CapsuleInvocationLoopFactory(
                    loggerFactory.CreateLogger<ICapsuleInvocationLoop>().AsCapsuleLogger())));
        
        var factory = new SomeCapsule().Encapsulate(runtimeContext);

        Console.WriteLine(factory);
    }
}
