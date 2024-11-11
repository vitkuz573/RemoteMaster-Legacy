// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.ParameterHandlers;

namespace RemoteMaster.Host.Core.Tests;

public class BooleanParameterHandlerTests
{
    private readonly BooleanParameterHandler _handler = new();

    private static void SetupMockParameterGetValue(Mock<ILaunchParameter<bool>> mockParameter, string parameterName)
    {
        mockParameter
            .Setup(p => p.GetValue(It.IsAny<string[]>()))
            .Returns((string[] inputArgs) =>
            {
                return inputArgs.Any(arg => arg.Equals($"--{parameterName}", StringComparison.OrdinalIgnoreCase));
            });
    }

    #region CanHandle Tests

    [Fact]
    public void CanHandle_ShouldReturnTrue_ForBooleanParameter()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act
        var result = _handler.CanHandle(mockParameter.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_ForNonBooleanParameter()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act
        var result = _handler.CanHandle(mockParameter.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanHandle_ShouldThrowArgumentNullException_WhenParameterIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _handler.CanHandle(null!));
    }

    #endregion

    #region Handle Tests

    [Fact]
    public void Handle_ShouldSetTrue_WhenArgumentIsPresent()
    {
        // Arrange
        var args = new[] { "--enable-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<string>()));

        SetupMockParameterGetValue(mockParameter, "enable-feature");

        // Act
        _handler.Handle(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(true), Times.Once);
    }

    [Fact]
    public void Handle_ShouldSetFalse_WhenArgumentIsAbsent()
    {
        // Arrange
        var args = new[] { "--other-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();
        mockParameter.Setup(p => p.SetValue(It.IsAny<string>()));

        // Act
        _handler.Handle(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(false), Times.Once);
    }

    [Fact]
    public void Handle_ShouldBeCaseInsensitive_ForArgumentName()
    {
        // Arrange
        var args = new[] { "--Enable-Feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        SetupMockParameterGetValue(mockParameter, "enable-feature");

        // Act
        _handler.Handle(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(true), Times.Once);
    }

    [Fact]
    public void Handle_ShouldHandleMultipleArgumentsCorrectly()
    {
        // Arrange
        var args = new[] { "--enable-feature", "--another-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        SetupMockParameterGetValue(mockParameter, "enable-feature");

        // Act
        _handler.Handle(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(true), Times.Once);
    }

    [Fact]
    public void Handle_ShouldThrowArgumentNullException_WhenArgsAreNull()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _handler.Handle(null!, mockParameter.Object, "enable-feature"));
    }

    [Fact]
    public void Handle_ShouldThrowArgumentNullException_WhenParameterIsNull()
    {
        // Arrange
        var args = new[] { "--enable-feature" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _handler.Handle(args, null!, "enable-feature"));
    }

    [Fact]
    public void Handle_ShouldThrowArgumentException_WhenNameIsNull()
    {
        // Arrange
        var args = new[] { "--enable-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(args, mockParameter.Object, null!));
    }

    [Fact]
    public void Handle_ShouldThrowArgumentException_WhenNameIsEmpty()
    {
        // Arrange
        var args = new[] { "--enable-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(args, mockParameter.Object, ""));
    }

    [Fact]
    public void Handle_ShouldThrowArgumentException_WhenNameIsWhitespace()
    {
        // Arrange
        var args = new[] { "--enable-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(args, mockParameter.Object, "   "));
    }

    [Fact]
    public void Handle_ShouldNotChangeValue_WhenNoMatchingHandler()
    {
        // Arrange
        var args = new[] { "--other-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act
        _handler.Handle(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(false), Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Handle_ShouldIgnoreDuplicateArguments()
    {
        // Arrange
        var args = new[] { "--enable-feature", "--enable-feature" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        SetupMockParameterGetValue(mockParameter, "enable-feature");

        // Act
        _handler.Handle(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(true), Times.Once);
    }

    [Fact]
    public void Handle_ShouldHandleSpecialCharactersInArgumentName()
    {
        // Arrange
        var args = new[] { "--enable-feature123" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        SetupMockParameterGetValue(mockParameter, "enable-feature123");

        // Act
        _handler.Handle(args, mockParameter.Object, "enable-feature123");

        // Assert
        mockParameter.Verify(p => p.SetValue(true), Times.Once);
    }

    [Fact]
    public void Handle_ShouldIgnoreArgumentsThatStartWithSimilarPrefix()
    {
        // Arrange
        var args = new[] { "--enable-feature-long" };
        var mockParameter = new Mock<ILaunchParameter<bool>>();

        // Act
        _handler.Handle(args, mockParameter.Object, "enable-feature");

        // Assert
        mockParameter.Verify(p => p.SetValue(false), Times.Once);
    }

    #endregion
}
