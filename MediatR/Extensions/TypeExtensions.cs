using MediatR.Abstractions;

namespace MediatR.Extensions;

internal static class TypeExtensions
{
    internal static bool IsRequestHandler(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IRequestHandler<,>);
    }

    internal static string FormatTypeName(this Type type)
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
