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

- .NET 10
- `Microsoft.Extensions.DependencyInjection`

## Installation

This repository currently contains the source project rather than a published NuGet package. Add a project reference to `MediatR.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="path\to\MediatR\MediatR.csproj" />
</ItemGroup>
```

## Dependency injection

Register MediatR with the dependency-injection container and scan the assembly that contains your requests and handlers:

```csharp
using MediatR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddMediator(typeof(CreateOrderHandler).Assembly);
```

## Usage

Inject `IMediator` into the class that needs to send requests:

```csharp
using MediatR.Abstractions;

public sealed class OrderService(IMediator mediator)
{
    public Task<Guid> CreateOrder(string orderNumber, CancellationToken cancellationToken)
    {
        return mediator.Send( new CreateOrderRequest(orderNumber), cancellationToken);
    }
}
```

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

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
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

When an assembly is scanned, concrete request handlers and both open-generic and closed pipeline behaviors are registered automatically.

## FluentValidation integration

Validation is provided as an optional integration package so applications that do not use FluentValidation do not take a dependency on it. Add the `MediatR.Extensions.FluentValidation` package alongside `MediatR`, then register the integration and validators:

```csharp
using FluentValidation;
using MediatR.Extensions.FluentValidation;

services.AddMediatRFluentValidation(
    typeof(CreateOrderValidator).Assembly);
```

The behavior runs all registered validators before the request handler. If validation fails, it throws FluentValidation's `ValidationException` and the handler is not invoked:

```csharp
public sealed class CreateOrderValidator
    : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(request => request.OrderNumber)
            .NotEmpty();
    }
}
```

The integration package performs input validation only. Applications should handle validation that depends on database or external system state in the appropriate application or domain layer.

## Project structure

| Project | Description |
| --- | --- |
| `MediatR` | Core mediator, dependency-injection registration, and request execution |
| `MediatR.Extensions.FluentValidation` | Optional FluentValidation pipeline behavior |
| `MediatR.Tests` | Unit and integration tests |
| `TestApp` | Sample application |

## Building and testing

From the repository root:

```powershell
dotnet build
dotnet test
```

## License

No license has been specified for this repository yet.
