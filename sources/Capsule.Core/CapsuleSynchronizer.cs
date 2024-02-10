using System.Threading.Channels;

namespace Capsule;

internal class CapsuleSynchronizer(ChannelWriter<Func<Task>> writer, Type capsuleType) : ICapsuleSynchronizer
{
    public async Task EnqueueAwaitResult(Func<Task> impl)
    {
        await EnqueueAwaitResult<object?>(async () =>
        {
            await impl().ConfigureAwait(false);
            return null;
        }).ConfigureAwait(false);
    }

    public async Task<T> EnqueueAwaitResult<T>(Func<Task<T>> impl)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task Func()
        {
            try
            {
                var result = await impl().ConfigureAwait(false);
                tcs.SetResult(result);
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        }

        Write(Func);

        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task EnqueueAwaitReception(Func<Task> impl)
    {
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task Func()
        {
            tcs.SetResult(null!);
            await impl().ConfigureAwait(false);
        }

        Write(Func);

        await tcs.Task.ConfigureAwait(false);
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
            throw new CapsuleInvocationException($"Unable to enqueue invocation for capsule of type {capsuleType}.");
        }
    }

    /// <summary>
    /// Ensure the invocation queue is closed when the synchronizer is finalized to avoid memory leaks on the queue
    /// reader side (invocation loop).
    /// </summary>
    ~CapsuleSynchronizer() => writer.TryComplete();
}
