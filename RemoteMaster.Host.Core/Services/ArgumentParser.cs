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

        if (modeArg == null)
        {
            helpService.PrintHelp(null);

            return null;
        }

        var modeName = modeArg.Split('=')[1];

        if (!modeProvider.GetAvailableModes().TryGetValue(modeName, out var mode))
        {
            helpService.SuggestSimilarModes(modeName);

            return null;
        }

        foreach (var paramPair in mode.Parameters)
        {
            var key = paramPair.Key;
            var param = paramPair.Value;

            var paramArg = args.FirstOrDefault(arg =>
                arg.StartsWith($"--{key}=", StringComparison.OrdinalIgnoreCase) ||
                param.Aliases.Any(alias => arg.StartsWith($"--{alias}=", StringComparison.OrdinalIgnoreCase)));

            if (paramArg != null)
            {
                var value = paramArg[(paramArg.IndexOf('=') + 1)..];
                param.SetValue(value);
            }
        }

        return mode;
    }
}
