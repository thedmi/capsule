using Capsule.Testing;

using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests;

public class FakeTimerServiceTest
{
    [Test]
    public async Task Timers_do_not_fire_by_themselves()
    {
        var sut = new FakeTimerService();
        var flag = false;

        sut.StartSingleShot(TimeSpan.FromMilliseconds(1), async () => flag = true);
        await Task.Delay(50);
        
        flag.ShouldBeFalse();
    }

    [Test]
    public async Task Timers_can_be_triggered_manually()
    {
        var sut = new FakeTimerService();

        var i = 0;

        var timer1 = sut.StartSingleShot(TimeSpan.Zero, async () => i += 1);
        sut.StartSingleShot(TimeSpan.Zero, async () => i += 2);
        await Task.Delay(50);
        
        i.ShouldBe(0);
        
        await sut.ExecuteAsync(timer1);
        
        i.ShouldBe(1);

        await sut.ExecuteAllAsync();
        
        i.ShouldBe(3);
    }
}
