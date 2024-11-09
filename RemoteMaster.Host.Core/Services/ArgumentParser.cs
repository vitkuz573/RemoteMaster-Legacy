// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Services;

public class ArgumentParser(ILaunchModeProvider modeProvider, IHelpService helpService) : IArgumentParser
{
    public LaunchModeBase? ParseArguments(string[] args)
    {
        var modeArg = args.FirstOrDefault(arg => arg.StartsWith("--launch-mode=", StringComparison.OrdinalIgnoreCase));

        if (modeArg == null || !modeArg.Contains('='))
        {
            helpService.PrintHelp(null);

            return null;
        }

        var modeName = modeArg[(modeArg.IndexOf('=') + 1)..].Trim();

        if (string.IsNullOrEmpty(modeName))
        {
            helpService.PrintHelp(null);

            return null;
        }

        if (!modeProvider.GetAvailableModes().TryGetValue(modeName, out var mode))
        {
            helpService.SuggestSimilarModes(modeName);

            return null;
        }

        foreach (var paramPair in mode.Parameters)
        {
            var value = GetArgumentValue(args, paramPair.Key, paramPair.Value.Aliases);

            if (value != null)
            {
                paramPair.Value.SetValue(value);
            }
        }

        return mode;
    }

    private static string? GetArgumentValue(string[] args, string key, IEnumerable<string> aliases)
    {
        var paramArg = args.FirstOrDefault(arg =>
            arg.StartsWith($"--{key}=", StringComparison.OrdinalIgnoreCase) ||
            aliases.Any(alias => arg.StartsWith($"--{alias}=", StringComparison.OrdinalIgnoreCase)));

        return paramArg?[(paramArg.IndexOf('=') + 1)..].Trim();
    }
}
