using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class TimerServiceTest
{
    [Test]
    public async Task Timers_enqueue_the_callback_after_the_timeout_has_elapsed_and_timers_are_cleaned_up()
    {
        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var synchronizer = new FakeSynchronizer();

        var timeSpan = TimeSpan.Zero;
        var cancellationToken = new CancellationToken();

        var sut = new TimerService(
            synchronizer,
            (ts, ct) =>
            {
                timeSpan = ts;
                cancellationToken = ct;
                return taskCompletionSource.Task;
            });

        var callbackCalled = false;
        var callback = async Task () => callbackCalled = true;

        // Act 1 - start timer
        var timerRef = sut.StartNew(TimeSpan.FromSeconds(30), callback);
        await Task.Delay(100);

        // Assert 1 - awaiting delay, no invocation enqueued yet
        timeSpan.ShouldBe(TimeSpan.FromSeconds(30));
        callbackCalled.ShouldBeFalse();
        synchronizer.InvocationQueue.ShouldBeEmpty();
        sut.TimerTasks.Count().ShouldBe(1);
        cancellationToken.ShouldBe(timerRef.CancellationToken);
        
        // Act 2 - the delay elapsed
        taskCompletionSource.SetResult();
        await Task.Delay(100);

        // Assert 2 - callback and timer management invocation enqueued
        synchronizer.InvocationQueue.Count.ShouldBe(2);
        synchronizer.InvocationQueue.ToList()[0].ShouldBe(callback);
        
        // Act 3 - run enqueued invocations
        await synchronizer.ExecuteInvocationsAsync();
        
        // Assert 3 - callback executed and timers cleaned up
        callbackCalled.ShouldBeTrue();
        sut.TimerTasks.ShouldBeEmpty();
    }
    
    [Test]
    public async Task Cleanup_is_triggered_even_when_timers_are_cancelled()
    {
        var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var synchronizer = new FakeSynchronizer();

        var sut = new TimerService(
            synchronizer,
            (_, ct) =>
            {
                ct.Register((_, t) => taskCompletionSource.SetCanceled(t), null);
                return taskCompletionSource.Task;
            });

        var callbackCalled = false;
        var callback = async Task () => callbackCalled = true;

        // Act 1 - start timer
        var timerRef = sut.StartNew(TimeSpan.FromSeconds(30), callback);
        await Task.Delay(100);

        // Assert 1 - awaiting delay, no invocation enqueued yet
        callbackCalled.ShouldBeFalse();
        synchronizer.InvocationQueue.ShouldBeEmpty();
        sut.TimerTasks.Count().ShouldBe(1);
        
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
        sut.TimerTasks.ShouldBeEmpty();
    }

    private class FakeSynchronizer : ICapsuleSynchronizer
    {
        public Queue<Func<Task>> InvocationQueue { get; } = new();

        /// <summary>
        /// Executes and awaits enqueued invocations "in the foreground" to simplify test synchronization
        /// </summary>
        public async Task ExecuteInvocationsAsync()
        {
            while (InvocationQueue.Count > 0)
            {
                await InvocationQueue.Dequeue()();
                await Task.Yield();
            }
        }

        public async Task EnqueueAwaitResult(Func<Task> impl) => throw new NotImplementedException();

        public async Task<TResult> EnqueueAwaitResult<TResult>(Func<Task<TResult>> impl) => throw new NotImplementedException();

        public async Task EnqueueAwaitReception(Func<Task> impl) => throw new NotImplementedException();

        public async Task EnqueueReturn(Func<Task> impl) => InvocationQueue.Enqueue(impl);

        public T PassThrough<T>(Func<T> impl) => throw new NotImplementedException();
    }
}
