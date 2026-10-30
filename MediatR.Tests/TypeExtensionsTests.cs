using FluentAssertions;
using MediatR.Abstractions;
using MediatR.Extensions;

namespace MediatR.Tests;

public class TypeExtensionsTests
{
    [Fact]
    public void FormatTypeName_CSharpAliasType_ReturnsAlias()
    {
        // Act
        var typeName = typeof(string).FormatTypeName();

        // Assert
        typeName.Should().Be("string");
    }

    [Fact]
    public void FormatTypeName_CustomType_ReturnsUnqualifiedTypeName()
    {
        // Act
        var typeName = typeof(CustomType).FormatTypeName();

        // Assert
        typeName.Should().Be(nameof(CustomType));
    }

    [Fact]
    public void IsRequestHandler_RequestHandlerInterface_ReturnsTrue()
    {
        // Act
        var result = typeof(IRequestHandler<CustomRequest, string>).IsRequestHandler();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsRequestHandler_OtherGenericInterface_ReturnsFalse()
    {
        // Act
        var result = typeof(IList<string>).IsRequestHandler();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsRequestHandler_NonGenericType_ReturnsFalse()
    {
        // Act
        var result = typeof(string).IsRequestHandler();

        // Assert
        result.Should().BeFalse();
    }

    private sealed record CustomRequest : IRequest<string>;

    private sealed class CustomType;
}
