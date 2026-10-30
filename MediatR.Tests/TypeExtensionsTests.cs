using FluentAssertions;
using MediatR.Abstractions;
using MediatR.Extensions;

namespace MediatR.Tests;

public class TypeExtensionsTests
{
    [Theory]
    [InlineData(typeof(bool), "bool")]
    [InlineData(typeof(byte), "byte")]
    [InlineData(typeof(char), "char")]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(double), "double")]
    [InlineData(typeof(float), "float")]
    [InlineData(typeof(int), "int")]
    [InlineData(typeof(long), "long")]
    [InlineData(typeof(sbyte), "sbyte")]
    [InlineData(typeof(short), "short")]
    [InlineData(typeof(string), "string")]
    [InlineData(typeof(uint), "uint")]
    [InlineData(typeof(ulong), "ulong")]
    [InlineData(typeof(ushort), "ushort")]
    public void FormatTypeName_CSharpAliasType_ReturnsAlias(Type type, string expectedName)
    {
        // Act
        var typeName = type.FormatTypeName();

        // Assert
        typeName.Should().Be(expectedName);
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
