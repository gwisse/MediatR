using MediatR.Abstractions;

namespace MediatR.DemoApp;

public sealed record GreetingRequest(string Name) : IRequest<string>;