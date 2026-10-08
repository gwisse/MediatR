using MediatR.Abstractions;

namespace TestApp;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}.");

        var response = await next();

        Console.WriteLine($"Handled {typeof(TRequest).Name}.");
        return response;
    }
}