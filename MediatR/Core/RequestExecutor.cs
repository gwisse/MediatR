using MediatR.Abstractions;
namespace MediatR.Core;

/// <summary>
/// Executes a request through its registered pipeline behaviors and handler.
/// </summary>
/// <typeparam name="TRequest">The type of request to execute.</typeparam>
/// <typeparam name="TResponse">The type of response produced by the request.</typeparam>
internal sealed class RequestExecutor<TRequest, TResponse> : IRequestExecutor where TRequest : IRequest<TResponse>
{
    private readonly IRequestHandler<TRequest, TResponse>? _handler;
    private readonly IPipelineBehavior<TRequest, TResponse>[] _behaviors;

    public RequestExecutor(IEnumerable<IRequestHandler<TRequest, TResponse>> handlers, IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
    {
        _handler = handlers.LastOrDefault();
        _behaviors = behaviors.ToArray();
    }

    public async Task<object?> Execute(object request, CancellationToken cancellationToken)
    {
        var typedRequest = (TRequest)request;

        if (_handler is null)
        {
            throw new InvalidOperationException($"No handler registered for request '{typeof(TRequest).FullName}'.");
        }

        RequestHandlerDelegate<TResponse> next = () => _handler.Handle(typedRequest, cancellationToken);

        // Build the pipeline from the inside out.
        //
        // DI order:
        //
        //   Logging
        //   Validation
        //   Transaction
        //
        // Execution:
        //
        //   Logging
        //      -> Validation
        //          -> Transaction
        //              -> Handler

        foreach (var behavior in _behaviors.Reverse())
        {
            var currentNext = next;

            next = () => behavior.Handle(typedRequest, currentNext, cancellationToken);
        }

        return await next();
    }
}