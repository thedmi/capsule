using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class TaskCollectionTest
{
    [Test]
    public void RemoveCompleted_removes_completed_tasks_and_returns_them()
    {
        var sut = new TaskCollection();

        var completedTask1 = Task.FromResult(1);
        var completedTask2 = Task.FromResult(2);
        var failedTask = Task.FromException(new InvalidOperationException());
        var cancelledTask = Task.FromCanceled(new CancellationToken(true));
        var runningTask = Task.Delay(-1);

        sut.Count().ShouldBe(0);
        
        sut.Add(completedTask1);
        sut.Add(completedTask2);
        sut.Add(failedTask);
        sut.Add(cancelledTask);
        sut.Add(runningTask);
        
        sut.Count().ShouldBe(5);

        var removed = sut.RemoveCompleted();
        
        removed.Count.ShouldBe(4);
        removed.ShouldContain(completedTask1);
        removed.ShouldContain(completedTask2);
        removed.ShouldContain(failedTask);
        removed.ShouldContain(cancelledTask);
        removed.ShouldNotContain(runningTask);
        
        sut.ShouldBe([runningTask]);
    }
}
