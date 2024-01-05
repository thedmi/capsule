namespace Senja.Test.Sample.Impl;

public class WagoDeviceActorFactory : ActorFactory<IDevice, WagoDeviceActor>
{
    private readonly Func<WagoDeviceActor> _implementationFactory;

    public WagoDeviceActorFactory(Func<WagoDeviceActor> implementationFactory, IActorHost actorHost) : base(actorHost)
    {
        _implementationFactory = implementationFactory;
    }

    protected override WagoDeviceActor CreateImplementation() => _implementationFactory();

    protected override IDevice CreateFacade(WagoDeviceActor implementation, IActorProxy actorProxy) =>
        new WagoDeviceActorFacade(implementation, actorProxy);
}
