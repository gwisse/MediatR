using FluentAssertions;
using System.Reflection;
using MediatR.Abstractions;
using MediatR.Core;
using MediatR.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace MediatR.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddMediatR_RegistersMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        RegisterTestAssembly(services);

        // Assert
        var descriptor = services
            .SingleOrDefault(x =>
                x.ServiceType == typeof(IMediator));

        descriptor.Should().NotBeNull();

        descriptor!.ImplementationFactory
            .Should().NotBeNull();
    }

    [Fact]
    public void AddMediatR_RegistersMediatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        RegisterTestAssembly(services);

        // Assert
        var descriptor = services.Single(x =>
            x.ServiceType == typeof(IMediator));

        descriptor.Lifetime
            .Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task AddMediatR_RegistersExecutorCacheAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        RegisterTestAssembly(services);

        await using var provider =
            services.BuildServiceProvider();

        // Act
        var first = provider.GetRequiredService<RequestExecutorCache>();
        var second = provider.GetRequiredService<RequestExecutorCache>();

        // Assert
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task AddMediatR_RegistersRequestHandler_HandlerCanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();

        RegisterTestAssembly(services);

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
    public void AddMediatR_RegistersRequestHandlerAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        RegisterTestAssembly(services);

        // Assert
        var descriptor = services.Single(x =>
            x.ServiceType ==
            typeof(IRequestHandler<TestRequest, string>));

        descriptor.Lifetime
            .Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task AddMediatR_DoesNotRegisterPipelineBehaviorsFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();

        RegisterTestAssembly(services);

        await using var provider =
            services.BuildServiceProvider();

        // Act
        var behaviors = provider.GetServices<
            IPipelineBehavior<TestRequest, string>>();

        // Assert
        behaviors.Should().BeEmpty();
    }

    [Fact]
    public async Task AddMediatR_ResolvesPipelineBehaviorRegisteredWithDependencyInjection()
    {
        // Arrange
        var services = new ServiceCollection();

        RegisterTestAssembly(services);
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
    public async Task AddMediatR_RegistersBehaviorFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterRequestHandlersFromAssembly(
                typeof(DependencyInjectionTests).Assembly);
            configuration.AddBehavior(typeof(TestClosedBehavior), ServiceLifetime.Scoped);
        });

        await using var provider =
            services.BuildServiceProvider();

        // Act
        var behavior = provider.GetRequiredService<
            IPipelineBehavior<TestRequest, string>>();

        // Assert
        behavior.Should().BeOfType<TestClosedBehavior>();
        services.Single(x =>
                x.ServiceType == typeof(IPipelineBehavior<TestRequest, string>))
            .Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public async Task AddMediatR_RegistersOpenGenericBehaviorFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddMediatR(configuration =>
        {
            configuration.RegisterRequestHandlersFromAssembly(
                typeof(DependencyInjectionTests).Assembly);
            configuration.AddBehavior(typeof(TestBehavior<,>));
        });

        await using var provider =
            services.BuildServiceProvider();

        // Act
        var behavior = provider.GetRequiredService<
            IPipelineBehavior<TestRequest, string>>();

        // Assert
        behavior.Should().BeOfType<TestBehavior<TestRequest, string>>();
        services.Single(x =>
                x.ServiceType == typeof(IPipelineBehavior<,>))
            .Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddMediatR_WithoutHandlerAssembly_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddMediatR(_ => { });

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*RegisterRequestHandlersFromAssembly*");
    }

    [Fact]
    public void AddMediatR_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () =>
            services.AddMediatR(configuration =>
                configuration.RegisterRequestHandlersFromAssembly(
                    typeof(DependencyInjectionTests).Assembly));

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMediatR_WithPartiallyLoadableAssembly_FailsFastWithLoaderErrors()
    {
        var services = new ServiceCollection();
        var loaderException = new TypeLoadException("Missing dependency");
        var assembly = new PartiallyLoadableAssembly(
            new ReflectionTypeLoadException([], [loaderException]));

        var act = () => services.AddMediatR(configuration =>
            configuration.RegisterRequestHandlersFromAssembly(assembly));

        var exception = act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*PartiallyLoadableAssembly*")
            .Which;

        exception.InnerException.Should()
            .BeOfType<AggregateException>()
            .Which.InnerExceptions.Should()
            .ContainSingle()
            .Which.Should()
            .BeSameAs(loaderException);
    }

    [Fact]
    public async Task AddMediatR_WithDuplicateAssembly_DoesNotDuplicateHandlers()
    {
        // Arrange
        var services = new ServiceCollection();

        var assembly =
            typeof(DependencyInjectionTests).Assembly;

        // Act
        services.AddMediatR(configuration =>
        {
            configuration.RegisterRequestHandlersFromAssembly(assembly);
            configuration.RegisterRequestHandlersFromAssembly(assembly);
            configuration.RegisterRequestHandlersFromAssembly(assembly);
        });

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

    private static void RegisterTestAssembly(IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterRequestHandlersFromAssembly(
                typeof(DependencyInjectionTests).Assembly));
    }

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

    public sealed class TestClosedBehavior
        : IPipelineBehavior<TestRequest, string>
    {
        public Task<string> Handle(
            TestRequest request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            return next();
        }
    }

    private sealed class PartiallyLoadableAssembly(
        ReflectionTypeLoadException exception) : Assembly
    {
        public override string FullName => "PartiallyLoadableAssembly";

        public override Type[] GetTypes() => throw exception;
    }
}