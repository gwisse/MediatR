using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Core;

/// <summary>
/// Caches request executors for the lifetime of the dependency-injection container.
/// </summary>
public sealed class RequestExecutorCache
{
    private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), ObjectFactory> _executorFactories = new();

    /// <summary>
    /// Gets the cached executor for a request/response pair or creates it when it is not cached.
    /// </summary>
    /// <param name="serviceProvider">The scoped provider used to resolve handler and behavior dependencies.</param>
    /// <param name="requestType">The runtime request type.</param>
    /// <param name="responseType">The expected response type.</param>
    /// <returns>The executor for the specified request and response types.</returns>
    internal IRequestExecutor GetOrCreate(IServiceProvider serviceProvider, Type requestType, Type responseType)
    {
        var factory = _executorFactories.GetOrAdd(
            (requestType, responseType),
            static key =>
            {
                var executorType = typeof(RequestExecutor<,>).MakeGenericType(key.RequestType, key.ResponseType);
                return ActivatorUtilities.CreateFactory(executorType, Type.EmptyTypes);
            });
        return (IRequestExecutor)factory(serviceProvider, []);
    }
}
