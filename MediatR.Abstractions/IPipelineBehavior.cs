namespace MediatR.Abstractions;

/// <summary>
/// Defines behavior that wraps the execution of a request handler.
/// </summary>
/// <typeparam name="TRequest">The type of request being processed.</typeparam>
/// <typeparam name="TResponse">The type of response produced by the request.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request and optionally invokes the next behavior or handler in the pipeline.
    /// </summary>
    /// <param name="request">The request being processed.</param>
    /// <param name="next">The delegate that invokes the next behavior or request handler.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response returned by the pipeline.</returns>
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}