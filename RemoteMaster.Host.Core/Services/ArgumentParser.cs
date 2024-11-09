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

        foreach (var param in mode.Parameters.Values)
        {
            var value = param.GetValue(args);

            if (value != null)
            {
                param.SetValue(value);
            }
        }

        return mode;
    }
}
