using System.Reflection;
using MediatR.Abstractions;
using MediatR.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        internal void RegisterHandlers(Assembly assembly, RequestExecutorCache executorCache)
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                foreach (var handlerInterface in type.GetInterfaces().Where(IsRequestHandlerInterface))
                {
                    services.AddScoped(handlerInterface, type);

                    var genericArguments = handlerInterface.GetGenericArguments();
                    executorCache.Prebuild(genericArguments[0], genericArguments[1]);
                }
            }
        }

        internal void ValidateNoDuplicateHandlerRegistrations()
        {
            var duplicateRegistration = services
                .Where(descriptor => IsRequestHandlerInterface(descriptor.ServiceType))
                .GroupBy(descriptor => descriptor.ServiceType)
                .FirstOrDefault(group => group.Count() > 1);

            if (duplicateRegistration is null)
            {
                return;
            }

            var handlerTypes = string.Join(
                ", ",
                duplicateRegistration.Select(descriptor =>
                    descriptor.ImplementationType?.FullName ?? "factory or instance"));

            throw new InvalidOperationException(
                $"Multiple handlers are registered for '{duplicateRegistration.Key}': {handlerTypes}.");
        }
    }

    private static bool IsRequestHandlerInterface(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            var loaderExceptions = ex.LoaderExceptions
                .Where(static exception => exception is not null)
                .Cast<Exception>();

            throw new InvalidOperationException(
                $"Could not load all types from assembly '{assembly.FullName}'.",
                new AggregateException(loaderExceptions));
        }
    }
}