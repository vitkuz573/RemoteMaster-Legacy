// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Reflection;
using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Helpers;
using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.Services;

public class HelpService : IHelpService
{
    private readonly Assembly _assembly;

    public HelpService()
    {
        _assembly = Assembly.GetExecutingAssembly();
    }

    public void PrintHelp(LaunchModeBase? specificMode)
    {
        if (specificMode != null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{specificMode.Name} Mode Options:");
            Console.ResetColor();

            Console.WriteLine($"  {specificMode.Description}");
            Console.WriteLine();

            var uniqueParameters = specificMode.Parameters
                .GroupBy(p => p.Value)
                .Select(g => g.First().Key)
                .ToList();

            foreach (var key in uniqueParameters)
            {
                var param = specificMode.Parameters[key];
                var aliases = param is LaunchParameter launchParam && launchParam.Aliases.Any()
                    ? $" (Aliases: {string.Join(", ", launchParam.Aliases.Select(alias => $"--{alias}"))})"
                    : string.Empty;

                Console.WriteLine($"  --{key}: {param.Description} {(param.IsRequired ? "(Required)" : "(Optional)")}{aliases}");
            }
        }
        else
        {
            if (_assembly == null)
            {
                return;
            }

            var launchModes = _assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(LaunchModeBase)))
                .Select(t => Activator.CreateInstance(t) as LaunchModeBase)
                .Where(instance => instance != null);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Usage: {_assembly.GetName().Name} [OPTIONS]");
            Console.ResetColor();
            Console.WriteLine();

            foreach (var mode in launchModes)
            {
                if (mode == null)
                {
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{mode.Name} Mode:");
                Console.ResetColor();

                Console.WriteLine($"  {mode.Description}");
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Use \"--help --launch-mode=<MODE>\" for more details on a specific mode.");
            Console.ResetColor();
        }
    }

    public void PrintMissingParametersError(string modeName, IEnumerable<KeyValuePair<string, ILaunchParameter>> missingParameters)
    {
        ArgumentNullException.ThrowIfNull(missingParameters);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: Missing required parameters for {modeName} mode.");
        Console.ResetColor();

        var groupedParameters = missingParameters
            .GroupBy(p => p.Value)
            .Select(g => new
            {
                MainKey = g.First().Key,
                Parameter = g.Key,
                Aliases = g.SelectMany(p => p.Value.Aliases).Distinct()
            });

        foreach (var param in groupedParameters)
        {
            var aliases = param.Aliases.Any()
                ? $" (Aliases: {string.Join(", ", param.Aliases.Select(alias => $"--{alias}"))})"
                : string.Empty;

            Console.WriteLine($"  --{param.MainKey}: {param.Parameter.Description} (Required){aliases}");
        }

        Console.WriteLine();
    }

    public void SuggestSimilarModes(string inputMode, IEnumerable<LaunchModeBase> availableModes)
    {
        if (string.IsNullOrEmpty(inputMode))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("You haven't provided a launch mode.");
            Console.ResetColor();

            return;
        }

        var suggestions = availableModes
            .Select(mode => new
            {
                Mode = mode,
                Distance = LevenshteinDistance.Compute(inputMode.ToLower(), mode.Name.ToLower())
            })
            .OrderBy(s => s.Distance)
            .Take(3)
            .Select(s => s.Mode)
            .ToList();

        if (suggestions.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No similar modes found.");
            Console.ResetColor();

            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Did you mean one of these modes?");
        Console.ResetColor();

        foreach (var suggestion in suggestions)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"- {suggestion.Name}");
            Console.ResetColor();

            Console.WriteLine($"  {suggestion.Description}");
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Use \"--help --launch-mode=<MODE>\" for more details on a specific mode.");
        Console.ResetColor();
    }
}
