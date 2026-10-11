using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Core;

/// <summary>
/// Caches request executors for the lifetime of the dependency-injection container.
/// </summary>
public sealed class RequestExecutorCache
{
    /// <summary>
    /// Thread-safe cache that stores ObjectFactory instances for each unique (RequestType, ResponseType) combination.
    /// The factory is used to efficiently instantiate RequestExecutor<TRequest, TResponse> objects.
    /// </summary>
    private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), ObjectFactory> _executorFactories = new();

    /// <summary>
    /// Gets the cached executor for a request/response pair or creates it when it is not cached.
    /// </summary>
    /// <remarks>
    /// This method uses a ConcurrentDictionary to ensure thread-safe caching of request executors.
    /// On the first access to a specific (requestType, responseType) combination:
    /// 1. Reflection is used to dynamically create a closed generic RequestExecutor type
    /// 2. A factory delegate is created to efficiently instantiate the executor
    /// 3. The factory is cached for future use
    ///
    /// Subsequent accesses for the same combination reuse the cached factory, avoiding repeated reflection overhead.
    /// </remarks>
    /// <param name="serviceProvider">The scoped provider used to resolve handler and behavior dependencies.</param>
    /// <param name="requestType">The runtime request type.</param>
    /// <param name="responseType">The expected response type.</param>
    /// <returns>The executor for the specified request and response types.</returns>
    internal IRequestExecutor GetOrCreate(IServiceProvider serviceProvider, Type requestType, Type responseType)
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
