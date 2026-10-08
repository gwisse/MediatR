using System.Reflection;
using MediatR.Abstractions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Extensions.FluentValidation;

/// <summary>
/// Provides dependency-injection registration for FluentValidation integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the open-generic FluentValidation pipeline behavior.
    /// </summary>
    /// <param name="services">The service collection to register the behavior with.</param>
    /// <returns>The same service collection instance for chaining.</returns>
    public static IServiceCollection AddValidationBehavior(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }

    /// <summary>
    /// Registers the validation pipeline behavior and FluentValidation validators from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection to register the validation integration with.</param>
    /// <param name="assemblies">The assemblies to scan for validators.</param>
    /// <returns>The same service collection instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when no assemblies are specified.</exception>
    public static IServiceCollection AddMediatRFluentValidation(this IServiceCollection services, params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (assemblies is null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
        }

        services.AddValidationBehavior();
        services.AddValidatorsFromAssemblies(assemblies);

        return services;
    }
}
