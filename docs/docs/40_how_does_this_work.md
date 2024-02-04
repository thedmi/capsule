
# How does this Work?

Internally, Capsule implements a message queue and [run-to-completion semantics](https://en.wikipedia.org/wiki/Run_to_completion_scheduling). This means that invocations are not executed directly but enqueued in a [thread-safe channel](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels.channel-1?view=net-8.0) and then processed one at a time (also known as turn-based concurrency).

When a capsule is instantiated, the following infrastructure is created:

- a dedicated, thread-safe channel
- a hull that implements the capsule interface and passes invocations to its synchronizer, which in turn passes them to the channel
- an invocation loop that executes invocations read from the channel

To ensure the invocation loops remain active, they are registered with a capsule host. By default, this is an `IHostedService` that runs and monitors the invocation loops. The host is registered with the `AddCapsuleHost()` DI extension method.
