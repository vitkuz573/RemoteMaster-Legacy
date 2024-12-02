// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.ParameterSerializers;

namespace RemoteMaster.Host.Core.Tests;

public class BooleanParameterSerializerTests
{
    private readonly BooleanParameterSerializer _serializer = new();

    #region CanHandle Tests

    [Fact]
    public void CanHandle_ShouldReturnTrue_ForBooleanParameter()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act
        var result = _serializer.CanHandle(mockParameter.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_ForNonBooleanParameter()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act
        var result = _serializer.CanHandle(mockParameter.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_ShouldThrowArgumentNullException_WhenParameterIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.CanHandle(null!));
    }

    #endregion

    #region Serialize Tests

    [Fact]
    public void Serialize_ShouldReturnArgument_WhenValueIsTrue()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.Value).Returns(true);

        // Act
        var result = _serializer.Serialize(mockParameter.Object, "enable-feature");

        // Assert
        Assert.Equal("--enable-feature", result);
    }

    [Fact]
    public void Serialize_ShouldReturnNull_WhenValueIsFalse()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.Value).Returns(false);

        // Act
        var result = _serializer.Serialize(mockParameter.Object, "enable-feature");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Serialize_ShouldThrowArgumentException_ForInvalidParameterType()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.Serialize(mockParameter.Object, "enable-feature"));
    }

    #endregion

    #region Deserialize Tests

    [Fact]
    public void Deserialize_ShouldSetTrue_WhenArgumentIsPresent()
    {
        // Arrange
        var args = new[] { "--enable-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<bool>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(true), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldSetFalse_WhenArgumentIsAbsent()
    {
        // Arrange
        var args = new[] { "--other-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<bool>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(false), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldBeCaseInsensitive_ForArgumentName()
    {
        // Arrange
        var args = new[] { "--Enable-Feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<bool>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(true), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldThrowArgumentException_WhenParameterTypeIsInvalid()
    {
        // Arrange
        var args = new[] { "--enable-feature" };
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.Deserialize(args, mockParameter.Object, "enable-feature"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Deserialize_ShouldIgnoreDuplicateArguments()
    {
        // Arrange
        var args = new[] { "--enable-feature", "--enable-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<bool>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(true), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldHandleSpecialCharactersInArgumentName()
    {
        // Arrange
        var args = new[] { "--enable-feature123" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<bool>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "enable-feature123");

        // Assert
        mockParameter.Verify(p => p.SetValue(true), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldIgnoreArgumentsThatStartWithSimilarPrefix()
    {
        // Arrange
        var args = new[] { "--enable-feature-long" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<bool>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(false), Times.Once);
    }

    #endregion
}
