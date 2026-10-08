using MediatR.Abstractions;

namespace MediatR.Core;

/// <summary>
/// Default implementation of <see cref="IMediator"/> that resolves request handlers from the service provider.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RequestExecutorCache _executorCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve request handlers and behaviors.</param>
    /// <param name="executorCache">The cache of request executors.</param>
    internal Mediator(IServiceProvider serviceProvider, RequestExecutorCache executorCache)
    {
        _serviceProvider = serviceProvider;
        _executorCache = executorCache;
    }

    /// <inheritdoc />
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executor = _executorCache.GetOrCreate(_serviceProvider, request.GetType(), typeof(TResponse));
        var result = await executor.Execute(request, cancellationToken);

        return (TResponse)result!;
    }
}