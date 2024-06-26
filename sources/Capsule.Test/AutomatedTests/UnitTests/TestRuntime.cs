using Microsoft.Extensions.Logging;

namespace Capsule.Test.AutomatedTests.UnitTests;

public static class TestRuntime
{
    public static CapsuleRuntimeContext Create()
    {
        var loggerFactory = LoggerFactory.Create(c => c.AddNUnit().SetMinimumLevel(LogLevel.Debug));
        var host = new CapsuleHost(loggerFactory.CreateLogger<CapsuleHost>());

        return new(
            host,
            new DefaultSynchronizerFactory(
                new DefaultQueueFactory(),
                new DefaultInvocationLoopFactory(loggerFactory, CapsuleFailureMode.Abort),
                loggerFactory));
    }
}
