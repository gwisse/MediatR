using System.Reflection;

namespace MediatR.Extensions;

internal static class AssemblyExtensions
{
    internal static Type[] GetLoadableTypes(this Assembly assembly)
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
