using System.Reflection;
using MediatR.Abstractions;
using MediatR.Core;
using MediatR.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjection;

/// <summary>
/// Registers MediatR handlers and pipeline behaviors with the dependency injection container.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Adds MediatR services and registers all request handlers and pipeline behaviors found in the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="assemblies">The assemblies to scan for handlers and behaviors.</param>
    /// <returns>The same service collection instance so additional registrations can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when no assemblies are specified.</exception>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assemblies is null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
        }

        var executorCache = new RequestExecutorCache();
        services.AddSingleton(executorCache);
        services.AddScoped<IMediator>(serviceProvider => new Mediator(serviceProvider, executorCache));

        foreach (var assembly in assemblies.Distinct())
        {
            services.RegisterHandlers(assembly, executorCache);
            services.RegisterPipelineBehaviors(assembly);
        }

        return services;
    }
}