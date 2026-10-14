# MediatR

MediatR is a small in-process mediator for .NET applications. It dispatches requests to their handlers and supports pipeline behaviors for cross-cutting concerns such as validation, logging, and transactions.

## What problems does it solve?

As an application grows, controllers, endpoints, and services can become tightly coupled to many other components. A caller may need to know which concrete service performs an operation, how it is constructed, and which cross-cutting concerns must run around it. This makes code harder to test, change, and maintain.

MediatR addresses these problems by:

- Separating the sender of a request from the class that handles it
- Keeping request-specific business logic in focused handlers
- Providing a consistent place for cross-cutting concerns through pipeline behaviors
- Supporting dependency-injection-based composition and easier unit testing
- Making application flows easier to discover and evolve as features are added

MediatR is also commonly used to support the **CQRS (Command Query Responsibility Segregation)** principle. CQRS separates operations that change state (commands) from operations that retrieve data (queries). In this model, each command or query is represented by a request with its own handler, allowing the two sides to evolve independently:

- A **command** expresses an intent to change application state and may return a result or no meaningful value.
- A **query** retrieves data without changing application state.

MediatR does not enforce CQRS rules by itself, but its request-and-handler model provides a natural structure for applying them.

## Requirements

- .NET 10 SDK to build this repository
- .NET 10 runtime to run applications using the packages

## Installation

Packages are distributed through the project's ProGet NuGet feed. Configure the feed using the v3 URL shown in ProGet (replace the example URL with your feed URL):

```bash
dotnet nuget add source "https://proget.example/nuget/your-feed/v3/index.json" --name ProGet
```

For an authenticated feed, configure credentials using your organization's approved secret-management approach; do not commit credentials to source control.

Applications that dispatch requests install `MediatR`:

```bash
dotnet add package MediatR --source ProGet
```

The `MediatR` package depends on `MediatR.Abstractions`. Projects that only declare requests, handlers, or test those contracts can install just the abstractions package:

```bash
dotnet add package MediatR.Abstractions --source ProGet
```

FluentValidation is optional. Install the integration package only when needed:

```bash
dotnet add package MediatR.Extensions.FluentValidation --source ProGet
```

## Dependency injection

In an ASP.NET Core application, register MediatR on the application's service collection and scan the assembly containing your handlers:

```csharp
using MediatR.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediatR(configuration =>
    configuration.RegisterRequestHandlersFromAssembly(typeof(GetGreetingHandler).Assembly));

var app = builder.Build();
app.Run();
```

## Usage

Inject `IMediator` into the application service, controller, or endpoint that sends requests. For example, an application service can use constructor injection:

```csharp
using MediatR.Abstractions;

public sealed class GreetingService(IMediator mediator)
{
    public Task<string> GetGreeting(string name, CancellationToken cancellationToken) =>
        mediator.Send(new GetGreeting(name), cancellationToken);
}
```

The request and handler can be defined in the assembly registered above:

```csharp
using MediatR.Abstractions;

public sealed record GetGreeting(string Name) : IRequest<string>;

public sealed class GetGreetingHandler : IRequestHandler<GetGreeting, string>
{
    public Task<string> Handle(GetGreeting request, CancellationToken cancellationToken) =>
        Task.FromResult($"Hello, {request.Name}!");
}
```

The application's dependency-injection container creates `GreetingService` and supplies the scoped `IMediator` instance. There is no need to build or resolve a service provider manually.

## Requests and handlers

Requests implement `IRequest<TResponse>`. Their handlers implement `IRequestHandler<TRequest, TResponse>`:

```csharp
using MediatR.Abstractions;

public sealed record CreateOrderRequest(string OrderNumber) : IRequest<Guid>;

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderRequest, Guid>
{
    public Task<Guid> Handle(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        return Task.FromResult(orderId);
    }
}
```

`Send` resolves the handler from dependency injection and passes the cancellation token through to it.

## Pipeline behaviors

Pipeline behaviors wrap request handler execution. They implement `IPipelineBehavior<TRequest, TResponse>` and call `next` to continue the pipeline:

```csharp
using MediatR.Abstractions;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");

        var response = await next();

        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

`AddMediatR` scans only the assemblies explicitly supplied via `RegisterRequestHandlersFromAssembly`. Pipeline behaviors are registered explicitly, are transient by default, and execute in the order in which they are added:

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterRequestHandlersFromAssembly(typeof(CreateOrderHandler).Assembly);
    cfg.AddBehavior(typeof(LoggingBehavior<,>));
});
```

## FluentValidation integration

After installing `MediatR.Extensions.FluentValidation`, register the validation behavior in the MediatR configuration. It scans the supplied assemblies for validators:

```csharp
using MediatR.Extensions.FluentValidation;

services.AddMediatR(cfg =>
{
    cfg.RegisterRequestHandlersFromAssembly(typeof(CreateOrderHandler).Assembly);
    cfg.AddFluentValidationBehaviour(typeof(CreateOrderValidator).Assembly);
});
```

`AddFluentValidationBehaviour` adds the validation behavior at that point in the configuration, so you control its order relative to other behaviors. It runs all registered validators asynchronously before the request handler. If validation fails, it throws FluentValidation's `ValidationException` and the handler is not invoked:

```csharp
public sealed class CreateOrderValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(request => request.OrderNumber).NotEmpty();
    }
}
```

The integration package performs input validation only. Applications should handle validation that depends on database or external system state in the appropriate application or domain layer.

## Project structure

| Project | Description |
| --- | --- |
| `MediatR.Abstractions` | Request, handler, mediator, and pipeline contracts |
| `MediatR` | Core mediator, dependency-injection registration, and request execution |
| `MediatR.Extensions.FluentValidation` | Optional FluentValidation pipeline behavior |
| `MediatR.Tests` | Tests for the core mediator and dependency-injection registration |
| `MediatR.Extensions.FluentValidation.Tests` | Tests for the FluentValidation integration |
| `MediatR.DemoApp` | Console demonstration application |

## Building and testing

From the repository root:

```powershell
dotnet build
dotnet test
```

