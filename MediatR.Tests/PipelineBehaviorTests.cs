using FluentAssertions;
using MediatR.Abstractions;
using MediatR.Core;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Tests;

public class PipelineBehaviorTests
{
    [Fact]
    public async Task Send_RequestWithBehavior_ExecutesBehaviorAroundHandler()
    {
        // Arrange
        var execution = new List<string>();

        var services = CreateServices();

        services.AddTransient<
            IRequestHandler<TestRequest, string>>(
            _ => new TestRequestHandler(execution));

        services.AddTransient<
            IPipelineBehavior<TestRequest, string>>(
            _ => new RecordingBehavior(execution));

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new TestRequest());

        // Assert
        execution.Should().Equal(
            "Behavior.Before",
            "Handler",
            "Behavior.After");
    }

    [Fact]
    public async Task Send_RequestWithMultipleBehaviors_ExecutesBehaviorsInRegistrationOrder()
    {
        // Arrange
        var execution = new List<string>();

        var services = CreateServices();

        services.AddTransient<
            IRequestHandler<TestRequest, string>>(
            _ => new TestRequestHandler(execution));

        services.AddTransient<
            IPipelineBehavior<TestRequest, string>>(
            _ => new RecordingBehavior(
                execution,
                "First"));

        services.AddTransient<
            IPipelineBehavior<TestRequest, string>>(
            _ => new RecordingBehavior(
                execution,
                "Second"));

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new TestRequest());

        // Assert
        execution.Should().Equal(
            "First.Before",
            "Second.Before",
            "Handler",
            "Second.After",
            "First.After");
    }

    [Fact]
    public async Task Send_BehaviorDoesNotCallNext_DoesNotExecuteHandler()
    {
        // Arrange
        var handlerExecuted = false;

        var services = CreateServices();

        services.AddTransient<
            IRequestHandler<
                ShortCircuitRequest,
                string>>(
            _ => new ShortCircuitRequestHandler(
                () => handlerExecuted = true));

        services.AddTransient<
            IPipelineBehavior<
                ShortCircuitRequest,
                string>>(
            _ => new ShortCircuitBehavior());

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        // Act
        var result = await mediator.Send(
            new ShortCircuitRequest());

        // Assert
        result.Should().Be("SHORT-CIRCUITED");
        handlerExecuted.Should().BeFalse();
    }

    [Fact]
    public async Task Send_BehaviorAndHandlerThrowException_PropagatesExceptionThroughPipeline()
    {
        // Arrange
        var execution = new List<string>();

        var services = CreateServices();

        services.AddTransient<
            IRequestHandler<FailingRequest, string>>(
            _ => new FailingRequestHandler());

        services.AddTransient<
            IPipelineBehavior<FailingRequest, string>>(
            _ => new FailingRecordingBehavior(
                execution));

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Send(
            new FailingRequest());

        // Assert
        await act.Should()
            .ThrowAsync<TestException>()
            .WithMessage("Handler failed");

        execution.Should().Equal(
            "Behavior.Before");
    }

    // ========================================================
    // Test setup
    // ========================================================

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new RequestExecutorCache());
        services.AddScoped<IMediator>(serviceProvider =>
            new Mediator(
                serviceProvider,
                serviceProvider.GetRequiredService<RequestExecutorCache>()));

        return services;
    }

    // ========================================================
    // Test requests / handlers
    // ========================================================

    public sealed record TestRequest
        : IRequest<string>;

    public sealed class TestRequestHandler
        : IRequestHandler<TestRequest, string>
    {
        private readonly List<string> _execution;

        public TestRequestHandler(
            List<string> execution)
        {
            _execution = execution;
        }

        public Task<string> Handle(
            TestRequest request,
            CancellationToken cancellationToken)
        {
            _execution.Add("Handler");

            return Task.FromResult("handler");
        }
    }

    public sealed record ShortCircuitRequest
        : IRequest<string>;

    public sealed class ShortCircuitRequestHandler
        : IRequestHandler<
            ShortCircuitRequest,
            string>
    {
        private readonly Action _onExecuted;

        public ShortCircuitRequestHandler(
            Action onExecuted)
        {
            _onExecuted = onExecuted;
        }

        public Task<string> Handle(
            ShortCircuitRequest request,
            CancellationToken cancellationToken)
        {
            _onExecuted();

            return Task.FromResult("handler");
        }
    }

    public sealed record FailingRequest
        : IRequest<string>;

    public sealed class FailingRequestHandler
        : IRequestHandler<
            FailingRequest,
            string>
    {
        public Task<string> Handle(
            FailingRequest request,
            CancellationToken cancellationToken)
        {
            throw new TestException(
                "Handler failed");
        }
    }

    // ========================================================
    // Test behaviors
    // ========================================================

    public sealed class RecordingBehavior
        : IPipelineBehavior<
            TestRequest,
            string>
    {
        private readonly List<string> _execution;
        private readonly string _name;

        public RecordingBehavior(
            List<string> execution,
            string name = "Behavior")
        {
            _execution = execution;
            _name = name;
        }

        public async Task<string> Handle(
            TestRequest request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            _execution.Add(
                $"{_name}.Before");

            var result = await next();

            _execution.Add(
                $"{_name}.After");

            return result;
        }
    }

    public sealed class FailingRecordingBehavior
        : IPipelineBehavior<
            FailingRequest,
            string>
    {
        private readonly List<string> _execution;

        public FailingRecordingBehavior(
            List<string> execution)
        {
            _execution = execution;
        }

        public async Task<string> Handle(
            FailingRequest request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            _execution.Add(
                "Behavior.Before");

            var result = await next();

            _execution.Add(
                "Behavior.After");

            return result;
        }
    }

    public sealed class ShortCircuitBehavior
        : IPipelineBehavior<
            ShortCircuitRequest,
            string>
    {
        public Task<string> Handle(
            ShortCircuitRequest request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                "SHORT-CIRCUITED");
        }
    }

    // ========================================================
    // Test helpers
    // ========================================================

    public sealed class TestException
        : Exception
    {
        public TestException(
            string message)
            : base(message)
        {
        }
    }
}