using System.Threading.Channels;

namespace Capsule;

internal class CapsuleSynchronizer(ChannelWriter<Func<Task>> writer, Type capsuleType) : ICapsuleSynchronizer
{
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

    public async Task EnqueueReturn(Func<Task> impl)
    {
        EnqueueReturnInternal(impl);
    }
    
    public void EnqueueReturnInternal(Func<Task> impl)
    {
        Write(impl);
    }
    
    public T PassThrough<T>(Func<T> impl)
    {
        return impl();
    }

    private void Write(Func<Task> func)
    {
        var success = writer.TryWrite(func);

        if (!success)
        {
            throw new CapsuleInvocationException($"Unable to enqueue invocation for capsule of type {capsuleType}");
        }
    }
}
