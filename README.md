
# Capsule

Capsule is a .NET Standard 2.0 library and C# source generator that provides thread-safe object encapsulation.

It turns ordinary objects into thread-safe *capsules*. A capsule can be used *concurrently* without the risk of race conditions. The original interface is retained, so this is *transparent* for callers.

![Encapsulating an object](capsule.drawio.svg)

To create a capsule, you provide an implementation (green) and specify how the methods and properties (yellow) are to be encapsulated. Capsule then adds synchronization infrastructure (blue) and the hull (red), a generated interface adapter.

Apart from a few attributes and the requirement to make methods `async`, implementations remain free of synchronization code. This keeps code readable and focused. It also improves testability because synchronization concerns are separated.


## Motivation

Writing thread-safe code is hard. .NET offers a lot of powerful synchronization primitives and helper classes, but using them in complex problem domains leads to a lot of boilerplate, lower readability, reduced testability and consequently a higher risk for bugs.

Capsule aims to improve the situation through the following approaches:

- Provide generic thread-safe synchronization that allows for different synchronization modes such as "await result" or "fire and forget"
- Ensure usage of thread-safe objects follows an object-oriented approach that integrates seamlessly with ordinary code
- Generate boilerplate instead of making library users maintain it themselves

To achieve this, Capsule employs well-known synchronization primitives such as `Channel<T>` and `TaskCompletionSource` and a purpose-built source generator.


## Quick Start

First, install the `Capsule` Nuget package. This package installs the runtime library as well as the source generator.

Now define a capsule implementation that should be wrapped in a thread-safe way:

```csharp
[Capsule]
public class MemoryCache
{
    private readonly Dictionary<int, Entry> _cacheEntries = new();

    [Expose]
    public async Task InsertOrUpdateAsync(int key, string content)
    {
        _cacheEntries[key] = new Entry(DateTime.UtcNow, content);
    }

    [Expose]
    public async Task<string?> GetAsync(int key)
    {
        return _cacheEntries.GetValueOrDefault(key)?.Content;
    }

    private record Entry(DateTime Timestamp, string Content);
}
```

This is a simple example of an in-memory cache. It is so simple that it could have been implemented with ordinary locks or a concurrent dictionary. We'll do it with Capsule anyway for the sake of this example.

The dictionary in `MemoryCache` is mutable state that must not be accessed concurrently. Capsule turns `MemoryCache` into a capsule by finding the `[Capsule]` attribute and wrapping the class in a thread-safe hull with an interface that matches the `[Expose]` attributed methods. This can then be used in concurrent contexts, e.g. in an ASP.NET Core controller:

```csharp
[Controller]
public class SampleController(IMemoryCache cache)
{
    [HttpGet]
    public async Task<string?> GetAsync(int key) => await cache.GetAsync(key);
    
    [HttpPut]
    public async Task InsertOrUpdateAsync(int key, string content) => await cache.InsertOrUpdateAsync(key, content);
}
```

Note the usage of `IMemoryCache` (capsule interface) instead of `MemoryCache`. The former is backed by the thread-safe hull, the latter is the non-thread-safe implementation.

As you can see, the only thing we need to make the implementation thread-safe are the `[Capsule]` and `[Expose]` attributes. Neither locks nor concurrent collections that are hard to use correctly are needed.

Typically, you'll register the capsule in the dependency injection container. Capsule comes with support for Microsoft DI through the `Capsule.Extensions.DependencyInjection` package, which is already included when using the `Capsule` nuget package.

Assuming `MemoryCache` has already been properly registered in DI, the following registers `IMemoryCache` capsules:

```csharp
services.AddCapsuleHost();

services.AddSingleton<IMemoryCache>(
    p => new MemoryCache(TimeSpan.FromDays(1))
        .Encapsulate(p.GetRequiredService<CapsuleRuntimeContext>()));
```

The first line registers Capsule infrastructure. The second line instantiates `MemoryCache` and wraps it in the thread-safe hull using `Encapsulate()`. The result is then registered as `IMemoryCache`.


