
# Usage

## Installation

Capsule is shipped as a set of Nuget packages:

- [![Capsule.Core](https://img.shields.io/nuget/v/Capsule.Core?label=Capsule.Core)](https://www.nuget.org/packages/Capsule.Core/) Core functionality such as invocation loop and synchronizer implementations. Also contains dependency injection registration extensions.
- [![Capsule.Generator](https://img.shields.io/nuget/v/Capsule.Generator?label=Capsule.Generator)](https://www.nuget.org/packages/Capsule.Generator/) C# source generator that generates interfaces and encapsulation boilerplate based on attributes.
- [![Capsule.Testing](https://img.shields.io/nuget/v/Capsule.Testing?label=Capsule.Testing)](https://www.nuget.org/packages/Capsule.Testing/) Test support library.

To get started, install the `Capsule.Core` and `Capsule.Generator` packages. Then, register the necessary dependencies with the `AddCapsuleHost()` DI extension method.


## Implementing Capsules

The *capsule implementation* is the class that needs to be made thread-safe. You create it, but do not add any synchronization primitives yourself.

By adding he `[Capsule]` attribute to that class, the Capsule generator is instructed to add the following parts:

- The capsule interface is generated or referenced.
- A static extension class is generated, containing the hull and an `Encapsulate()` extension method. This is what turns your capsule implementation into a thread-safe object.

The following subsections provide further details on these two parts.


### Interface Generation

Capsule generator is able to generate a capsule interface for each capsule implementation.

By default, Capsule considers the list of implemented interfaces on the Capsule implementation:

- If there is exactly one interface that is not `[CapsuleIgnore]` attributed, that interface is used and no additional interface is generated.
- Otherwise, an interface with the same name as the implementation, but prefixed with an "I", will be generated.

Interface generation can be customized through `CapsuleAttribute.InterfaceGeneration`:

- `Enable`: Generate an interface.
- `Disable`: Do not generate an interface.
- `Auto`: The default behavior described above applies.

The interface name can be customized through `CapsuleAttribute.InterfaceName`:

- If this property is specified and non-null, that interface name will be used.
- Otherwise, the default behavior applies.

!!! note
    If you bring your own interface, you'll need to ensure it matches the exposed methods and properties. Otherwise, the build will fail.


### Exposing Methods, Properties & Events {: #exposing-methods-properties }

In addition to the `[Capsule]` attribute on the implementation class, you'll need to add the `[Expose]` attribute to all methods, properties and events that you want to make accessible through the capsule interface. The following restrictions apply:

- The exposed methods must have a return type that is compatible with the chosen synchronization mode (see below).
- Properties are restricted to immutable getters and `PassThrough` synchronization mode must be used.
- Events are restricted to `PassThrough` synchronization mode.


The synchronization mode defaults to `AwaitCompletion`. Different modes can be specified through the `ExposeAttribute.Synchronization` property.


## Synchronization

Capsule can expose methods in different ways to match different use cases. The synchronization mode controls if/how invocations are awaited and whether thread-safety is guaranteed. Capsule defaults to "await completion" synchronization. This is the only mode that is thread-safe *and* makes encapsulated objects behave in an object-oriented way from the caller's perspective.


### Synchronization Modes

The following synchronization modes are supported:

| Synchronization Mode                        | Completes when               | Sync / Async                      | Behavior                                                                                             |
|---------------------------------------------|------------------------------|-----------------------------------|------------------------------------------------------------------------------------------------------|
| `AwaitCompletion` (default)                 | implementation completes     | async                             | :fontawesome-regular-user:{title="caller-owned"} :octicons-shield-check-16:{title="thread-safe"}     |
| `AwaitEnqueueing`                           | invocation has been enqueued | sync or async (details see below) | :material-reload:{title="loop-owned"} :octicons-shield-check-16:{title="thread-safe"}                |
| `AwaitReception`                            | invocation has been dequeued | async                             | :material-reload:{title="loop-owned"} :octicons-shield-check-16:{title="thread-safe"}                |
| `PassThrough`                               | implementation completes     | sync or async                     | :fontawesome-regular-user:{title="caller-owned"} :octicons-shield-slash-16:{title="not thread-safe"} |
| `AwaitCompletionOrPassThroughIfQueueClosed` | implementation completes     | async                             | :fontawesome-regular-user:{title="caller-owned"} :octicons-shield-slash-16:{title="not thread-safe"} |


### Invocation Owner

Invocations are either caller-owned :fontawesome-regular-user: or loop-owned :material-reload:. Caller-owned invocations behave the same way as you'd expect any invocation on an object would behave: Results and exceptions are passed back to the caller.

Loop-owned invocations enable a "fire and forget" communication between objects. The caller can continue before the invocation has been executed, but as a result won't receive return values or exceptions. `AwaitEnqueueing` is the recommended synchronization mode for "fire and forget" style communication.


### Sync / Async

Caller-owned invocations must be async. This is due to the fact that the caller will need to be suspended until the invocation can be dequeued/executed.

A special case regarding sync/async is `AwaitEnqueueing` synchronization mode. This mode only needs to enqueue the invocation, which is a synchronous operation. Thus, such methods will be exposed as synchronous on the interface, unless the interface was resolved from the list of implemented interfaces on the Capsule (see [interface generation](#interface-generation) for details).


### Thread-Safety

`AwaitCompletion`, `AwaitEnqueueing` and `AwaitReception` modes provide thread safety :octicons-shield-check-16:, `PassThrough` does not :octicons-shield-slash-16:.

`PassThrough` is not thread-safe because it doesn't enqueue the invocation, but executes it directly. It is *only* safe for immutable operations and is typically used with get-only properties that return an immutable field of the capsule implementation (e.g. an ID), or for events.

`AwaitCompletionOrPassThroughIfQueueClosed` is a special case synchronization mode that behaves as `AwaitCompletion`, but falls back to `PassThrough` if the invocation queue has been closed. This is useful for `DisposeAsync()` operations, where `DisposeAsync()` may be called by DI infrastructure just before shutdown. At that point, the invocation queue has already been terminated and `AwaitCompletion` would throw a `CapsuleInvocationException`. When it falls back to `PassThrough`, thread-safety is not guaranteed.


### Run to Completion Semantics

All synchronization modes except `PassThrough` will execute one invocation at a time (aka [run-to-completion scheduling](https://en.wikipedia.org/wiki/Run_to_completion_scheduling)). Make sure that your implementation completes quickly, as a blocked or delayed implementation will delay all pending invocations. This means that it is generally not a good idea to use `Task.Delay()` in capsule implementations (use a [timer](#timers) instead).


### Exception Handling

Exceptions that throw out of capsule implementations are routed the same way as return values are, so caller-owned invocations will throw exceptions back to the caller.

Loop-owned synchronization modes throw exceptions back into the invocation loop, because there is no caller that waits for the invocation to complete. In a default Capsule setup, such exceptions will abort the invocation loop and consequently the hosted service that runs it. This in turn will terminate the application.

This behavior is consistent with how .NET background services treat uncaught exceptions in .NET 6 and newer. The rationale for this is that uncaught exceptions must not go unnoticed. Consequently, you'll need to ensure that *expected* exceptions are caught and handled in capsule implementations.

Optionally, the failure mode can be changed to `Continue` through `CapsuleOptions.FailureMode`. With this failure mode, uncaught loop-owned exceptions will be logged and then ignored.


### Cancellation

Capsule treats cancellation tokens on exposed methods as any other parameter, so the tokens flow through to the capsule implementation unmodified and cancellation can be realized in the capsule implementation.

In case the cancellation leads to an `OperationCanceledException` being thrown out of the capsule implementation, the same behavior as outlined in [excpetion handling](#exception-handling) applies, so you'll probably want to catch these exceptions.

In any case, cancellation does not remove the invocation from the queue or otherwise change synchronization behavior.


### Disposable Capsules

In case your capsule implementation manages disposable resources, you may need to make the capsule disposable, too. Depending on your usage patterns, there are two possibilities to achieve this:

The recommended approach is to implement `IAsyncDisposable`. Like this, you'll be able to treat the dispose operation the same way as other methods. The `DisposeAsync()` method can be exposed with `[Expose]`, invocations will then flow through the invocation queue as expected. In case the capsules are registered with DI, you may want to use `AwaitCompletionOrPassThroughIfQueueClosed` synchronization mode to avoid exceptions on app shutdown (see [synchronization modes](#synchronization-modes) for details).

Another possibility is to implement `IDisposable` and expose the `Dispose()` method with synchronization mode `PassThrough`. With this approach, `Dispose()` will be called synchronously, thread-safety is not guaranteed. However, for cases where `Dispose()` is only called by DI on app shutdown, this may be a suitable solution for you, because Capsule hosting will have terminated and processed all invocation queues by the time DI calls `Dispose()`.


## Opt-In Features

### Async Initializer

Capsule implementations that need to perform asynchronous initialization can implement the `CapsuleFeature.IInitializer` interface, which defines a `Task InitializeAsync()` method.

Capsule will enqueue a single call to this method when a capsule is instantiated through `Encapsulate()`, so the invocation is guaranteed to be the first one to end up in the invocation queue and will run as soon as the invocation loop is started.


### Timers

Run-to-completion semantics dictate that individual runs should complete quickly. In other words, awaiting large delays or even sleeping is discouraged and will break responsiveness of the capsule, potentially leading to invocation queue exhaustion.

To work around this limitation, Capsule provides a timer service. The service can be used by making capsules implement `CapsuleFeature.ITimers`. When implemented, Capsule will inject an `ITimerService` during encapsulation.

!!! note
    The timer service will not be available when the constructor of the capsule implementation runs as this would create a chicken-and-egg problem. If you need to start timers when the capsule is instantiated, use an [async initializer](#async-initializer).

You can then use the timer service to register callbacks with a timeout through `StartSingleShot()`. When the timeout expires, the callback will be enqueued as just another invocation. Timers thus adheres to the thread-safety guarantees of the capsule.

Pending timers can also be cancelled. Either cancel a single timer through its `TimerReference`, or cancel all timers through `ITimerService.CancelAll()`.

For cases where only a single timer is needed, but that timer may be restarted multiple times, consider passing a `discriminator` to `StartSingleShot()`. The timer service will then ensure that at most one timer with the same discriminator exists, previous ones will be cancelled.


## Testing

One of the advantages of Capsule is that implementations remain free of synchronization code like locks or similar. Consequently, implementations can simply be unit tested by omitting the encapsulation in tests.

Generally, the following approaches work best:

- Use the unencapsulated implementation in unit tests.
- Use the whole Capsule in integration tests (integration as in "tests the combination of several classes together").


### Test Support Library

A test support library called [![Capsule.Testing](https://img.shields.io/nuget/v/Capsule.Testing?label=Capsule.Testing)](https://www.nuget.org/packages/Capsule.Testing/) is available. It provides fake implementations of Capsule infrastructure that simplifies certain test scenarios:

- `FakeTimerService`: An `ITimerService` that can be injected into capsules that use [Timers](#timers). Timer firing can be controlled from test code to avoid delays in tests.
- `FakeSynchronizer`: An `ICapsuleSynchronizer` that just executes invocations directly.
