using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

/// <summary>
/// Tests that callers awaiting an invocation are released when the invocation loop terminates without processing the
/// invocation, instead of waiting forever.
/// </summary>
public class LoopTerminationTest
{
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(5);

    [Test]
    public async Task Awaited_invocations_queued_behind_an_aborting_invocation_fail()
    {
        var host = CreateHost();
        var sut = CreateSynchronizerFactory().Create(new object(), host);

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var invocationException = new InvalidOperationException("boom");

        sut.EnqueueReturn(async () =>
        {
            await gate.Task;
            throw invocationException;
        });
        var awaitResult = sut.EnqueueAwaitResult(async () => 42);
        var awaitReception = sut.EnqueueAwaitReception(async () => { });
        var passThroughExecuted = false;
        var awaitResultOrPassThrough = sut.EnqueueAwaitResult(async () => passThroughExecuted = true, true);

        var hostTask = host.RunAsync(CancellationToken.None);
        gate.SetResult();

        (await Should.ThrowAsync<InvalidOperationException>(() => hostTask.WaitAsync(HangGuard))).ShouldBe(
            invocationException
        );

        await Should.ThrowAsync<CapsuleInvocationException>(() => awaitResult.WaitAsync(HangGuard));
        await Should.ThrowAsync<CapsuleInvocationException>(() => awaitReception.WaitAsync(HangGuard));
        (await awaitResultOrPassThrough.WaitAsync(HangGuard)).ShouldBeTrue();
        passThroughExecuted.ShouldBeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Awaited_invocation_that_is_written_but_never_read_is_released_on_termination(
        bool passThroughIfQueueClosed
    )
    {
        // Simulates the race where an invocation is written after the loop's final read, but before it terminates
        var channel = Channel.CreateUnbounded<Func<Task>>();
        var status = new InvocationLoopStatus();
        var sut = new CapsuleSynchronizer(channel.Writer, status, typeof(object));

        var invocationTask = sut.EnqueueAwaitResult(async () => 42, passThroughIfQueueClosed);

        await Task.Yield();
        invocationTask.IsCompleted.ShouldBeFalse();

        status.SetTerminated();

        if (passThroughIfQueueClosed)
        {
            (await invocationTask.WaitAsync(HangGuard)).ShouldBe(42);
        }
        else
        {
            await Should.ThrowAsync<CapsuleInvocationException>(() => invocationTask.WaitAsync(HangGuard));
        }
    }

    [Test]
    public async Task Host_shuts_down_remaining_loops_before_rethrowing_a_loop_fault()
    {
        var host = CreateHost();
        var failing = CreateSynchronizerFactory().Create(new object(), host);

        // Wire up the other capsule manually to get access to its loop status
        var otherChannel = Channel.CreateUnbounded<Func<Task>>();
        var otherStatus = new InvocationLoopStatus();
        host.Register(
            new InvocationLoop(
                otherChannel.Reader,
                otherStatus,
                typeof(object),
                NullLogger<InvocationLoop>.Instance,
                CapsuleFailureMode.Abort
            )
        );
        var other = new CapsuleSynchronizer(otherChannel.Writer, otherStatus, typeof(object));

        var otherRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        other.EnqueueReturn(async () =>
        {
            otherRunning.SetResult();
            await Task.Delay(200);
        });

        var hostTask = host.RunAsync(CancellationToken.None);
        await otherRunning.Task.WaitAsync(HangGuard);

        failing.EnqueueReturn(() => throw new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() => hostTask.WaitAsync(HangGuard));

        // The other loop was busy when the fault occurred, but has terminated by the time the fault surfaces
        otherStatus.Terminated.ShouldBeTrue();

        // Consequently, a dispose-like call passes through
        (await other.EnqueueAwaitResult(async () => 1, true).WaitAsync(HangGuard)).ShouldBe(1);
    }

    [Test]
    public async Task Host_rethrows_a_loop_fault_after_the_timeout_if_a_remaining_loop_is_blocked()
    {
        var host = new CapsuleHost(NullLogger<CapsuleHost>.Instance, TimeSpan.FromMilliseconds(100));
        var factory = CreateSynchronizerFactory();
        var failing = factory.Create(new object(), host);
        var blocked = factory.Create(new object(), host);

        var blockedRunning = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        blocked.EnqueueReturn(async () =>
        {
            blockedRunning.SetResult();
            await release.Task;
        });

        var hostTask = host.RunAsync(CancellationToken.None);
        await blockedRunning.Task.WaitAsync(HangGuard);

        failing.EnqueueReturn(() => throw new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() => hostTask.WaitAsync(HangGuard));
        release.Task.IsCompleted.ShouldBeFalse();

        // A call to the still running loop is processed or passed through once the loop terminates
        var call = blocked.EnqueueAwaitResult(async () => 1, true);
        release.SetResult();
        (await call.WaitAsync(HangGuard)).ShouldBe(1);
    }

    [Test]
    public async Task Capsule_created_after_host_termination_does_not_hang()
    {
        var host = CreateHost();
        using var cts = new CancellationTokenSource();

        var hostTask = host.RunAsync(cts.Token);
        await cts.CancelAsync();
        await hostTask.WaitAsync(HangGuard);

        var sut = CreateSynchronizerFactory().Create(new object(), host);

        (await sut.EnqueueAwaitResult(async () => 1, true).WaitAsync(HangGuard)).ShouldBe(1);
        var awaitResult = sut.EnqueueAwaitResult(async () => 1);
        await Should.ThrowAsync<CapsuleInvocationException>(() => awaitResult.WaitAsync(HangGuard));
    }

    [Test]
    public async Task Fault_of_a_loop_registered_after_host_termination_is_logged()
    {
        var logger = new ErrorCapturingLogger<CapsuleHost>();
        var host = new CapsuleHost(logger);
        using var cts = new CancellationTokenSource();

        var hostTask = host.RunAsync(cts.Token);
        await cts.CancelAsync();
        await hostTask.WaitAsync(HangGuard);

        var loopException = new InvalidOperationException("custom loop fault");
        host.Register(new FaultingInvocationLoop(loopException));

        (await logger.FirstError.WaitAsync(HangGuard)).ShouldBe(loopException);
    }

    [TestCase(-2)]
    [TestCase(30L * 24 * 60 * 60 * 1000)]
    public void Invalid_fault_shutdown_timeout_is_rejected(long milliseconds)
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new CapsuleHost(NullLogger<CapsuleHost>.Instance, TimeSpan.FromMilliseconds(milliseconds))
        );
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(int.MaxValue)]
    public void Valid_fault_shutdown_timeout_is_accepted(long milliseconds)
    {
        Should.NotThrow(() =>
            new CapsuleHost(NullLogger<CapsuleHost>.Instance, TimeSpan.FromMilliseconds(milliseconds))
        );
    }

    private static CapsuleHost CreateHost() => new(NullLogger<CapsuleHost>.Instance);

    private static DefaultSynchronizerFactory CreateSynchronizerFactory() =>
        new(
            new DefaultQueueFactory(),
            new DefaultInvocationLoopFactory(NullLoggerFactory.Instance, CapsuleFailureMode.Abort),
            NullLoggerFactory.Instance
        );
}

file class FaultingInvocationLoop(Exception exception) : ICapsuleInvocationLoop
{
    public Task RunAsync(CancellationToken cancellationToken) => Task.FromException(exception);
}

file class ErrorCapturingLogger<T> : ILogger<T>
{
    private readonly TaskCompletionSource<Exception?> _firstError = new(
        TaskCreationOptions.RunContinuationsAsynchronously
    );

    public Task<Exception?> FirstError => _firstError.Task;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        if (logLevel == LogLevel.Error)
        {
            _firstError.TrySetResult(exception);
        }
    }
}
