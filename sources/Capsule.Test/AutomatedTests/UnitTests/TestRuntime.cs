using Capsule.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.UnitTests;

public static class TestRuntime
{
    public static CapsuleRuntimeContext Create()
    {
        var loggerFactory = LoggerFactory.Create(c => c.AddNUnit().SetMinimumLevel(LogLevel.Debug));
        var host = new CapsuleHost(loggerFactory.CreateLogger<CapsuleHost>().AsCapsuleLogger());

        return new(
            host,
            new CapsuleSynchronizerFactory(
                new CapsuleInvocationLoopFactory(
                    loggerFactory.CreateLogger<ICapsuleInvocationLoop>().AsCapsuleLogger())));
    }
}
