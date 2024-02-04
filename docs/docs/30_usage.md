
# Usage

## Installation

Capsule is shipped as a set of Nuget packages. To get started quickly, just install the `Capsule` convenience package. If you need more control or have advanced use cases, packages can be used individually as well:

- `Capsule.Core`: Core functionality such as invocation loop and synchronizer implementations. Lightweight library without upstream dependencies.
- `Capsule.Generator`: C# source generator that generates interfaces and encapsulation boilerplate based on attributes.
- `Capsule.Extensions.DependencyInjection`: Integrates the core library with [.NET generic hosting](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host) and .NET logging. Provides extension methods to register dependencies.


## Implementing Capsules

The *capsule implementation* is the class that needs to be made thread-safe. You create it, but do not add any synchronization primitives yourself.

By adding he `[Capsule]` attribute to that class, the Capsule generator is instructed to add the following parts:

- The capsule interface is generated or referenced.
- A static extension class is generated, containing the hull and an `Encapsulate()` extension method.

The following subsections provide further details on these two parts.

### Interface Generation

Capsule generator generates a capsule interface for each capsule implementation. By default, the interface has the implementation class' name with an "I" prefix. This can be customized through the `CapsuleAttribute.InterfaceName` property.

Also, you can bring your own capsule interface. Capsule uses the following logic to decide interface generation:

1. If `CapsuleAttribute.GenerateInterface` is specified, that value determines if an interface is generated.
1. If the implementation implements exactly one interface that is not `[CapsuleIgnore]` attributed, that interface will be used and no additional interface will be generated.
1. Otherwise, the generator defaults to generating an interface.

If you provide the interface yourself, you'll need to ensure it matches the exposed methods and properties.


### Exposing Methods & Properties

In addition to the `[Capule]` attribute on the implementation class, you'll need to add the `[Expose]` attribute to all methods and properties that you want to make accessible through the capsule interface. The following restrictions apply:

- The exposed methods need to be `async` (unless synchronization mode `PassThrough` is specified).
- Properties are restricted to immutable getters and `PassThrough` synchronization mode must be used.

The synchronization mode defaults to `AwaitCompletion`. Different modes can be specified through the `ExposeAttribute.Synchronization` property.


## Synchronization Modes

Capsule defaults to "await completion" synchronization. This is the only mode that makes encapsulated objects behave in an object-oriented way from the caller's perspective because it is the only mode that is able to route method return values back to the caller.

However, other synchronization modes are available that provide different synchronization guarantees:

- `AwaitCompletion` (default): The hull will await completion of invocation processing by the capsule implementation. This is the only thread-safe synchronization mode that is able to return values and throw exceptions back to the caller.
- `AwaitReception`: The hull will await reception of the invocation by the capsule implementation and then return. Such methods cannot return a value.
- `AwaitEnqueueing`: The hull will enqueue the invocation and return immediately. Such methods cannot return a value. This mode is a good choice for "fire and forget" calls, e.g. offloading work to other activity contexts.
- `PassThrough`: The hull will invoke the capsule implementation directly and thus bypass all thread-safety mechanisms that the Capsule library provides. This is *only* safe for immutable operations and is typically used with get-only properties that return an immutable field of the capsule implementation (e.g. an ID).

All synchronization modes except `PassThrough` will ensure thread-safety by handling one invocation at a time (aka [run-to-completion scheduling](https://en.wikipedia.org/wiki/Run_to_completion_scheduling)). Make sure that your implementation completes quickly, as a blocked or delayed implementation will delay all pending invocations. This means that it is generally not a good idea to use `Task.Delay()` in capsule implementations.


### Exception Handling

Exceptions that throw out of capsule implementations are routed the same way as return values are, so synchronization modes `AwaitCompletion` and `PassThrough` will throw exceptions back to the caller.

With the other synchronization modes, exceptions throw back into the invocation loop. The default invocation loop implementation catches all exceptions and logs them.


### Cancellation

Capsule treats cancellation tokens on exposed parameters as any other parameter, so the tokens flow through to the capsule implementation unmodified and cancellation can be realized in the capsule implementation.

In case the cancellation leads to an `OperationCanceledException` being thrown out of the capsule implementation, the same behavior as outlined in [excpetion handling](#exception-handling) applies.

In any case, cancellation does not remove the invocation from the queue or otherwise change synchronization behavior.
