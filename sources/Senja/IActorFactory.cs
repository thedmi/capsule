namespace Senja;

public interface IActorFactory<out T>
{
    T CreateActor();
}
