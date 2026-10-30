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
            var duplicateRegistrations = services
                .Where(descriptor => descriptor.ServiceType.IsRequestHandler())
                .GroupBy(descriptor => descriptor.ServiceType)
                .Where(group => group.Count() > 1)
                .Select(group => FormatHandlerType(group.Key))
                .ToArray();

            if (duplicateRegistrations.Length == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Multiple handlers are registered for: {string.Join(", ", duplicateRegistrations)}.");
        }
    }

    private static string FormatHandlerType(Type handlerType)
    {
        return $"{nameof(IRequestHandler<IRequest<object>, object>)}<{string.Join(", ", handlerType.GetGenericArguments().Select(FormatTypeName))}>";
    }

    private static string FormatTypeName(Type type)
    {
        return type switch
        {
            _ when type == typeof(bool) => "bool",
            _ when type == typeof(byte) => "byte",
            _ when type == typeof(char) => "char",
            _ when type == typeof(decimal) => "decimal",
            _ when type == typeof(double) => "double",
            _ when type == typeof(float) => "float",
            _ when type == typeof(int) => "int",
            _ when type == typeof(long) => "long",
            _ when type == typeof(sbyte) => "sbyte",
            _ when type == typeof(short) => "short",
            _ when type == typeof(string) => "string",
            _ when type == typeof(uint) => "uint",
            _ when type == typeof(ulong) => "ulong",
            _ when type == typeof(ushort) => "ushort",
            _ => type.Name
        };
    }
}