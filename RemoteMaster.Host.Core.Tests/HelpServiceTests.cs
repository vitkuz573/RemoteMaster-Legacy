// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Models;
using RemoteMaster.Host.Core.Services;

namespace RemoteMaster.Host.Core.Tests;

public class HelpServiceTests
{
    [Fact]
    public void PrintHelp_Should_Print_General_Help_When_No_Specific_Mode()
    {
        // Arrange
        var helpService = new HelpService();
        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.PrintHelp(null);

        // Assert
        var output = writer.ToString();
        Assert.Contains("Usage:", output);
        Assert.Contains("Mode:", output);
        Assert.Contains("Use \"--help --launch-mode=<MODE>\" for more details", output);
    }

    [Fact]
    public void PrintHelp_Should_Print_Specific_Mode_Help()
    {
        // Arrange
        var specificMode = new TestLaunchMode("TestMode", "Test mode description", new Dictionary<string, ILaunchParameter>
        {
            { "param1", new LaunchParameter("Parameter 1", true, ["p1"]) },
            { "param2", new LaunchParameter("Parameter 2", false) }
        });

        var helpService = new HelpService();
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

        var helpService = new HelpService();
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
            new("param1", new LaunchParameter("Parameter 1", true)),
            new("param2", new LaunchParameter("Parameter 2", true, ["p2"]))
        };

        var helpService = new HelpService();
        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.PrintMissingParametersError("TestMode", missingParameters);

        // Assert
        var output = writer.ToString();
        Assert.Contains("Error: Missing required parameters for TestMode mode.", output);
        Assert.Contains("--param1: Parameter 1 (Required)", output);
        Assert.Contains("--param2: Parameter 2 (Required) (Aliases: --p2)", output);
    }

    [Fact]
    public void PrintMissingParametersError_Should_Handle_No_Missing_Parameters()
    {
        // Arrange
        var helpService = new HelpService();
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
        var availableModes = new[] { "TestMode", "ProdMode", "DevMode" };

        var helpService = new HelpService();
        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.SuggestSimilarModes(inputMode, availableModes);

        // Assert
        var output = writer.ToString();
        Assert.Contains("Did you mean one of these modes?", output);
        Assert.Contains("- TestMode", output);
    }

    [Fact]
    public void SuggestSimilarModes_Should_Handle_Empty_InputMode()
    {
        // Arrange
        var availableModes = new[] { "TestMode", "ProdMode", "DevMode" };

        var helpService = new HelpService();
        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.SuggestSimilarModes(string.Empty, availableModes);

        // Assert
        var output = writer.ToString();
        Assert.Contains("No launch mode provided.", output);
    }

    [Fact]
    public void SuggestSimilarModes_Should_Handle_Empty_AvailableModes()
    {
        // Arrange
        var inputMode = "TesMode";

        var helpService = new HelpService();
        using var writer = new StringWriter();
        Console.SetOut(writer);

        // Act
        helpService.SuggestSimilarModes(inputMode, []);

        // Assert
        var output = writer.ToString();
        Assert.Contains("Did you mean one of these modes?", output);
        Assert.DoesNotContain("-", output); // No suggestions listed
    }

    private class TestLaunchMode(string name, string description, Dictionary<string, ILaunchParameter> parameters) : LaunchModeBase
    {
        public override string Name => name;

        public override string Description => description;

        protected override void InitializeParameters()
        {
            foreach (var parameter in parameters)
            {
                Parameters.Add(parameter.Key, parameter.Value);
            }
        }

        public override Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
