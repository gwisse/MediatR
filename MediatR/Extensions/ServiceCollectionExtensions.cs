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
    }

    private static bool IsRequestHandlerInterface(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
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