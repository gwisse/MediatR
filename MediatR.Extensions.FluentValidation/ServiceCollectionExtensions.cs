using System.Reflection;
using MediatR.DependencyInjection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Extensions.FluentValidation;

/// <summary>
/// Provides dependency-injection registration for FluentValidation integration.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers FluentValidation validators and adds the validation behavior at this position in the MediatR behavior order.
    /// </summary>
    /// <param name="configuration">The MediatR configuration to extend.</param>
    /// <param name="assemblies">The assemblies to scan for validators.</param>
    /// <returns>The same configuration instance so additional registrations can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when no assemblies are specified.</exception>
    public static MediatorServiceConfiguration AddFluentValidationBehaviour(
        this MediatorServiceConfiguration configuration,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (assemblies is null || assemblies.Length == 0)
        {
            throw new ArgumentException("At least one assembly must be specified.", nameof(assemblies));
        }

        configuration.Services.AddValidatorsFromAssemblies(assemblies);
        configuration.AddBehavior(typeof(ValidationBehavior<,>));

        return configuration;
    }
}
