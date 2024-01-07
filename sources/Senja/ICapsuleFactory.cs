namespace Senja;

public interface ICapsuleFactory<out T>
{
    T CreateCapsule();
}
