using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class InvocationLoopTest
{
    [Test]
    public async Task Invocations_continue_to_be_consumed_with_failure_mode_continue()
    {
        var channel = Channel.CreateUnbounded<Func<Task>>();
        var status = new InvocationLoopStatus();

        var sut = new InvocationLoop(
            channel.Reader,
            status,
            typeof(object),
            Mock.Of<ILogger<InvocationLoop>>(),
            CapsuleFailureMode.Continue
        );

        var invocationCounter = 0;

        var loopTask = sut.RunAsync(CancellationToken.None);

        channel.Writer.TryWrite(async () => invocationCounter++);
        channel.Writer.TryWrite(async () => throw new Exception("the exception"));
        channel.Writer.TryWrite(async () => invocationCounter++);

        await Task.Delay(100);

        status.Terminated.ShouldBeFalse();
        invocationCounter.ShouldBe(2);

        channel.Writer.Complete();
        await Task.Delay(100);

        status.Terminated.ShouldBeTrue();
        invocationCounter.ShouldBe(2);

        await loopTask;
    }

    [Test]
    public async Task Invocation_loop_is_aborted_with_failure_mode_abort()
    {
        var channel = Channel.CreateUnbounded<Func<Task>>();
        var status = new InvocationLoopStatus();

        var sut = new InvocationLoop(
            channel.Reader,
            status,
            typeof(object),
            Mock.Of<ILogger<InvocationLoop>>(),
            CapsuleFailureMode.Abort
        );

        var invocationException = new Exception("the exception");

        var invocationCounter = 0;

        var loopTask = sut.RunAsync(CancellationToken.None);

        channel.Writer.TryWrite(async () => invocationCounter++);
        channel.Writer.TryWrite(async () => throw invocationException);
        channel.Writer.TryWrite(async () => invocationCounter++);

        await Task.Delay(100);

        status.Terminated.ShouldBeTrue();
        invocationCounter.ShouldBe(1);

        var loopException = await Should.ThrowAsync<Exception>(async () => await loopTask);
        loopException.ShouldBe(invocationException);
    }
}
