// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Helpers;

namespace RemoteMaster.Host.Core.Services;

public class HelpService(ILaunchModeProvider modeProvider) : IHelpService
{
    public void PrintHelp(LaunchModeBase? specificMode = null)
    {
        if (specificMode != null)
        {
            PrintModeHelp(specificMode);
        }
        else
        {
            PrintGeneralHelp();
        }
    }

    public void PrintMissingParametersError(string modeName, IEnumerable<KeyValuePair<string, ILaunchParameter>> missingParameters)
    {
        ArgumentNullException.ThrowIfNull(missingParameters);

        PrintMessage($"Error: Missing required parameters for {modeName} mode.", ConsoleColor.Red);

        var uniqueParameters = missingParameters
            .GroupBy(p => p.Value)
            .Select(g => g.First())
            .ToList();

        foreach (var parameterPair in uniqueParameters)
        {
            var mainKey = parameterPair.Value.Name;

            var aliases = parameterPair.Value.Aliases
                .Where(alias => alias != mainKey)
                .ToList();

            var aliasText = aliases.Count > 0
                ? $" (Aliases: {string.Join(", ", aliases.Select(alias => $"--{alias}"))})"
                : string.Empty;

            Console.WriteLine($"  --{mainKey}: {parameterPair.Value.Description} (Required){aliasText}");
        }
    }

    public void SuggestSimilarModes(string inputMode)
    {
        var availableModes = modeProvider.GetAvailableModes().Values;

        if (string.IsNullOrEmpty(inputMode))
        {
            PrintMessage("You haven't provided a launch mode.", ConsoleColor.Yellow);

            return;
        }

        const double similarityThreshold = 0.6;

        var suggestions = availableModes
            .Select(mode => new
            {
                Mode = mode,
                Similarity = ComputeSimilarity(inputMode.ToLower(), mode.Name.ToLower())
            })
            .Where(result => result.Similarity >= similarityThreshold)
            .OrderByDescending(result => result.Similarity)
            .Take(3)
            .Select(result => result.Mode)
            .ToList();

        if (suggestions.Count == 0)
        {
            PrintMessage($"No similar modes found for input: \"{inputMode}\".", ConsoleColor.Red);
            PrintMessage("Make sure you have typed the launch mode correctly or use \"--help\" for available modes.", ConsoleColor.Yellow);
            
            return;
        }

        PrintMessage("Did you mean one of these modes?", ConsoleColor.Yellow);

        foreach (var suggestion in suggestions)
        {
            PrintModeSummary(suggestion);
        }
    }

    private void PrintGeneralHelp()
    {
        var availableModes = modeProvider.GetAvailableModes().Values;

        PrintMessage("Available Modes:", ConsoleColor.Green);

        foreach (var mode in availableModes)
        {
            PrintModeSummary(mode);
        }

        PrintMessage("Use \"--help --launch-mode=<MODE>\" for more details on a specific mode.", ConsoleColor.Yellow);
    }

    private static void PrintModeHelp(LaunchModeBase mode)
    {
        PrintMessage($"{mode.Name} Mode Options:", ConsoleColor.Yellow);

        Console.WriteLine($"  {mode.Description}\n");

        var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, param) in mode.Parameters)
        {
            if (processedKeys.Contains(key))
            {
                continue;
            }

            var aliases = param.Aliases
                .Where(alias => !string.Equals(alias, key, StringComparison.OrdinalIgnoreCase))
                .Distinct()
                .ToList();

            processedKeys.Add(key);

            foreach (var alias in aliases)
            {
                processedKeys.Add(alias);
            }

            var aliasText = aliases.Count > 0
                ? $" (Aliases: {string.Join(", ", aliases.Select(alias => $"--{alias}"))})"
                : string.Empty;

            Console.WriteLine($"  --{key}: {param.Description} {(param.IsRequired ? "(Required)" : "(Optional)")}{aliasText}");
        }
    }

    private static void PrintModeSummary(LaunchModeBase mode)
    {
        PrintMessage($"{mode.Name} Mode:", ConsoleColor.Yellow);
        Console.WriteLine($"  {mode.Description}\n");
    }

    private static void PrintMessage(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static double ComputeSimilarity(string input, string target)
    {
        var distance = LevenshteinDistance.Compute(input, target);

        return 1.0 - (double)distance / Math.Max(input.Length, target.Length);
    }
}
