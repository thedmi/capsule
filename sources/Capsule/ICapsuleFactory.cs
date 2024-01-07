namespace Capsule;

public interface ICapsuleFactory<out T>
{
    T CreateCapsule();
}
