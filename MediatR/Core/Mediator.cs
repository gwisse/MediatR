using MediatR.Abstractions;

namespace MediatR.Core;

/// <summary>
/// Default implementation of <see cref="IMediator"/> that resolves request handlers from the service provider.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RequestExecutorFactoryCache _executorFactoryCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve request handlers and behaviors.</param>
    /// <param name="executorFactoryCache">The cache of request executor factories.</param>
    internal Mediator(IServiceProvider serviceProvider, RequestExecutorFactoryCache executorFactoryCache)
    {
        _serviceProvider = serviceProvider;
        _executorFactoryCache = executorFactoryCache;
    }

    /// <inheritdoc />
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var executor = _executorFactoryCache.CreateExecutor(_serviceProvider, request.GetType(), typeof(TResponse));
        var result = await executor.Execute(request, cancellationToken);

        return (TResponse)result!;
    }
}