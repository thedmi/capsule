using System.Threading.Channels;

namespace Senja;

public class ActorProxy : IActorProxy
{
    private readonly ChannelWriter<Func<Task>> _writer;

    public ActorProxy(ChannelWriter<Func<Task>> writer)
    {
        _writer = writer;
    }

    public async Task<TResult> EnqueueAwaitResult<TResult>(Func<Task<TResult>> impl)
    {
        var tcs = new TaskCompletionSource<TResult>();

        async Task Func()
        {
            try
            {
                var result = await impl();
                tcs.SetResult(result);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }

        _writer.TryWrite(Func);

        return await tcs.Task;
    }

    public async Task EnqueueAwaitReception(Func<Task> impl)
    {
        var tcs = new TaskCompletionSource();

        async Task Func()
        {
            tcs.SetResult();
            await impl();
        }

        _writer.TryWrite(Func);

        await tcs.Task;
    }

    public void EnqueueReturn(Func<Task> impl)
    {
        _writer.TryWrite(impl);
    }
}
