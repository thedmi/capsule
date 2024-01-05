namespace Senja.Test.Sample.Impl;

public class DeviceLifecycleCoordinatorActorFactory : ActorFactory<IDeviceRepository, DeviceLifecycleCoordinatorActor>
{
    private readonly Func<DeviceLifecycleCoordinatorActor> _implementationFactory;

    public DeviceLifecycleCoordinatorActorFactory(Func<DeviceLifecycleCoordinatorActor> implementationFactory, IActorHost actorHost) : base(actorHost)
    {
        _implementationFactory = implementationFactory;
    }

    protected override DeviceLifecycleCoordinatorActor CreateImplementation() => _implementationFactory();

    protected override IDeviceRepository CreateFacade(
        DeviceLifecycleCoordinatorActor implementation,
        IActorProxy actorProxy) =>
        new DeviceLifecycleCoordinatorActorFacade(implementation, actorProxy);
}
