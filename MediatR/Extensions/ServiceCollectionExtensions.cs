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

        internal void RegisterPipelineBehaviors(Assembly assembly)
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                if (type.IsGenericTypeDefinition && ImplementsPipelineBehavior(type))
                {
                    services.AddTransient(typeof(IPipelineBehavior<,>), type);
                    continue;
                }

                foreach (var behaviorInterface in type.GetInterfaces().Where(IsPipelineBehaviorInterface))
                {
                    services.AddTransient(behaviorInterface, type);
                }
            }
        }
    }

    private static bool IsRequestHandlerInterface(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);
    }

    private static bool IsPipelineBehaviorInterface(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>);
    }

    private static bool ImplementsPipelineBehavior(Type type)
    {
        return type.GetInterfaces().Any(IsPipelineBehaviorInterface);
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>();
        }
    }

    //TODO: GetLoadableTypes hides reflection exceptions, which may lead to unexpected behavior. Consider the method below.
    /*
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
     */
}