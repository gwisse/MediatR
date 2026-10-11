using System.Reflection;
using MediatR.Abstractions;
using MediatR.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Extensions;

internal static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        internal void RegisterHandlers(Assembly assembly)
        {
            foreach (var type in assembly.GetLoadableTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                foreach (var handlerInterface in type.GetInterfaces().Where(handler => handler.IsRequestHandler()))
                {
                    services.AddScoped(handlerInterface, type);
                }
            }
        }

        internal void ValidateNoDuplicateHandlerRegistrations()
        {
            var duplicateRegistration = services
                .Where(descriptor => descriptor.ServiceType.IsRequestHandler())
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
}