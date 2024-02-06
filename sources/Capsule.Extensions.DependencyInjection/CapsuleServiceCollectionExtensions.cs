﻿using Microsoft.Extensions.DependencyInjection;

namespace Capsule.Extensions.DependencyInjection;

public static class CapsuleServiceCollectionExtensions
{
    public static IServiceCollection AddCapsuleHost(this IServiceCollection services)
    {
        services.AddSingleton(typeof(ICapsuleLogger<>), typeof(LoggingAdapter<>));
        
        services.AddSingleton<CapsuleHost>();
        services.AddSingleton<ICapsuleHost>(p => p.GetRequiredService<CapsuleHost>());
        
        services.AddSingleton<ICapsuleSynchronizerFactory, DefaultSynchronizerFactory>();
        services.AddSingleton<ICapsuleInvocationLoopFactory, DefaultInvocationLoopFactory>();
        services.AddSingleton<ICapsuleQueueFactory, DefaultQueueFactory>();
        
        services.AddSingleton<CapsuleRuntimeContext>();
        
        services.AddHostedService<CapsuleBackgroundService>();

        return services;
    }
}
