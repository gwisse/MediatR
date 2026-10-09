using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjection;

/// <summary>
/// Registers MediatR handlers with the dependency injection container.
/// </summary>
public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Adds MediatR services, registers request handlers from configured assemblies, and adds configured pipeline behaviors.
    /// </summary>
    /// <param name="services">The service collection to register services into.</param>
    /// <param name="configure">Configures handler assemblies and pipeline behaviors.</param>
    /// <returns>The same service collection instance so additional registrations can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddMediatR(
        this IServiceCollection services,
        Action<MediatorServiceConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new MediatorServiceConfiguration(services);
        configure(configuration);
        configuration.RegisterServices();

        return services;
    }

}