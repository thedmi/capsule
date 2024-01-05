namespace Senja;

public interface IActorProxy
{
    Task<TResult> EnqueueAwaitResult<TResult>(Func<Task<TResult>> impl);

    Task EnqueueAwaitReception(Func<Task> impl);

    void EnqueueReturn(Func<Task> impl);
}
