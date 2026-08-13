using Capsule.Attribution;
using Shouldly;

namespace Capsule.Test.AutomatedTests.UnitTests
{
    public class EventCodeGenTest
    {
        [Test]
        public void Can_expose_events_defined_in_interface_automatically()
        {
            var impl = new EventCapsule();
            var sut = impl.Encapsulate(TestRuntime.Create());

            var eventRaised = false;
            sut.MyEvent += (_, _) => eventRaised = true;

            impl.RaiseEvent();

            eventRaised.ShouldBeTrue();
        }

        [Capsule]
        public class EventCapsule : IEventInterface
        {
            [Expose(Synchronization = CapsuleSynchronization.PassThrough)]
            public event EventHandler? MyEvent;

            public void RaiseEvent()
            {
                MyEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        public interface IEventInterface
        {
            event EventHandler MyEvent;
        }
    }
}
