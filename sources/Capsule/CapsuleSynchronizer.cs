using System.Threading.Channels;

namespace Capsule;

public class CapsuleSynchronizer : ICapsuleSynchronizer
{
    private readonly ChannelWriter<Func<Task>> _writer;
    
    private readonly Type _capsuleType;

    public CapsuleSynchronizer(ChannelWriter<Func<Task>> writer, Type capsuleType)
    {
        _writer = writer;
        _capsuleType = capsuleType;
    }

    public async Task EnqueueAwaitResult(Func<Task> impl)
    {
        await EnqueueAwaitResult<object?>(async () =>
        {
            await impl();
            return null;
        });
    }

    public async Task<TResult> EnqueueAwaitResult<TResult>(Func<Task<TResult>> impl)
    {
        var tcs = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

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

        Write(Func);

        return await tcs.Task;
    }

    public async Task EnqueueAwaitReception(Func<Task> impl)
    {
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task Func()
        {
            tcs.SetResult(null!);
            await impl();
        }

        Write(Func);

        await tcs.Task;
    }

    public void EnqueueReturn(Func<Task> impl)
    {
        Write(impl);
    }

    public T PassThrough<T>(Func<T> impl)
    {
        return impl();
    }

    private void Write(Func<Task> func)
    {
        var success = _writer.TryWrite(func);

        if (!success)
        {
            throw new CapsuleInvocationException($"Unable to enqueue invocation for capsule of type {_capsuleType}");
        }
    }
}
