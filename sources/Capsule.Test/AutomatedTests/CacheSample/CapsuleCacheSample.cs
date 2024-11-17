using Capsule.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Capsule.Test.AutomatedTests.CacheSample;

public class CapsuleCacheSample
{
    [Test]
    public async Task Use_cache_concurrently()
    {
        using var app = ConfigureHost();
        await app.StartAsync();

        var cache = app.Services.GetRequiredService<IMemoryCache>();

        var tasks = Enumerable
            .Range(0, 1000)
            .Select(i => Task.Run(() => cache.InsertOrUpdateAsync(i, $"content_{i}")))
            .ToList();

        await Task.WhenAll(tasks);

        foreach (var i in Enumerable.Range(0, 1000))
        {
            var content = await cache.GetAsync(i);
            content.ShouldBe($"content_{i}");
        }
    }

    private static IHost ConfigureHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(l =>
                {
                    l.AddNUnit();
                    l.SetMinimumLevel(LogLevel.Debug);
                });

                services.AddCapsuleHost();

                services.AddSingleton(new MemoryCache(TimeSpan.FromDays(1)));

                services.AddSingleton<IMemoryCache>(p =>
                    p.GetRequiredService<MemoryCache>().Encapsulate(p.GetRequiredService<CapsuleRuntimeContext>())
                );
            })
            .Build();
    }
}
