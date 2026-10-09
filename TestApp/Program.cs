using MediatR.Abstractions;
using MediatR.DependencyInjection;
using MediatR.Extensions.FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TestApp;

var services = new ServiceCollection();
services.AddMediator(typeof(Program).Assembly);
services.AddMediatRFluentValidation(typeof(Program).Assembly);
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

await using var serviceProvider = services.BuildServiceProvider();
await using var scope = serviceProvider.CreateAsyncScope();

var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
var response = await mediator.Send(new GreetingRequest("MediatR"));