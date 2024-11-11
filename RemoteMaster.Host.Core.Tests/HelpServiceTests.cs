// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using Moq;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class HelpServiceTests
{
    private readonly Mock<ILaunchModeProvider> _launchModeProviderMock = new();

    private HelpService CreateHelpService(Dictionary<string, LaunchModeBase>? availableModes = null)
    {
        var modes = availableModes ?? [];

        _launchModeProviderMock.Setup(p => p.GetAvailableModes()).Returns(modes);

        return new HelpService(_launchModeProviderMock.Object);
    }

    [Fact]
    public void PrintHelp_Should_Print_General_Help_When_No_Specific_Mode()
    {
        // Arrange
        var helpService = CreateHelpService(new Dictionary<string, LaunchModeBase>
        {
            { "TestMode", new TestLaunchMode("TestMode", "Test mode description", []) }
        });

        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.PrintHelp();

        // Assert
        var output = writer.ToString();
        Assert.Contains("Available Modes:", output);
        Assert.Contains("TestMode Mode:", output);
        Assert.Contains("Use \"--help --launch-mode=<MODE>\" for more details", output);
    }

    [Fact]
    public void PrintHelp_Should_Print_Specific_Mode_Help()
    {
        // Arrange
        var specificMode = new TestLaunchMode("TestMode", "Test mode description", new Dictionary<string, ILaunchParameter>
        {
            { "param1", new LaunchParameter<string>("param1", "Parameter 1", true, "p1") },
            { "param2", new LaunchParameter<string>("param2", "Parameter 2", false) }
        });

        var helpService = CreateHelpService();

        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.PrintHelp(specificMode);

        // Assert
        var output = writer.ToString();
        Assert.Contains("TestMode Mode Options:", output);
        Assert.Contains("--param1: Parameter 1 (Required)", output);
        Assert.Contains("--param2: Parameter 2 (Optional)", output);
        Assert.Contains("(Aliases: --p1)", output);
    }

    [Fact]
    public void PrintHelp_Should_Handle_Empty_Parameters_In_Specific_Mode()
    {
        // Arrange
        var specificMode = new TestLaunchMode("EmptyMode", "Mode with no parameters", []);

        var helpService = CreateHelpService();

        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.PrintHelp(specificMode);

        // Assert
        var output = writer.ToString();
        Assert.Contains("EmptyMode Mode Options:", output);
        Assert.Contains("Mode with no parameters", output);
        Assert.DoesNotContain("--", output); // No parameters listed
    }

    [Fact]
    public void PrintMissingParametersError_Should_Print_Error_For_Missing_Parameters()
    {
        // Arrange
        var missingParameters = new List<KeyValuePair<string, ILaunchParameter>>
        {
            new("param1", new LaunchParameter<string>("param1", "Parameter 1", true, "p1")),
            new("param2", new LaunchParameter<string>("param2", "Parameter 2", true, "p2"))
        };

        var helpService = CreateHelpService();

        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.PrintMissingParametersError("TestMode", missingParameters);

        // Assert
        var output = writer.ToString();
        Assert.Contains("Error: Missing required parameters for TestMode mode.", output);
        Assert.Contains("--param1: Parameter 1 (Required) (Aliases: --p1)", output);
        Assert.Contains("--param2: Parameter 2 (Required) (Aliases: --p2)", output);
    }

    [Fact]
    public void PrintMissingParametersError_Should_Handle_No_Missing_Parameters()
    {
        // Arrange
        var helpService = CreateHelpService();

        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.PrintMissingParametersError("TestMode", []);

        // Assert
        var output = writer.ToString();
        Assert.Contains("Error: Missing required parameters for TestMode mode.", output);
        Assert.DoesNotContain("--", output); // No parameters listed
    }

    [Fact]
    public void SuggestSimilarModes_Should_Suggest_Closest_Modes()
    {
        // Arrange
        var inputMode = "TesMode";
        var availableModes = new Dictionary<string, LaunchModeBase>
        {
            { "TestMode", new TestLaunchMode("TestMode", "Test mode description", []) },
            { "ProdMode", new TestLaunchMode("ProdMode", "Production mode description", []) },
            { "DevMode", new TestLaunchMode("DevMode", "Development mode description", []) }
        };

        var helpService = CreateHelpService(availableModes);

        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.SuggestSimilarModes(inputMode);

        // Assert
        var output = writer.ToString();
        Assert.Contains("Did you mean one of these modes?", output);
        Assert.Contains("TestMode", output);
        Assert.Contains("Test mode description", output);
    }

    [Fact]
    public void SuggestSimilarModes_Should_Handle_Empty_InputMode()
    {
        // Arrange
        var availableModes = new Dictionary<string, LaunchModeBase>
        {
            { "TestMode", new TestLaunchMode("TestMode", "Test mode description", []) },
            { "ProdMode", new TestLaunchMode("ProdMode", "Production mode description", []) },
            { "DevMode", new TestLaunchMode("DevMode", "Development mode description", []) }
        };

        var helpService = CreateHelpService(availableModes);

        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.SuggestSimilarModes(string.Empty);

        // Assert
        var output = writer.ToString();
        Assert.Contains("You haven't provided a launch mode.", output);
    }

    [Fact]
    public void SuggestSimilarModes_Should_Handle_Empty_AvailableModes()
    {
        // Arrange
        const string inputMode = "TesMode";

        var helpService = CreateHelpService();

        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.SuggestSimilarModes(inputMode);

        // Assert
        var output = writer.ToString();
        Assert.Contains($"No similar modes found for input: \"{inputMode}\".", output);
        Assert.DoesNotContain("Did you mean one of these modes?", output);
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
