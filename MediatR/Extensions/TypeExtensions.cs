using MediatR.Abstractions;

namespace MediatR.Extensions;

internal static class TypeExtensions
{
    internal static bool IsRequestHandler(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);
    }
}
