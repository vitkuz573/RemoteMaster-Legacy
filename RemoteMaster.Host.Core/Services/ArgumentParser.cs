// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Exceptions;

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

        foreach (var paramGroup in mode.Parameters.GroupBy(p => p.Value).Distinct())
        {
            var mainParam = paramGroup.Key;
            var aliases = paramGroup.Select(p => p.Key).ToList();

            var value = aliases
                .Select(alias => mode.Parameters.FirstOrDefault(p => p.Key == alias).Value.GetValue(args))
                .FirstOrDefault(v => v != null);

            if (value != null)
            {
                mainParam.SetValue(Convert.ToString(value) ?? throw new InvalidCastException($"Cannot convert value of type {value.GetType()} to string."));
            }
            else if (mainParam.IsRequired)
            {
                var missingParameters = mode.Parameters
                    .Where(p => p.Value.IsRequired && p.Value.Value == null)
                    .ToList();

                throw new MissingParametersException(mode.Name, missingParameters);
            }
        }

        return mode;
    }
}
