using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Core;

/// <summary>
/// Caches factories for creating request executors.
/// </summary>
public sealed class RequestExecutorFactoryCache
{
    /// <summary>
    /// Thread-safe cache of factories keyed by request and response type.
    /// Each factory creates a request executor using dependencies from the supplied service provider.
    /// </summary>
    private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), ObjectFactory> _executorFactories = new();

    /// <summary>
    /// Creates an executor using the cached factory for a request/response pair.
    /// </summary>
    /// <remarks>
    /// This method uses a ConcurrentDictionary to ensure thread-safe caching of executor factories.
    /// On the first access to a specific (requestType, responseType) combination:
    /// 1. Reflection is used to dynamically create a closed generic request executor type.
    /// 2. A factory delegate is created to efficiently instantiate that executor type.
    /// 3. The factory is cached for future use.
    ///
    /// Subsequent accesses for the same combination reuse the cached factory, avoiding repeated reflection overhead.
    /// Each call still creates a new executor using the supplied service provider.
    /// </remarks>
    /// <param name="serviceProvider">The scoped provider used to resolve handler and behavior dependencies.</param>
    /// <param name="requestType">The runtime request type.</param>
    /// <param name="responseType">The expected response type.</param>
    /// <returns>A new executor for the specified request and response types.</returns>
    internal IRequestExecutor CreateExecutor(IServiceProvider serviceProvider, Type requestType, Type responseType)
    {
        // Try to get or create a factory for this (requestType, responseType) combination
        var factory = _executorFactories.GetOrAdd(
            (requestType, responseType),
            static key =>
            {
                // On cache miss: use reflection to create the closed generic type
                // E.g., RequestExecutor<GetUserQuery, User>
                var executorType = typeof(RequestExecutor<,>).MakeGenericType(key.RequestType, key.ResponseType);

                // Create an optimized factory delegate that can instantiate the executor type
                // The factory will use dependency injection to resolve constructor dependencies
                return ActivatorUtilities.CreateFactory(executorType, Type.EmptyTypes);
            });

        // Use the factory to instantiate and return the executor
        return (IRequestExecutor)factory(serviceProvider, []);
    }
}
