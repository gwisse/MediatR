using System.Reflection;
using MediatR.Abstractions;
using MediatR.Core;
using MediatR.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.DependencyInjection;

/// <summary>
/// Configures MediatR handler assembly scanning and pipeline behavior registrations.
/// </summary>
public sealed class MediatorServiceConfiguration
{
    private readonly IServiceCollection _services;
    private readonly List<Assembly> _assemblies = [];

    internal MediatorServiceConfiguration(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Gets the service collection being configured.
    /// </summary>
    public IServiceCollection Services => _services;

    /// <summary>
    /// Adds an assembly to scan for request handlers.
    /// </summary>
    /// <param name="assembly">The assembly containing request handlers.</param>
    public void RegisterRequestHandlersFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _assemblies.Add(assembly);
    }

    /// <summary>
    /// Registers a pipeline behavior implementation with the specified lifetime.
    /// Use <c>typeof(Behavior&lt;,&gt;)</c> for open generic behavior types.
    /// </summary>
    /// <param name="behaviorType">The pipeline behavior implementation type.</param>
    /// <param name="lifetime">The dependency-injection lifetime. Defaults to transient.</param>
    public void AddBehavior(Type behaviorType, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(behaviorType);

        if (behaviorType.IsAbstract || behaviorType.IsInterface)
        {
            throw new ArgumentException("A pipeline behavior must be a concrete class.", nameof(behaviorType));
        }

        if (behaviorType.ContainsGenericParameters && !behaviorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException("An open pipeline behavior must be a generic type definition.", nameof(behaviorType));
        }

        var serviceTypes = behaviorType.GetInterfaces()
            .Where(type => type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToArray();

        if (serviceTypes.Length == 0)
        {
            throw new ArgumentException(
                $"Type '{behaviorType}' does not implement {typeof(IPipelineBehavior<,>)}.",
                nameof(behaviorType));
        }

        foreach (var serviceType in serviceTypes)
        {
            var registrationType = serviceType;

            if (behaviorType.IsGenericTypeDefinition)
            {
                var behaviorArguments = behaviorType.GetGenericArguments();
                var serviceArguments = serviceType.GetGenericArguments();

                if (!serviceType.ContainsGenericParameters ||
                    serviceArguments.Length != behaviorArguments.Length ||
                    !serviceArguments.SequenceEqual(behaviorArguments))
                {
                    throw new ArgumentException(
                        $"Open pipeline behavior '{behaviorType}' must implement IPipelineBehavior<TRequest, TResponse> using its generic parameters in the same order.",
                        nameof(behaviorType));
                }

                registrationType = serviceType.GetGenericTypeDefinition();
            }

            _services.Add(new ServiceDescriptor(registrationType, behaviorType, lifetime));
        }
    }

    internal void RegisterServices()
    {
        if (_assemblies.Count == 0)
        {
            throw new InvalidOperationException("At least one assembly must be specified with RegisterRequestHandlersFromAssembly.");
        }

        var executorCache = new RequestExecutorCache();
        _services.AddSingleton(executorCache);
        _services.AddScoped<IMediator>(serviceProvider => new Mediator(serviceProvider, executorCache));

        foreach (var assembly in _assemblies.Distinct())
        {
            _services.RegisterHandlers(assembly, executorCache);
        }
    }
}
