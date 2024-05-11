using Capsule.GenericHosting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Capsule.DependencyInjection;

public static class CapsuleServiceCollectionExtensions
{
    /// <summary>
    /// Registers Capsule hosting infrastructure and the default factories for running Capsules.
    /// </summary>
    /// <remarks>
    /// This extension method establishes one singleton Capsule environment. This is sufficient for typical cases. For
    /// advanced usage where different capsules run on different invocation loops or hosts or use different queue
    /// settings, you may need to create multiple <see cref="CapsuleRuntimeContext"/> and manage them yourself.
    /// </remarks>
    public static IServiceCollection AddCapsuleHost(this IServiceCollection services)
    {
        return AddCapsuleHost(services, new CapsuleOptions());
    }

    /// <summary>
    /// Registers Capsule hosting infrastructure and the default factories that Capsule provides.
    /// </summary>
    /// <remarks>
    /// This extension method establishes one singleton Capsule environment. This is sufficient for typical cases. For
    /// advanced usage where different capsules run on different invocation loops or hosts or use different queue
    /// settings, you may need to create multiple <see cref="CapsuleRuntimeContext"/> and manage them yourself.
    /// </remarks>
    public static IServiceCollection AddCapsuleHost(this IServiceCollection services, CapsuleOptions options)
    {
        services.AddSingleton<CapsuleHost>();
        services.AddSingleton<ICapsuleHost>(p => p.GetRequiredService<CapsuleHost>());
        
        services.AddSingleton<ICapsuleSynchronizerFactory, DefaultSynchronizerFactory>();
        services.AddSingleton<ICapsuleInvocationLoopFactory>(
            p => new DefaultInvocationLoopFactory(
                p.GetRequiredService<ILogger<ICapsuleInvocationLoop>>(),
                options.FailureMode));
        services.AddSingleton<ICapsuleQueueFactory, DefaultQueueFactory>();
        
        services.AddSingleton<CapsuleRuntimeContext>();
        
        services.AddHostedService<CapsuleBackgroundService>();

        return services;
    }
}
