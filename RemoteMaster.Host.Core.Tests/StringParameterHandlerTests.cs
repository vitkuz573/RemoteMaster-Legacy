// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.ParameterHandlers;

namespace RemoteMaster.Host.Core.Tests;

public class StringParameterHandlerTests
{
    private readonly StringParameterHandler _handler = new();

    private static void SetupMockParameterGetValue(Mock<ILaunchParameter<string>> mockParameter, string parameterName, string? returnValue = null)
    {
        mockParameter
            .Setup(p => p.GetValue(It.IsAny<string[]>()))
            .Returns((string[] inputArgs) =>
            {
                foreach (var arg in inputArgs)
                {
                    if (arg.StartsWith($"--{parameterName}=", StringComparison.OrdinalIgnoreCase))
                    {
                        return arg[(arg.IndexOf('=') + 1)..].Trim();
                    }
                }

                return returnValue;
            });
    }

    #region CanHandle Tests

    [Fact]
    public void CanHandle_ShouldReturnTrue_ForStringParameter()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act
        var result = _handler.CanHandle(mockParameter.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanHandle_ShouldReturnFalse_ForNonStringParameter()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<bool>>();

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
    public void Handle_ShouldSetParameterValue_WhenArgumentIsPresent()
    {
        // Arrange
        var args = new[] { "--name=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();

        SetupMockParameterGetValue(mockParameter, "name");

        var handler = new StringParameterHandler();

        // Act
        handler.Handle(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue("John"), Times.Once);
    }

    [Fact]
    public void Handle_ShouldNotSetParameterValue_WhenArgumentIsAbsent()
    {
        // Arrange
        var args = new[] { "--other=Value" };
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act
        _handler.Handle(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Handle_ShouldBeCaseInsensitive_ForArgumentName()
    {
        // Arrange
        var args = new[] { "--Name=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();

        SetupMockParameterGetValue(mockParameter, "name");

        // Act
        _handler.Handle(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue("John"), Times.Once);
    }

    [Fact]
    public void Handle_ShouldThrowArgumentNullException_WhenArgsAreNull()
    {
        // Arrange
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _handler.Handle(null!, mockParameter.Object, "name"));
    }

    [Fact]
    public void Handle_ShouldThrowArgumentNullException_WhenParameterIsNull()
    {
        // Arrange
        var args = new[] { "--name=John" };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _handler.Handle(args, null!, "name"));
    }

    [Fact]
    public void Handle_ShouldThrowArgumentException_WhenNameIsNull()
    {
        // Arrange
        var args = new[] { "--name=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(args, mockParameter.Object, null!));
    }

    [Fact]
    public void Handle_ShouldThrowArgumentException_WhenNameIsEmpty()
    {
        // Arrange
        var args = new[] { "--name=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(args, mockParameter.Object, ""));
    }

    [Fact]
    public void Handle_ShouldThrowArgumentException_WhenNameIsWhitespace()
    {
        // Arrange
        var args = new[] { "--name=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(args, mockParameter.Object, "   "));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Handle_ShouldHandleSpecialCharactersInArgumentName()
    {
        // Arrange
        var args = new[] { "--name_123=John" };
        var mockParameter = new Mock<ILaunchParameter<string>>();

        SetupMockParameterGetValue(mockParameter, "name_123");

        // Act
        _handler.Handle(args, mockParameter.Object, "name_123");

        // Assert
        mockParameter.Verify(p => p.SetValue("John"), Times.Once);
    }

    [Fact]
    public void Handle_ShouldHandleSpecialCharactersInValue()
    {
        // Arrange
        var args = new[] { "--name=John_Doe@123" };
        var mockParameter = new Mock<ILaunchParameter<string>>();

        SetupMockParameterGetValue(mockParameter, "name");

        // Act
        _handler.Handle(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue("John_Doe@123"), Times.Once);
    }

    [Fact]
    public void Handle_ShouldNotThrow_WhenArgumentIsMalformed()
    {
        // Arrange
        var args = new[] { "--nameJohn" }; // No "=" in argument
        var mockParameter = new Mock<ILaunchParameter<string>>();

        // Act
        _handler.Handle(args, mockParameter.Object, "name");

        // Assert
        mockParameter.Verify(p => p.SetValue(It.IsAny<string>()), Times.Never);
    }

    #endregion
}
