using System.Threading.Channels;

namespace Capsule;

internal class CapsuleSynchronizer(
    ChannelWriter<Func<Task>> writer,
    IInvocationLoopStatus invocationLoopStatus,
    Type capsuleType) : ICapsuleSynchronizer
{
    public async Task EnqueueAwaitResult(Func<Task> impl, bool passThroughIfQueueClosed = false)
    {
        await EnqueueAwaitResult<object?>(
                async () =>
                {
                    await impl().ConfigureAwait(false);
                    return null;
                },
                passThroughIfQueueClosed)
            .ConfigureAwait(false);
    }

    public async Task<T> EnqueueAwaitResult<T>(Func<Task<T>> impl, bool passThroughIfQueueClosed = false)
    {
        if (passThroughIfQueueClosed && invocationLoopStatus.Terminated)
        {
            // Queue has been closed, enqueuing will not work
            return await impl();
        }
        
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

    public void EnqueueReturn(Func<Task> impl)
    {
        Write(impl);
    }

    public void EnqueueReturn(Action impl)
    {
        EnqueueReturn(
            () =>
            {
                impl();
                return Task.CompletedTask;
            });
    }
    
    public T PassThrough<T>(Func<T> impl)
    {
        return impl();
    }

    private void Write(Func<Task> func)
    {
        if (invocationLoopStatus.Terminated)
        {
            throw new CapsuleInvocationException(
                $"Unable to enqueue invocation for capsule of type {capsuleType}, invocation loop has been terminated.");
        }
        
        var success = writer.TryWrite(func);

        if (!success)
        {
            throw new CapsuleInvocationException(
                $"Enqueuing invocation for capsule of type {capsuleType} failed, cannot write to queue.");
        }
    }

    /// <summary>
    /// Ensure the invocation queue is closed when the synchronizer is finalized to avoid memory leaks on the queue
    /// reader side (invocation loop).
    /// </summary>
    ~CapsuleSynchronizer() => writer.TryComplete();
}
