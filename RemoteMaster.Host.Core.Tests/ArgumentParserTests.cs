// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Exceptions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class ArgumentParserTests
{
    private readonly Mock<ILaunchModeProvider> _mockModeProvider;
    private readonly Mock<IHelpService> _mockHelpService;
    private readonly ArgumentParser _parser;

    public ArgumentParserTests()
    {
        _mockModeProvider = new Mock<ILaunchModeProvider>();
        _mockHelpService = new Mock<IHelpService>();
        _parser = new ArgumentParser(_mockModeProvider.Object, _mockHelpService.Object);
    }

    [Fact]
    public void ParseArguments_NoArguments_PrintsHelpAndReturnsNull()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        _mockHelpService.Verify(h => h.PrintHelp(null), Times.Once);
        Assert.Null(result);
    }

    [Fact]
    public void ParseArguments_InvalidMode_PrintsHelpAndReturnsNull()
    {
        // Arrange
        var args = new[] { "--launch-mode=invalidMode" };
        _mockModeProvider.Setup(m => m.GetAvailableModes()).Returns(new Dictionary<string, LaunchModeBase>());

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        _mockHelpService.Verify(h => h.SuggestSimilarModes("invalidMode"), Times.Once);
        Assert.Null(result);
    }

    [Fact]
    public void ParseArguments_ValidMode_MissingRequiredParameter_ThrowsException()
    {
        // Arrange
        var mode = new TestLaunchMode("testMode", "Test mode", new Dictionary<string, ILaunchParameter>
        {
            { "param1", new LaunchParameter<string>("param1", "Parameter 1", true) }
        });

        _mockModeProvider.Setup(m => m.GetAvailableModes()).Returns(new Dictionary<string, LaunchModeBase>
        {
            { "testMode", mode }
        });

        var args = new[] { "--launch-mode=testMode" };

        // Act & Assert
        var exception = Assert.Throws<MissingParametersException>(() => _parser.ParseArguments(args));
        Assert.Equal("testMode", exception.LaunchModeName);
        Assert.Single(exception.MissingParameters);
    }

    [Fact]
    public void ParseArguments_ValidMode_SetsBooleanParameterCorrectly()
    {
        // Arrange
        var boolParam = new LaunchParameter<bool>("param1", "Boolean Parameter", false);
        var mode = new TestLaunchMode("testMode", "Test mode", new Dictionary<string, ILaunchParameter>
        {
            { "param1", boolParam }
        });

        _mockModeProvider.Setup(m => m.GetAvailableModes()).Returns(new Dictionary<string, LaunchModeBase>
        {
            { "testMode", mode }
        });

        var args = new[] { "--launch-mode=testMode", "--param1" };

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        Assert.NotNull(result);
        Assert.True(boolParam.Value);
    }

    [Fact]
    public void ParseArguments_ValidMode_SetsStringParameterCorrectly()
    {
        // Arrange
        var stringParam = new LaunchParameter<string>("param1", "String Parameter", false);
        var mode = new TestLaunchMode("testMode", "Test mode", new Dictionary<string, ILaunchParameter>
        {
            { "param1", stringParam }
        });

        _mockModeProvider.Setup(m => m.GetAvailableModes()).Returns(new Dictionary<string, LaunchModeBase>
        {
            { "testMode", mode }
        });

        var args = new[] { "--launch-mode=testMode", "--param1=value1" };

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("value1", stringParam.Value);
    }

    [Fact]
    public void ParseArguments_ValidMode_HandlesMultipleAliases()
    {
        // Arrange
        var stringParam = new LaunchParameter<string>("param1", "String Parameter", false, "alias1", "alias2");
        var mode = new TestLaunchMode("testMode", "Test mode", new Dictionary<string, ILaunchParameter>
        {
            { "param1", stringParam }
        });

        _mockModeProvider.Setup(m => m.GetAvailableModes()).Returns(new Dictionary<string, LaunchModeBase>
        {
            { "testMode", mode }
        });

        var args = new[] { "--launch-mode=testMode", "--alias2=value2" };

        // Act
        var result = _parser.ParseArguments(args);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("value2", stringParam.Value);
    }

    private class TestLaunchMode(string name, string description, Dictionary<string, ILaunchParameter> parameters) : LaunchModeBase
    {
        public override string Name { get; } = name;

        public override string Description { get; } = description;

        protected override void InitializeParameters()
        {
            foreach (var parameter in parameters.Values)
            {
                if (parameter is ILaunchParameter<string> stringParam)
                {
                    AddParameter(stringParam);
                }
                else if (parameter is ILaunchParameter<bool> boolParam)
                {
                    AddParameter(boolParam);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported parameter type: {parameter.GetType()}");
                }
            }
        }

        public override Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
