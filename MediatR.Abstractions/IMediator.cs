namespace MediatR.Abstractions;

/// <summary>
/// Coordinates sending requests to the appropriate request handler.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request and returns the response value.
    /// </summary>
    /// <typeparam name="TResponse">The type of response produced by the request.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The response from the request handler.</returns>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}