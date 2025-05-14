namespace Capsule;

public interface ISelfInvocationEnqueuer
{
    void Enqueue(Func<Task> impl);

    void Enqueue(Action impl);
}
