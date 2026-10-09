using FluentAssertions;
using FluentValidation;
using MediatR.Abstractions;
using MediatR.DependencyInjection;
using MediatR.Extensions.FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Extensions.FluentValidation.Tests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public void AddFluentValidation_RegistersBehaviorAtConfigurationPosition()
    {
        var services = new ServiceCollection();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterRequestHandlersFromAssembly(
                typeof(ValidationBehaviorTests).Assembly);
            configuration.AddBehavior(typeof(BeforeValidationBehavior<,>));
            configuration.AddFluentValidationBehaviour(
                typeof(ValidationBehaviorTests).Assembly);
            configuration.AddBehavior(typeof(AfterValidationBehavior<,>));
        });

        var behaviors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descriptor => descriptor.ImplementationType)
            .ToArray();

        behaviors.Should().Equal(
            typeof(BeforeValidationBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(AfterValidationBehavior<,>));
    }

    [Fact]
    public async Task Send_InvalidRequest_ThrowsValidationExceptionWithoutCallingHandler()
    {
        var tracker = new ValidationHandlerTracker();
        var services = new ServiceCollection();
        services.AddSingleton(tracker);
        services.AddMediatR(configuration =>
        {
            configuration.RegisterRequestHandlersFromAssembly(
                typeof(ValidationBehaviorTests).Assembly);
            configuration.AddFluentValidationBehaviour(
                typeof(ValidationBehaviorTests).Assembly);
        });
        services.AddTransient<IValidator<ValidatedRequest>, ValidatedRequestValidator>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var act = () => mediator.Send(new ValidatedRequest(string.Empty));

        await act.Should().ThrowAsync<ValidationException>();
        tracker.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Send_ValidRequest_InvokesHandler()
    {
        var tracker = new ValidationHandlerTracker();
        var services = new ServiceCollection();
        services.AddSingleton(tracker);
        services.AddMediatR(configuration =>
        {
            configuration.RegisterRequestHandlersFromAssembly(
                typeof(ValidationBehaviorTests).Assembly);
            configuration.AddFluentValidationBehaviour(
                typeof(ValidationBehaviorTests).Assembly);
        });
        services.AddTransient<IValidator<ValidatedRequest>, ValidatedRequestValidator>();

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new ValidatedRequest("valid"));

        result.Should().Be("VALID");
        tracker.InvocationCount.Should().Be(1);
    }

    public sealed record ValidatedRequest(string Value) : IRequest<string>;

    public sealed class BeforeValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) => next();
    }

    public sealed class AfterValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) => next();
    }

    public sealed class ValidatedRequestHandler(ValidationHandlerTracker tracker)
        : IRequestHandler<ValidatedRequest, string>
    {
        public Task<string> Handle(ValidatedRequest request, CancellationToken cancellationToken)
        {
            tracker.InvocationCount++;
            return Task.FromResult(request.Value.ToUpperInvariant());
        }
    }

    private sealed class ValidatedRequestValidator : AbstractValidator<ValidatedRequest>
    {
        public ValidatedRequestValidator()
        {
            RuleFor(request => request.Value).NotEmpty();
        }
    }

    public sealed class ValidationHandlerTracker
    {
        public int InvocationCount { get; set; }
    }
}
