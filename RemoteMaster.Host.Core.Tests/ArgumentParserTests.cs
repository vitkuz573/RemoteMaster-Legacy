// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class ArgumentParserTests
{
    private readonly Mock<IArgumentSerializer> _mockSerializer;
    private readonly Mock<IHelpService> _mockHelpService;
    private readonly ArgumentParser _parser;

    public ArgumentParserTests()
    {
        _mockSerializer = new Mock<IArgumentSerializer>();
        _mockHelpService = new Mock<IHelpService>();

        _parser = new ArgumentParser(_mockSerializer.Object, _mockHelpService.Object);
    }

    [Fact]
    public void ParseArguments_NoArguments_ReturnsNull()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        Assert.Null(result);
        _mockHelpService.Verify(h => h.PrintHelp(It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public void ParseArguments_WithHelpFlag_PrintsHelpAndReturnsNull()
    {
        // Arrange
        var args = new[] { "--help", "--launch-mode=testMode" };

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        _mockHelpService.Verify(h => h.PrintHelp("testMode"), Times.Once);
        Assert.Null(result);
    }

    [Fact]
    public void ParseArguments_InvalidArguments_PrintsGeneralHelpAndThrows()
    {
        // Arrange
        var args = new[] { "--launch-mode=invalidMode" };

        _mockSerializer.Setup(s => s.Deserialize(args))
            .Throws(new ArgumentException());

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _parser.ParseArguments(args));
        _mockHelpService.Verify(h => h.PrintHelp(null), Times.Once);
    }

    [Fact]
    public void ParseArguments_ValidArguments_ReturnsLaunchMode()
    {
        // Arrange
        var args = new[] { "--launch-mode=testMode", "--param1=value1" };
        var mockLaunchMode = new Mock<LaunchModeBase>();

        _mockSerializer.Setup(s => s.Deserialize(args))
            .Returns(mockLaunchMode.Object);

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockLaunchMode.Object, result);
        _mockHelpService.Verify(h => h.PrintHelp(It.IsAny<string?>()), Times.Never);
    }
}
