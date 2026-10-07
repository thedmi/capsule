using System.Threading.Channels;

namespace Capsule;

internal class CapsuleSynchronizer(
    ChannelWriter<Func<Task>> writer,
    InvocationLoopStatus invocationLoopStatus,
    Type capsuleType
) : ICapsuleSynchronizer
{
    public async Task EnqueueAwaitResult(Func<Task> impl, bool passThroughIfQueueClosed = false)
    {
        await EnqueueAwaitResult<object?>(
                async () =>
                {
                    await impl().ConfigureAwait(false);
                    return null;
                },
                passThroughIfQueueClosed
            )
            .ConfigureAwait(false);
    }

    public async Task<T> EnqueueAwaitResult<T>(Func<Task<T>> impl, bool passThroughIfQueueClosed = false)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task FuncAsync()
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

        if (TryWrite(FuncAsync) && await AwaitProcessingAsync(tcs.Task).ConfigureAwait(false))
        {
            return await tcs.Task.ConfigureAwait(false);
        }

        if (passThroughIfQueueClosed)
        {
            // Queue has been closed before the invocation was processed, so execute it directly
            return await impl().ConfigureAwait(false);
        }

        throw NotProcessedException();
    }

    public async Task EnqueueAwaitReception(Func<Task> impl)
    {
        var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task FuncAsync()
        {
            tcs.SetResult(null!);
            await impl().ConfigureAwait(false);
        }

        Write(FuncAsync);

        if (!await AwaitProcessingAsync(tcs.Task).ConfigureAwait(false))
        {
            throw NotProcessedException();
        }
    }

    public void EnqueueReturn(Func<Task> impl)
    {
        Write(impl);
    }

    public void EnqueueReturn(Action impl)
    {
        EnqueueReturn(() =>
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
        if (!TryWrite(func))
        {
            throw new CapsuleInvocationException(
                $"Unable to enqueue invocation for capsule of type {capsuleType}, invocation loop has been terminated."
            );
        }
    }

    /// <summary>
    /// Writes the invocation to the queue. Returns false if the invocation loop has already been terminated.
    /// </summary>
    private bool TryWrite(Func<Task> func)
    {
        if (invocationLoopStatus.Terminated)
        {
            return false;
        }

        var success = writer.TryWrite(func);

        if (!success)
        {
            throw new CapsuleInvocationException(
                $"Enqueuing invocation for capsule of type {capsuleType} failed, cannot write to queue."
            );
        }

        return true;
    }

    /// <summary>
    /// Awaits until the invocation loop has processed the invocation that completes <paramref name="invocationTask"/>.
    /// Returns false if the loop terminated without processing it, e.g. because the loop aborted or the invocation was
    /// enqueued just before termination. Without this, the caller would await <paramref name="invocationTask"/> forever.
    /// </summary>
    private async Task<bool> AwaitProcessingAsync(Task invocationTask)
    {
#pragma warning disable VSTHRD003
        await Task.WhenAny(invocationTask, invocationLoopStatus.Termination).ConfigureAwait(false);
#pragma warning restore VSTHRD003

        // The loop does not process any invocations after termination, so an invocation that is not completed by now
        // will never be
        return invocationTask.IsCompleted;
    }

    private CapsuleInvocationException NotProcessedException() =>
        new($"Invocation for capsule of type {capsuleType} was not processed, invocation loop has been terminated.");

    /// <summary>
    /// Ensure the invocation queue is closed when the synchronizer is finalized to avoid memory leaks on the queue
    /// reader side (invocation loop).
    /// </summary>
    ~CapsuleSynchronizer() => writer.TryComplete();
}
