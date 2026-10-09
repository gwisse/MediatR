using MediatR.Abstractions;
using MediatR.DependencyInjection;
using MediatR.Extensions.FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TestApp;

var services = new ServiceCollection();
services.AddMediatR(cfg =>
{
    cfg.RegisterRequestHandlersFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(LoggingBehavior<,>));
});
services.AddMediatRFluentValidation(typeof(Program).Assembly);

await using var serviceProvider = services.BuildServiceProvider();
await using var scope = serviceProvider.CreateAsyncScope();

var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
var response = await mediator.Send(new GreetingRequest("MediatR"));