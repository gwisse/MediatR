using FluentAssertions;
using MediatR.Abstractions;
using MediatR.Core;
using MediatR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddMediator_RegistersMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(
            typeof(DependencyInjectionTests).Assembly);

        // Assert
        var descriptor = services
            .SingleOrDefault(x =>
                x.ServiceType == typeof(IMediator));

        descriptor.Should().NotBeNull();

        descriptor!.ImplementationFactory
            .Should().NotBeNull();
    }

    [Fact]
    public void AddMediator_RegistersMediatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(
            typeof(DependencyInjectionTests).Assembly);

        // Assert
        var descriptor = services.Single(x =>
            x.ServiceType == typeof(IMediator));

        descriptor.Lifetime
            .Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task AddMediator_RegistersExecutorCacheAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddMediator(
            typeof(DependencyInjectionTests).Assembly);

        await using var provider =
            services.BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<RequestExecutorCache>();
        var second = provider.GetRequiredService<RequestExecutorCache>();

        // Assert
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task AddMediator_RegistersRequestHandler_HandlerCanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddMediator(
            typeof(DependencyInjectionTests).Assembly);

        await using var provider =
            services.BuildServiceProvider();

        // Act
        var handler = provider.GetService<
            IRequestHandler<TestRequest, string>>();

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<TestRequestHandler>();
    }

    [Fact]
    public void AddMediator_RegistersRequestHandlerAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(
            typeof(DependencyInjectionTests).Assembly);

        // Assert
        var descriptor = services.Single(x =>
            x.ServiceType ==
            typeof(IRequestHandler<TestRequest, string>));

        descriptor.Lifetime
            .Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task AddMediator_DoesNotRegisterPipelineBehaviorsFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddMediator(
            typeof(DependencyInjectionTests).Assembly);

        await using var provider =
            services.BuildServiceProvider();

        // Act
        var behaviors = provider.GetServices<
            IPipelineBehavior<TestRequest, string>>();

        // Assert
        behaviors.Should().BeEmpty();
    }

    [Fact]
    public async Task AddMediator_ResolvesPipelineBehaviorRegisteredWithDependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddMediator(
            typeof(DependencyInjectionTests).Assembly);
        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(TestBehavior<,>));

        await using var provider =
            services.BuildServiceProvider();

        var behaviors = provider.GetServices<
            IPipelineBehavior<TestRequest, string>>();

        behaviors.Should()
            .ContainSingle()
            .Which
            .Should()
            .BeOfType<TestBehavior<TestRequest, string>>();
    }

    [Fact]
    public void AddMediator_WithoutAssemblies_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddMediator();

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("assemblies");
    }

    [Fact]
    public void AddMediator_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () =>
            services.AddMediator(
                typeof(DependencyInjectionTests).Assembly);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddMediator_WithDuplicateAssembly_DoesNotDuplicateHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        var assembly =
            typeof(DependencyInjectionTests).Assembly;

        // Act
        services.AddMediator(
            assembly,
            assembly,
            assembly);

        await using var provider =
            services.BuildServiceProvider();

        var handlers = provider.GetServices<
            IRequestHandler<TestRequest, string>>();

        // Assert
        handlers.Should().ContainSingle();
    }

    // ========================================================
    // Test types
    // ========================================================

    public sealed record TestRequest
        : IRequest<string>;

    public sealed class TestRequestHandler
        : IRequestHandler<TestRequest, string>
    {
        public Task<string> Handle(
            TestRequest request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult("test");
        }
    }

    public sealed class TestBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            return next();
        }
    }
}