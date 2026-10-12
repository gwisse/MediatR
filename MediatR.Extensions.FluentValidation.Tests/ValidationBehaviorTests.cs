using FluentAssertions;
using FluentValidation;
using MediatR.Abstractions;
using MediatR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Extensions.FluentValidation.Tests;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public void AddFluentValidation_WithConfiguredBehavior_RegistersAtConfigurationPosition()
    {
        // Arrange
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

        // Act
        var behaviors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descriptor => descriptor.ImplementationType)
            .ToArray();

        // Assert
        behaviors.Should().Equal(
            typeof(BeforeValidationBehavior<,>),
            typeof(ValidationBehavior<,>),
            typeof(AfterValidationBehavior<,>));
    }

    [Fact]
    public async Task Send_InvalidRequest_ThrowsValidationExceptionWithoutCallingHandler()
    {
        // Arrange
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

        // Act
        var act = () => mediator.Send(new ValidatedRequest(string.Empty));

        // Assert
        await act.Should().ThrowAsync<ValidationException>();

        tracker.InvocationCount.Should().Be(0);
    }

    [Fact]
    public async Task Send_ValidRequest_InvokesHandler()
    {
        // Arrange
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

        // Act
        var result = await mediator.Send(new ValidatedRequest("valid"));

        // Assert
        result.Should().Be("VALID");
        tracker.InvocationCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_RunsValidatorsInParallelWithSeparateContexts()
    {
        // Arrange
        var tracker = new ValidatorConcurrencyTracker();
        var behavior = new ValidationBehavior<ConcurrentRequest, string>(
        [
            new FirstConcurrentValidator(tracker),
            new SecondConcurrentValidator(tracker)
        ]);

        // Act
        var response = await behavior.Handle(
            new ConcurrentRequest("valid"),
            () => Task.FromResult("handled"),
            CancellationToken.None);

        // Assert
        response.Should().Be("handled");
        tracker.MaximumConcurrentValidations.Should().Be(2);
        tracker.Contexts.Should().HaveCount(2);
        tracker.Contexts[0].Should().NotBeSameAs(tracker.Contexts[1]);
    }

    [Fact]
    public async Task Handle_WithMultipleFailingValidators_AggregatesFailures()
    {
        // Arrange
        var behavior = new ValidationBehavior<ValidatedRequest, string>(
        [
            new FirstFailingValidator(),
            new SecondFailingValidator()
        ]);

        // Act
        var act = () => behavior.Handle(
            new ValidatedRequest("invalid"),
            () => Task.FromResult("should not execute"),
            CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors
            .Select(failure => failure.ErrorMessage)
            .Should()
            .BeEquivalentTo("First validation failed", "Second validation failed");
    }

    [Fact]
    public async Task Handle_WithCancelledToken_PropagatesCancellationAndSkipsHandler()
    {
        // Arrange
        var behavior = new ValidationBehavior<ConcurrentRequest, string>(
        [
            new CancellationValidator()
        ]);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var handlerExecuted = false;

        // Act
        var act = () => behavior.Handle(
            new ConcurrentRequest("valid"),
            () =>
            {
                handlerExecuted = true;
                return Task.FromResult("handled");
            },
            cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        handlerExecuted.Should().BeFalse();
    }

    public sealed record ValidatedRequest(string Value) : IRequest<string>;

    private sealed record ConcurrentRequest(string Value) : IRequest<string>;

    private sealed class BeforeValidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) => next();
    }

    private sealed class AfterValidationBehavior<TRequest, TResponse>
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

    private sealed class FirstFailingValidator : AbstractValidator<ValidatedRequest>
    {
        public FirstFailingValidator()
        {
            RuleFor(request => request.Value)
                .Must(_ => false)
                .WithMessage("First validation failed");
        }
    }

    private sealed class SecondFailingValidator : AbstractValidator<ValidatedRequest>
    {
        public SecondFailingValidator()
        {
            RuleFor(request => request.Value)
                .Must(_ => false)
                .WithMessage("Second validation failed");
        }
    }

    private sealed class CancellationValidator : AbstractValidator<ConcurrentRequest>
    {
        public CancellationValidator()
        {
            RuleFor(request => request.Value)
                .MustAsync((_, cancellationToken) =>
                    Task.FromCanceled<bool>(cancellationToken));
        }
    }

    private sealed class FirstConcurrentValidator : AbstractValidator<ConcurrentRequest>
    {
        public FirstConcurrentValidator(ValidatorConcurrencyTracker tracker)
        {
            RuleFor(request => request.Value).CustomAsync(
                (request, context, cancellationToken) =>
                    tracker.ValidateAsync(context, cancellationToken));
        }
    }

    private sealed class SecondConcurrentValidator : AbstractValidator<ConcurrentRequest>
    {
        public SecondConcurrentValidator(ValidatorConcurrencyTracker tracker)
        {
            RuleFor(request => request.Value).CustomAsync(
                (request, context, cancellationToken) =>
                    tracker.ValidateAsync(context, cancellationToken));
        }
    }

    public sealed class ValidationHandlerTracker
    {
        public int InvocationCount { get; set; }
    }

    private sealed class ValidatorConcurrencyTracker
    {
        private int _activeValidations;
        private int _maximumConcurrentValidations;
        private readonly System.Collections.Concurrent.ConcurrentBag<object> _contexts = [];

        public int MaximumConcurrentValidations => _maximumConcurrentValidations;
        public object[] Contexts => _contexts.ToArray();

        public async Task ValidateAsync(object context, CancellationToken cancellationToken)
        {
            _contexts.Add(context);
            var active = Interlocked.Increment(ref _activeValidations);
            UpdateMaximum(active);

            try
            {
                await Task.Delay(20, cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _activeValidations);
            }
        }

        private void UpdateMaximum(int active)
        {
            while (true)
            {
                var currentMaximum = _maximumConcurrentValidations;
                if (active <= currentMaximum ||
                    Interlocked.CompareExchange(ref _maximumConcurrentValidations, active, currentMaximum) == currentMaximum)
                {
                    return;
                }
            }
        }
    }
}
