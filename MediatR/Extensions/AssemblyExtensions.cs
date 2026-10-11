using System.Reflection;

namespace MediatR.Extensions;

/// <summary>
/// Extension methods for working with assemblies, particularly for handler and behavior discovery.
/// </summary>
internal static class AssemblyExtensions
{
    /// <summary>
    /// Safely retrieves all loadable types from an assembly, with comprehensive error handling.
    /// </summary>
    /// <remarks>
    /// This method wraps Assembly.GetTypes() to handle ReflectionTypeLoadException, which occurs when
    /// one or more types in the assembly cannot be loaded (e.g., due to missing dependencies).
    ///
    /// In such cases, this method converts the individual loader exceptions into a more informative
    /// InvalidOperationException with an AggregateException containing all load failures, making it
    /// easier to diagnose what types failed to load and why.
    /// </remarks>
    /// <param name="assembly">The assembly to scan for types.</param>
    /// <returns>An array of all successfully loadable types from the assembly.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more types cannot be loaded from the assembly.
    /// The inner AggregateException contains all individual loader exceptions.
    /// </exception>
    internal static Type[] GetLoadableTypes(this Assembly assembly)
    {
        try
        {
            // Attempt to retrieve all types from the assembly
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // ReflectionTypeLoadException contains an array of exceptions for types that failed to load
            // Filter out null exceptions and collect all meaningful load errors
            var loaderExceptions = ex.LoaderExceptions
                .Where(static exception => exception is not null)
                .Cast<Exception>();

            // Throw a more descriptive exception with all load failures aggregated
            // This helps developers understand which types failed and why
            throw new InvalidOperationException(
                $"Could not load all types from assembly '{assembly.FullName}'.",
                new AggregateException(loaderExceptions));
        }
    }
}
