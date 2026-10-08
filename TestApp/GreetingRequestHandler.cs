using MediatR.Abstractions;

namespace TestApp;

public sealed class GreetingRequestHandler : IRequestHandler<GreetingRequest, string>
{
    public Task<string> Handle(GreetingRequest request, CancellationToken cancellationToken)
    {
        var response = $"Hello, {request.Name}!";
        Console.WriteLine(response);
        return Task.FromResult(response);
    }
}