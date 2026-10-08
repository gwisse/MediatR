namespace MediatR.Abstractions;

/// <summary>
/// Handles a request and returns a response.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the specified request.
    /// </summary>
    /// <param name="request">The request instance.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response produced by the request.</returns>
    Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken);
}