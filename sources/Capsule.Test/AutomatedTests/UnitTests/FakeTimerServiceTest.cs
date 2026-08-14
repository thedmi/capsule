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

    [Test]
    public async Task Deadline_timers_do_not_fire_by_themselves()
    {
        var sut = new FakeTimerService();
        var flag = false;

        sut.StartSingleShot(DateTimeOffset.UtcNow - TimeSpan.FromDays(1), async () => flag = true);
        await Task.Delay(50);

        flag.ShouldBeFalse();
    }

    [Test]
    public async Task Deadline_timers_can_be_triggered_manually_and_expose_their_deadline()
    {
        var sut = new FakeTimerService();
        var deadline = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

        var i = 0;

        var timer1 = sut.StartSingleShot(deadline, async () => i += 1);
        sut.StartSingleShot(deadline, async () => i += 2);

        timer1.Deadline.ShouldBe(deadline);
        timer1.Timeout.ShouldBeNull();
        sut.Count.ShouldBe(2);

        await sut.ExecuteAsync(timer1);

        i.ShouldBe(1);

        await sut.ExecuteAllAsync();

        i.ShouldBe(3);
        sut.Count.ShouldBe(0);
    }

    [Test]
    public async Task Deadline_timers_are_cancelled_when_the_discriminator_matches()
    {
        var sut = new FakeTimerService();
        var deadline = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

        var i = 0;

        sut.StartSingleShot(TimeSpan.FromSeconds(10), async () => i += 1, "d1");
        var timer2 = sut.StartSingleShot(deadline, async () => i += 2, "d1");

        sut.Timers.ShouldBe([timer2]);

        await sut.ExecuteAllAsync();

        i.ShouldBe(2);
    }
}
