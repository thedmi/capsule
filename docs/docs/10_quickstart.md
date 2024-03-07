
# Quick Start

First, install the [![Capsule.Core](https://img.shields.io/nuget/v/Capsule.Core?label=Capsule.Core)](https://www.nuget.org/packages/Capsule.Core/) and [![Capsule.Generator](https://img.shields.io/nuget/v/Capsule.Generator?label=Capsule.Generator)](https://www.nuget.org/packages/Capsule.Generator/) Nuget packages.

Now define a capsule implementation that you want to wrap in a thread-safe way:

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
    public async Task InsertOrUpdateAsync(int key, string content) => 
        await cache.InsertOrUpdateAsync(key, content);
}
```

Note the usage of `IMemoryCache` (capsule interface) instead of `MemoryCache`. The former is backed by the thread-safe hull, the latter is the non-thread-safe implementation.

As you can see, the only thing we need to make the implementation thread-safe are the `[Capsule]` and `[Expose]` attributes. Neither locks nor concurrent collections (that are hard to use correctly) are needed.

Typically, you'll register the capsule in DI. Capsule comes with support for Microsoft dependency injection, which is included in the `Capsule.Core` nuget package.

Assuming `MemoryCache` has already been properly registered in DI, the following registers `IMemoryCache` capsules:

```csharp
services.AddCapsuleHost();

services.AddSingleton<IMemoryCache>(
    p => new MemoryCache(TimeSpan.FromDays(1))
        .Encapsulate(p.GetRequiredService<CapsuleRuntimeContext>()));
```

The first line registers Capsule infrastructure. The second line instantiates `MemoryCache` and wraps it in the thread-safe hull using `Encapsulate()`. The result is then registered as `IMemoryCache`.