## Usage Details

### Nuget Packages

Capsule is shipped as a set of Nuget packages. To get started quickly, just install the `Capsule` convenience package. If you need more control or have advanced use cases, packages can be used individually as well:

- `Capsule.Core`: Core functionality such as invocation loop and synchronizer implementations. Lightweight library without upstream dependencies.
- `Capsule.Generator`: C# source generator that generates interfaces and encapsulation boilerplate based on attributes.
- `Capsule.Extensions.DependencyInjection`: Integrates the core library with [.NET generic hosting](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host) and .NET logging. Provides extension methods to register dependencies.


### Implementing Capsules

The *capsule implementation* is the class that needs to be made thread-safe. You create it, but do not add any synchronization primitives yourself.

By adding he `[Capsule]` attribute to that class, the Capsule generator is instructed to add the following parts:

- The capsule interface is generated or referenced.
- A static extension class is generated, containing the hull and an `Encapsulate()` extension method.

The following subsections provide further details on these two parts.

#### Interface Generation

Capsule generator generates a capsule interface for each capsule implementation. By default, the interface has the implementation class' name with an "I" prefix. This can be customized through the `CapsuleAttribute.InterfaceName` property.

Also, you can bring your own capsule interface. Capsule uses the following logic to decide interface generation:

1. If `CapsuleAttribute.GenerateInterface` is specified, that value determines if an interface is generated.
1. If the implementation implements exactly one interface that is not `[CapsuleIgnore]` attributed, that interface will be used and no additional interface will be generated.
1. Otherwise, the generator defaults to generating an interface.

If you provide the interface yourself, you'll need to ensure it matches the exposed methods and properties.


#### Exposing Methods & Properties

In addition to the `[Capule]` attribute on the implementation class, you'll need to add the `[Expose]` attribute to all methods and properties that you want to make accessible through the capsule interface. The following restrictions apply:

- The exposed methods need to be `async` (unless synchronization mode `PassThrough` is specified).
- Properties are restricted to immutable getters and `PassThrough` synchronization mode must be used.

The synchronization mode defaults to `AwaitCompletion`. Different modes can be specified through the `ExposeAttribute.Synchronization` property.


### Synchronization Modes

Capsule defaults to "await completion" synchronization. This is the only mode that makes encapsulated objects behave in an object-oriented way from the caller's perspective because it is the only mode that is able to route method return values back to the caller.

However, other synchronization modes are available that provide different synchronization guarantees:

- `AwaitCompletion` (default): The hull will await completion of invocation processing by the capsule implementation. This is the only thread-safe synchronization mode that is able to return values and throw exceptions back to the caller.
- `AwaitReception`: The hull will await reception of the invocation by the capsule implementation and then return. Such methods cannot return a value.
- `AwaitEnqueueing`: The hull will enqueue the invocation and return immediately. Such methods cannot return a value. This mode is a good choice for "fire and forget" calls, e.g. offloading work to other activity contexts.
- `PassThrough`: The hull will invoke the capsule implementation directly and thus bypass all thread-safety mechanisms that the Capsule library provides. This is *only* safe for immutable operations and is typically used with get-only properties that return an immutable field of the capsule implementation (e.g. an ID).


#### Exception Handling

TBD

## How does this Work?

Internally, Capsule implements a message queue and run-to-completion semantics. This means that invocations are not executed directly but enqueued in a [thread-safe channel](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels.channel-1?view=net-8.0) and then processed one at a time (also known as turn-based concurrency).

When a capsule is instantiated, the following infrastructure is created:

- a dedicated, thread-safe channel
- a hull that implements the capsule interface and passes invocations to its synchronizer, which in turn passes them to the channel
- an invocation loop that executes invocations read from the channel

To ensure the invocation loops remain active, they are registered with a capsule host. By default, this is an `IHostedService` that runs and monitors the invocation loops. The host is registered with the `AddCapsuleHost()` DI extension method.
