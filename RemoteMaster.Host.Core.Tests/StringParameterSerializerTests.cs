// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.ParameterSerializers;

namespace RemoteMaster.Host.Core.Tests;

public class StringParameterSerializerTests
{
    private readonly StringParameterSerializer _serializer = new();

    #region CanHandle Tests

    [Fact]
    public void CanHandle_ShouldReturnTrue_ForStringParameter()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act
        var result = _serializer.CanHandle(mockParameter.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_ForNonStringParameter()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<bool>>();

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
    public void Serialize_ShouldReturnArgument_WhenValueIsNonEmptyString()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.Value).Returns("John");

        // Act
        var result = _serializer.Serialize(mockParameter.Object, "name");

        // Assert
        Assert.Equal("--name=John", result);
    }

    [Fact]
    public void Serialize_ShouldReturnNull_WhenValueIsNull()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.Value).Returns((string?)null);

        // Act
        var result = _serializer.Serialize(mockParameter.Object, "name");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Serialize_ShouldReturnNull_WhenValueIsEmptyString()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.Value).Returns("");

        // Act
        var result = _serializer.Serialize(mockParameter.Object, "name");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Serialize_ShouldThrowArgumentException_ForInvalidParameterType()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.Serialize(mockParameter.Object, "name"));
    }

    #endregion

    #region Deserialize Tests

    [Fact]
    public void Deserialize_ShouldSetParameterValue_WhenArgumentIsPresent()
    {
        // Arrange
        var args = new[] { "--name=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<string>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue("John"), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldSetNull_WhenArgumentIsAbsent()
    {
        // Arrange
        var args = new[] { "--other=Value" };
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<string?>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue(null), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldBeCaseInsensitive_ForArgumentName()
    {
        // Arrange
        var args = new[] { "--Name=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<string>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue("John"), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldThrowArgumentException_WhenParameterTypeIsInvalid()
    {
        // Arrange
        var args = new[] { "--name=John" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _serializer.Deserialize(args, mockParameter.Object, "name"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Deserialize_ShouldHandleSpecialCharactersInArgumentName()
    {
        // Arrange
        var args = new[] { "--name_123=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<string>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "name_123");

        // Assert
        mockParameter.Verify(p => p.SetValue("John"), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldHandleSpecialCharactersInValue()
    {
        // Arrange
        var args = new[] { "--name=John_Doe@123" };
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<string>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue("John_Doe@123"), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldSetNull_WhenArgumentIsMalformed()
    {
        // Arrange
        var args = new[] { "--nameJohn" }; // No "=" in argument
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<string?>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue(null), Times.Once);
    }

    [Fact]
    public void Deserialize_ShouldIgnoreDuplicateArguments()
    {
        // Arrange
        var args = new[] { "--name=John", "--name=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<string>()));

        // Act
        _serializer.Deserialize(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue("John"), Times.Once);
    }

    #endregion
}
