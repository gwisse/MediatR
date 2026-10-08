using MediatR.Abstractions;

namespace TestApp;

public sealed record GreetingRequest(string Name) : IRequest<string>;