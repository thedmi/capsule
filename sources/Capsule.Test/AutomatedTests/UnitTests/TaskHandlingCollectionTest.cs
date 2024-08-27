using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class TaskHandlingCollectionTest
{
    [Test]
    public void RemoveCompleted_removes_completed_tasks_and_returns_them()
    {
        var sut = new TaskHandlingCollection<TimerReference>(tr => tr.TimerTask);

        var completed1 = new TimerReference(TimeSpan.FromSeconds(1), Task.FromResult(1), new CancellationTokenSource());
        var completed2 = new TimerReference(TimeSpan.FromSeconds(2), Task.FromResult(2), new CancellationTokenSource());
        var failed = new TimerReference(TimeSpan.FromSeconds(3), Task.FromException(new InvalidOperationException()), new CancellationTokenSource());
        var cancelled = new TimerReference(TimeSpan.FromSeconds(4), Task.FromCanceled(new CancellationToken(true)), new CancellationTokenSource());
        var running = new TimerReference(TimeSpan.FromSeconds(5), Task.Delay(-1), new CancellationTokenSource());

        sut.Count().ShouldBe(0);
        
        sut.Add(completed1);
        sut.Add(completed2);
        sut.Add(failed);
        sut.Add(cancelled);
        sut.Add(running);
        
        sut.Count().ShouldBe(5);

        var removed = sut.RemoveCompleted();
        
        removed.Count.ShouldBe(4);
        removed.ShouldContain(completed1);
        removed.ShouldContain(completed2);
        removed.ShouldContain(failed);
        removed.ShouldContain(cancelled);
        removed.ShouldNotContain(running);
        
        sut.ToList().ShouldBe([running]);
    }
}
