using System.Collections.Concurrent;
using System.Diagnostics;
using Capsule.Testing;
using MathNet.Numerics.Statistics;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class TimerServiceTest
{
    [Test]
    public async Task Timers_enqueue_the_callback_after_the_timeout_has_elapsed_and_timers_are_cleaned_up()
    {
        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var synchronizer = new ManualSynchronizer();

        var timeSpan = TimeSpan.Zero;
        var cancellationToken = new CancellationToken();

        var sut = new TimerService(
            synchronizer,
            new NullLogger<TimerService>(),
            (ts, ct) =>
            {
                timeSpan = ts;
                cancellationToken = ct;
                return taskCompletionSource.Task;
            }
        );

        var callbackCalled = false;
        var callback = async Task () => callbackCalled = true;

        // Act 1 - start timer
        var timerRef = sut.StartSingleShot(TimeSpan.FromSeconds(30), callback);
        await Task.Delay(100);

        // Assert 1 - awaiting delay, no invocation enqueued yet
        timeSpan.ShouldBe(TimeSpan.FromSeconds(30));
        callbackCalled.ShouldBeFalse();
        synchronizer.InvocationQueue.ShouldBeEmpty();
        sut.Timers.ToList().ShouldBe([timerRef]);
        cancellationToken.ShouldBe(timerRef.CancellationToken);

        // Act 2 - the delay elapsed
        taskCompletionSource.SetResult();
        await Task.Delay(100);

        // Assert 2 - callback and timer management invocation enqueued
        synchronizer.InvocationQueue.Count.ShouldBe(2);

        // Act 3 - run enqueued invocations
        await synchronizer.ExecuteInvocationsAsync();

        // Assert 3 - callback executed and timers cleaned up
        callbackCalled.ShouldBeTrue();
        sut.Timers.ShouldBeEmpty();
    }

    [Test]
    public async Task Cleanup_is_triggered_even_when_timers_are_cancelled()
    {
        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var synchronizer = new ManualSynchronizer();

        var sut = new TimerService(
            synchronizer,
            new NullLogger<TimerService>(),
            (_, ct) =>
            {
                ct.Register((_, t) => taskCompletionSource.SetCanceled(t), null);
                return taskCompletionSource.Task;
            }
        );

        var callbackCalled = false;
        var callback = async Task () => callbackCalled = true;

        // Act 1 - start timer
        var timerRef = sut.StartSingleShot(TimeSpan.FromSeconds(30), callback);
        await Task.Delay(100);

        // Assert 1 - awaiting delay, no invocation enqueued yet
        callbackCalled.ShouldBeFalse();
        synchronizer.InvocationQueue.ShouldBeEmpty();
        sut.Timers.ToList().ShouldBe([timerRef]);

        // Act 2 - cancel the timer
        timerRef.Cancel();
        await Task.Delay(100);

        // Assert 2 - callback and timer management invocation enqueued
        synchronizer.InvocationQueue.Count.ShouldBe(1);
        synchronizer.InvocationQueue.ShouldNotContain(callback);

        // Act 3 - run enqueued invocations
        await synchronizer.ExecuteInvocationsAsync();

        // Assert 3 - callback executed and timers cleaned up
        callbackCalled.ShouldBeFalse();
        sut.Timers.ShouldBeEmpty();
    }

    [Test]
    public async Task Timers_dont_fire_early()
    {
        const int numberOfRuns = 200;

        var synchronizer = new FakeSynchronizer();
        var sut = new TimerService(synchronizer, new NullLogger<TimerService>());

        var stopwatch = new Stopwatch();

        var runs = new List<double>(numberOfRuns);

        for (var i = 0; i < numberOfRuns; i++)
        {
            stopwatch.Reset();
            stopwatch.Start();

            var timerRef = sut.StartSingleShot(
                TimeSpan.FromMilliseconds(14),
                () =>
                {
                    var elapsed = stopwatch.Elapsed;
                    elapsed.ShouldBeGreaterThan(TimeSpan.FromMilliseconds(5));
                    runs.Add(elapsed.TotalSeconds);
                    return Task.CompletedTask;
                }
            );

            await timerRef.TimerTask;
        }

        Console.WriteLine("Mean: " + runs.Mean());
        Console.WriteLine("StdDev: " + runs.StandardDeviation());
        Console.WriteLine("Min: " + runs.Min());
        Console.WriteLine("Max: " + runs.Max());

        runs.Count.ShouldBe(numberOfRuns);
    }

    [Test]
    public async Task Existing_timers_are_cancelled_when_the_discriminator_matches()
    {
        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var synchronizer = new ManualSynchronizer();

        var callback1Called = false;
        var callback2Called = false;
        var callback3Called = false;

        var sut = new TimerService(synchronizer, new NullLogger<TimerService>(), (_, _) => taskCompletionSource.Task);

        sut.StartSingleShot(TimeSpan.FromSeconds(10), async () => callback1Called = true, "d1");
        sut.StartSingleShot(TimeSpan.FromSeconds(20), async () => callback2Called = true, "d1");
        sut.StartSingleShot(TimeSpan.FromSeconds(10), async () => callback3Called = true, "d2");

        // Act
        taskCompletionSource.SetResult();
        await Task.Delay(100);
        await synchronizer.ExecuteInvocationsAsync();

        // Assert
        callback1Called.ShouldBeFalse();
        callback2Called.ShouldBeTrue();
        callback3Called.ShouldBeTrue();
    }

    [Test]
    public async Task Callbacks_are_not_executed_when_the_timer_is_cancelled()
    {
        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var synchronizer = new ManualSynchronizer();

        var callbackCalled = false;

        var sut = new TimerService(synchronizer, new NullLogger<TimerService>(), (_, _) => taskCompletionSource.Task);

        var timerRef = sut.StartSingleShot(TimeSpan.FromSeconds(10), async () => callbackCalled = true);

        // Act
        // Timer expires, callback is enqueued
        taskCompletionSource.SetResult();
        await Task.Delay(100);

        // Now the timer is cancelled
        timerRef.Cancel();
        await synchronizer.ExecuteInvocationsAsync();

        // Assert
        callbackCalled.ShouldBeFalse();
    }

    [Test]
    public async Task Deadline_timers_wait_for_the_remaining_time_and_then_enqueue_the_callback()
    {
        var synchronizer = new ManualSynchronizer();
        var clock = new TestTimeProvider(T0);
        var delays = new DelayRecorder();

        var sut = new TimerService(synchronizer, new NullLogger<TimerService>(), delays.DelayAsync, clock);

        var callbackCalled = false;
        var deadline = T0 + TimeSpan.FromSeconds(30);

        // Act 1 - start timer
        var timerRef = sut.StartSingleShot(deadline, async () => callbackCalled = true);
        await Task.Delay(100);

        // Assert 1 - awaiting the remaining time, no invocation enqueued yet
        delays.Requested.ShouldBe([TimeSpan.FromSeconds(30)]);
        timerRef.Deadline.ShouldBe(deadline);
        timerRef.Timeout.ShouldBeNull();
        callbackCalled.ShouldBeFalse();
        synchronizer.InvocationQueue.ShouldBeEmpty();
        sut.Timers.ToList().ShouldBe([timerRef]);

        // Act 2 - the deadline is reached and the delay elapses
        clock.UtcNow = deadline;
        await delays.CompleteLatestAsync();

        // Assert 2 - callback and timer management invocation enqueued, no further delay requested
        delays.Requested.Count.ShouldBe(1);
        synchronizer.InvocationQueue.Count.ShouldBe(2);

        // Act 3 - run enqueued invocations
        await synchronizer.ExecuteInvocationsAsync();

        // Assert 3 - callback executed and timers cleaned up
        callbackCalled.ShouldBeTrue();
        sut.Timers.ShouldBeEmpty();
    }

    [Test]
    public async Task Deadline_timers_dont_fire_early_when_the_wall_clock_is_stepped_backwards()
    {
        var synchronizer = new ManualSynchronizer();
        var clock = new TestTimeProvider(T0);
        var delays = new DelayRecorder();

        var sut = new TimerService(synchronizer, new NullLogger<TimerService>(), delays.DelayAsync, clock);

        var callbackCalled = false;
        var deadline = T0 + TimeSpan.FromSeconds(30);

        sut.StartSingleShot(deadline, async () => callbackCalled = true);
        await Task.Delay(100);

        delays.Requested.ShouldBe([TimeSpan.FromSeconds(30)]);

        // Act 1 - the wall clock is stepped backwards by 5 seconds while the timer is pending, so when the delay
        // elapses, the deadline has not been reached yet
        clock.UtcNow = deadline - TimeSpan.FromSeconds(5);
        await delays.CompleteLatestAsync();

        // Assert 1 - nothing enqueued, the remaining 5 seconds (plus backoff) are awaited instead
        callbackCalled.ShouldBeFalse();
        synchronizer.InvocationQueue.ShouldBeEmpty();
        delays.Requested.ShouldBe([TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5) + TimeSpan.FromMilliseconds(1)]);

        // Act 2 - the deadline is reached
        clock.UtcNow = deadline;
        await delays.CompleteLatestAsync();
        await synchronizer.ExecuteInvocationsAsync();

        // Assert 2 - the callback ran, and it ran only once
        callbackCalled.ShouldBeTrue();
        delays.Requested.Count.ShouldBe(2);
        sut.Timers.ShouldBeEmpty();
    }

    [Test]
    public async Task Deadline_timers_fire_on_wake_up_when_the_wall_clock_is_stepped_forward()
    {
        var synchronizer = new ManualSynchronizer();
        var clock = new TestTimeProvider(T0);
        var delays = new DelayRecorder();

        var sut = new TimerService(synchronizer, new NullLogger<TimerService>(), delays.DelayAsync, clock);

        var callbackCalled = false;

        sut.StartSingleShot(T0 + TimeSpan.FromSeconds(30), async () => callbackCalled = true);
        await Task.Delay(100);

        // Act - the wall clock is stepped forward past the deadline
        clock.UtcNow = T0 + TimeSpan.FromMinutes(5);
        await delays.CompleteLatestAsync();
        await synchronizer.ExecuteInvocationsAsync();

        // Assert - the timer is late already, so it fires without further delays
        callbackCalled.ShouldBeTrue();
        delays.Requested.Count.ShouldBe(1);
        sut.Timers.ShouldBeEmpty();
    }

    [Test]
    public async Task Deadlines_that_have_passed_already_fire_immediately()
    {
        var synchronizer = new ManualSynchronizer();
        var clock = new TestTimeProvider(T0);
        var delays = new DelayRecorder();

        var sut = new TimerService(synchronizer, new NullLogger<TimerService>(), delays.DelayAsync, clock);

        var callbackCalled = false;

        // Act
        sut.StartSingleShot(T0 - TimeSpan.FromSeconds(1), async () => callbackCalled = true);
        await Task.Delay(100);

        // Assert - just like a timeout of zero, a deadline in the past leads to a zero delay
        delays.Requested.ShouldBe([TimeSpan.Zero]);

        await delays.CompleteLatestAsync();
        await synchronizer.ExecuteInvocationsAsync();

        callbackCalled.ShouldBeTrue();
        sut.Timers.ShouldBeEmpty();
    }

    [Test]
    public async Task Waits_for_distant_deadlines_are_split_into_chunks()
    {
        var synchronizer = new ManualSynchronizer();
        var clock = new TestTimeProvider(T0);
        var delays = new DelayRecorder();

        var sut = new TimerService(synchronizer, new NullLogger<TimerService>(), delays.DelayAsync, clock);

        var deadline = T0 + TimeSpan.FromDays(3);

        sut.StartSingleShot(deadline, async () => { });
        await Task.Delay(100);

        // Assert 1 - the first chunk is limited to a day
        delays.Requested.ShouldBe([TimeSpan.FromDays(1)]);

        // Act - a day passes
        clock.UtcNow = T0 + TimeSpan.FromDays(1);
        await delays.CompleteLatestAsync();

        // Assert 2 - the next chunk is limited as well, and nothing has been enqueued yet
        delays.Requested.ShouldBe([TimeSpan.FromDays(1), TimeSpan.FromDays(1)]);
        synchronizer.InvocationQueue.ShouldBeEmpty();
    }

    [Test]
    public async Task Cleanup_is_triggered_even_when_deadline_timers_are_cancelled()
    {
        var synchronizer = new ManualSynchronizer();
        var clock = new TestTimeProvider(T0);
        var delays = new DelayRecorder();

        var sut = new TimerService(synchronizer, new NullLogger<TimerService>(), delays.DelayAsync, clock);

        var callbackCalled = false;

        var timerRef = sut.StartSingleShot(T0 + TimeSpan.FromSeconds(30), async () => callbackCalled = true);
        await Task.Delay(100);

        // Act - cancel the pending timer
        timerRef.Cancel();
        await Task.Delay(100);

        // Assert - only the timer management invocation was enqueued
        synchronizer.InvocationQueue.Count.ShouldBe(1);

        await synchronizer.ExecuteInvocationsAsync();

        callbackCalled.ShouldBeFalse();
        sut.Timers.ShouldBeEmpty();
    }

    [Test]
    public async Task Existing_timers_are_cancelled_when_the_discriminator_matches_across_overloads()
    {
        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var synchronizer = new ManualSynchronizer();
        var clock = new TestTimeProvider(T0);

        var callback1Called = false;
        var callback2Called = false;
        var callback3Called = false;
        var callback4Called = false;

        var sut = new TimerService(
            synchronizer,
            new NullLogger<TimerService>(),
            (_, _) => taskCompletionSource.Task,
            clock
        );

        // A deadline timer replaces a pending timeout timer with the same discriminator and vice versa
        sut.StartSingleShot(TimeSpan.FromSeconds(10), async () => callback1Called = true, "d1");
        sut.StartSingleShot(T0 - TimeSpan.FromSeconds(1), async () => callback2Called = true, "d1");
        sut.StartSingleShot(T0 - TimeSpan.FromSeconds(1), async () => callback3Called = true, "d2");
        sut.StartSingleShot(TimeSpan.FromSeconds(10), async () => callback4Called = true, "d2");

        // Act
        taskCompletionSource.SetResult();
        await Task.Delay(100);
        await synchronizer.ExecuteInvocationsAsync();

        // Assert
        callback1Called.ShouldBeFalse();
        callback2Called.ShouldBeTrue();
        callback3Called.ShouldBeFalse();
        callback4Called.ShouldBeTrue();
    }

    [Test]
    public async Task Deadline_timers_dont_fire_early()
    {
        const int numberOfRuns = 50;

        var synchronizer = new FakeSynchronizer();
        var sut = new TimerService(synchronizer, new NullLogger<TimerService>());

        var runs = new List<double>(numberOfRuns);

        for (var i = 0; i < numberOfRuns; i++)
        {
            var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(14);

            var timerRef = sut.StartSingleShot(
                deadline,
                () =>
                {
                    var now = DateTimeOffset.UtcNow;
                    now.ShouldBeGreaterThanOrEqualTo(deadline);
                    runs.Add((now - deadline).TotalSeconds);
                    return Task.CompletedTask;
                }
            );

            await timerRef.TimerTask;
        }

        Console.WriteLine("Mean lateness: " + runs.Mean());
        Console.WriteLine("Max lateness: " + runs.Max());

        runs.Count.ShouldBe(numberOfRuns);
    }

    [Test]
    public void Timeout_timers_expose_their_timeout()
    {
        var sut = new TimerService(
            new FakeSynchronizer(),
            new NullLogger<TimerService>(),
            (_, _) => new TaskCompletionSource().Task
        );

        var timerRef = sut.StartSingleShot(TimeSpan.FromSeconds(30), () => Task.CompletedTask);

        timerRef.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        timerRef.Deadline.ShouldBeNull();
    }

    private static readonly DateTimeOffset T0 = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// A wall clock that is fully controlled by the test. Contrary to FakeTimeProvider from
    /// Microsoft.Extensions.TimeProvider.Testing, this one allows the clock to be stepped backwards, which is exactly
    /// what deadline timers need to cope with.
    /// </summary>
    private class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    /// <summary>
    /// A delay provider that records the requested delays and completes them on demand.
    /// </summary>
    private class DelayRecorder
    {
        private readonly List<TaskCompletionSource> _pending = [];

        public List<TimeSpan> Requested { get; } = [];

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken));

            Requested.Add(delay);
            _pending.Add(taskCompletionSource);

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Completes the delay that was requested last and gives continuations a chance to run.
        /// </summary>
        public async Task CompleteLatestAsync()
        {
            _pending[^1].TrySetResult();
            await Task.Delay(100);
        }
    }

    private class ManualSynchronizer : ICapsuleSynchronizer
    {
        // Timers enqueue their invocations from the thread pool, so several timers that elapse at the same time will
        // enqueue concurrently. A non-thread-safe queue corrupts itself in that situation.
        public ConcurrentQueue<Func<Task>> InvocationQueue { get; } = new();

        /// <summary>
        /// Executes and awaits enqueued invocations "in the foreground" to simplify test synchronization
        /// </summary>
        public async Task ExecuteInvocationsAsync()
        {
            while (InvocationQueue.TryDequeue(out var invocation))
            {
                await invocation();
                await Task.Yield();
            }
        }

        public async Task EnqueueAwaitResult(Func<Task> impl, bool passThroughIfQueueClosed = false) =>
            throw new InvalidOperationException();

        public async Task<TResult> EnqueueAwaitResult<TResult>(
            Func<Task<TResult>> impl,
            bool passThroughIfQueueClosed = false
        ) => throw new InvalidOperationException();

        public async Task EnqueueAwaitReception(Func<Task> impl) => throw new InvalidOperationException();

#pragma warning disable VSTHRD100
        public async void EnqueueReturn(Func<Task> impl) => InvocationQueue.Enqueue(impl);
#pragma warning restore VSTHRD100

        public void EnqueueReturn(Action impl) =>
            EnqueueReturn(() =>
            {
                impl();
                return Task.CompletedTask;
            });

        public T PassThrough<T>(Func<T> impl) => throw new InvalidOperationException();
    }
}
