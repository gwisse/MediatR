using FluentAssertions;
using MediatR.Abstractions;
using MediatR.Core;
using MediatR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Tests;

public class MediatorTests
{
    [Fact]
    public async Task Send_RequestWithRegisteredHandler_ReturnsHandlerResponse()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(new RequestExecutorCache());
        services.AddScoped<IMediator>(serviceProvider =>
            new Mediator(
                serviceProvider,
                serviceProvider.GetRequiredService<RequestExecutorCache>()));
        services.AddScoped<
            IRequestHandler<TestRequest, string>,
            TestRequestHandler>();

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        var request = new TestRequest("hello");

        // Act
        var result = await mediator.Send(request);

        // Assert
        result.Should().Be("HELLO");
    }

    [Fact]
    public async Task Send_RequestWithRegisteredHandler_InvokesHandlerOnce()
    {
        // Arrange
        var tracker = new HandlerTracker();

        var services = new ServiceCollection();

        services.AddSingleton(new RequestExecutorCache());
        services.AddScoped<IMediator>(serviceProvider =>
            new Mediator(
                serviceProvider,
                serviceProvider.GetRequiredService<RequestExecutorCache>()));
        services.AddSingleton(tracker);

        services.AddScoped<
            IRequestHandler<TrackingRequest, Unit>,
            TrackingRequestHandler>();

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new TrackingRequest());

        // Assert
        tracker.InvocationCount
            .Should().Be(1);
    }

    [Fact]
    public async Task Send_RequestWithNoHandler_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(new RequestExecutorCache());
        services.AddScoped<IMediator>(serviceProvider =>
            new Mediator(
                serviceProvider,
                serviceProvider.GetRequiredService<RequestExecutorCache>()));

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        var request = new UnhandledRequest();

        // Act
        var act = () => mediator.Send(request);

        // Assert
        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*No handler registered*");
    }

    [Fact]
    public void AddMediatR_WithMultipleHandlers_ThrowsDuringRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddMediatR(configuration =>
        {
            configuration.RegisterRequestHandlersFromAssembly(
                typeof(DependencyInjectionTests).Assembly);
            configuration.Services.AddScoped<
                IRequestHandler<TestRequest, string>>(
                _ => new TestRequestHandler());
        });

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Multiple handlers are registered*TestRequest*");
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(new RequestExecutorCache());
        services.AddScoped<IMediator>(serviceProvider =>
            new Mediator(
                serviceProvider,
                serviceProvider.GetRequiredService<RequestExecutorCache>()));

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        // Act
        var act = () => mediator.Send<string>(null!);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Send_RequestWithCancellationToken_PassesCancellationTokenToHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(new RequestExecutorCache());
        services.AddScoped<IMediator>(serviceProvider =>
            new Mediator(
                serviceProvider,
                serviceProvider.GetRequiredService<RequestExecutorCache>()));
        services.AddScoped<
            IRequestHandler<CancellationRequest, CancellationToken>,
            CancellationRequestHandler>();

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        // Act
        var result = await mediator.Send(
            new CancellationRequest(),
            cancellationToken);

        // Assert
        result.Should().Be(cancellationToken);
    }

    [Fact]
    public async Task Send_HandlerThrowsException_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(new RequestExecutorCache());
        services.AddScoped<IMediator>(serviceProvider =>
            new Mediator(
                serviceProvider,
                serviceProvider.GetRequiredService<RequestExecutorCache>()));
        services.AddScoped<
            IRequestHandler<FailingRequest, string>,
            FailingRequestHandler>();

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
    }

    [Fact]
    public async Task Send_SameRequestMultipleTimes_ExecutesHandlerForEachRequest()
    {
        // Arrange
        var tracker = new HandlerTracker();

        var services = new ServiceCollection();

        services.AddSingleton(new RequestExecutorCache());
        services.AddScoped<IMediator>(serviceProvider =>
            new Mediator(
                serviceProvider,
                serviceProvider.GetRequiredService<RequestExecutorCache>()));
        services.AddSingleton(tracker);

        services.AddScoped<
            IRequestHandler<TrackingRequest, Unit>,
            TrackingRequestHandler>();

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new TrackingRequest());
        await mediator.Send(new TrackingRequest());
        await mediator.Send(new TrackingRequest());

        // Assert
        tracker.InvocationCount
            .Should().Be(3);
    }

    [Fact]
    public async Task Send_DifferentRequestTypes_UsesCorrectHandler()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(new RequestExecutorCache());
        services.AddScoped<IMediator>(serviceProvider =>
            new Mediator(
                serviceProvider,
                serviceProvider.GetRequiredService<RequestExecutorCache>()));

        services.AddScoped<
            IRequestHandler<TestRequest, string>,
            TestRequestHandler>();

        services.AddScoped<
            IRequestHandler<IntRequest, int>,
            IntRequestHandler>();

        await using var provider =
            services.BuildServiceProvider();

        var mediator =
            provider.GetRequiredService<IMediator>();

        // Act
        var stringResult = await mediator.Send(
            new TestRequest("hello"));

        var intResult = await mediator.Send(
            new IntRequest(42));

        // Assert
        stringResult.Should().Be("HELLO");
        intResult.Should().Be(84);
    }

    // ========================================================
    // Test requests
    // ========================================================

    public sealed record TestRequest(
        string Value)
        : IRequest<string>;

    public sealed class TestRequestHandler
        : IRequestHandler<TestRequest, string>
    {
        public Task<string> Handle(
            TestRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                request.Value.ToUpperInvariant());
        }
    }

    public sealed record IntRequest(
        int Value)
        : IRequest<int>;

    public sealed class IntRequestHandler
        : IRequestHandler<IntRequest, int>
    {
        public Task<int> Handle(
            IntRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                request.Value * 2);
        }
    }

    public sealed record UnhandledRequest
        : IRequest<string>;

    public sealed record TrackingRequest
        : IRequest<Unit>;

    public sealed class TrackingRequestHandler
        : IRequestHandler<TrackingRequest, Unit>
    {
        private readonly HandlerTracker _tracker;

        public TrackingRequestHandler(
            HandlerTracker tracker)
        {
            _tracker = tracker;
        }

        public Task<Unit> Handle(
            TrackingRequest request,
            CancellationToken cancellationToken)
        {
            _tracker.InvocationCount++;

            return Task.FromResult(
                Unit.Value);
        }
    }

    public sealed record CancellationRequest
        : IRequest<CancellationToken>;

    public sealed class CancellationRequestHandler
        : IRequestHandler<
            CancellationRequest,
            CancellationToken>
    {
        public Task<CancellationToken> Handle(
            CancellationRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(
                cancellationToken);
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
    // Test helpers
    // ========================================================

    public sealed class HandlerTracker
    {
        public int InvocationCount { get; set; }
    }

    public sealed class TestException
        : Exception
    {
        public TestException(
            string message)
            : base(message)
        {
        }
    }

    public readonly record struct Unit
    {
        public static Unit Value => new();
    }
}