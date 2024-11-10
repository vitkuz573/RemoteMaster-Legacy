// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Exceptions;
using RemoteMaster.Host.Core.Extensions;

namespace RemoteMaster.Host.Core.Services;

public class ArgumentParser(ILaunchModeProvider modeProvider, IHelpService helpService, IEnumerable<IParameterHandler> handlers) : IArgumentParser
{
    private readonly IEnumerable<IParameterHandler> _handlers = handlers;

    public LaunchModeBase? ParseArguments(string[] args)
    {
        var modeName = ExtractLaunchModeName(args);

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

        ParseAndSetParameters(args, mode);

        return mode;
    }

    private static string? ExtractLaunchModeName(string[] args)
    {
        var modeArg = args.FirstOrDefault(arg => arg.StartsWith("--launch-mode=", StringComparison.OrdinalIgnoreCase));
        
        return modeArg?[(modeArg.IndexOf('=') + 1)..].Trim();
    }

    private void ParseAndSetParameters(string[] args, LaunchModeBase mode)
    {
        foreach (var parameterEntry in mode.Parameters)
        {
            var name = parameterEntry.Key;
            var parameter = parameterEntry.Value;

            var handler = _handlers.FirstOrDefault(h => h.CanHandle(parameter)) ?? throw new NotSupportedException($"No handler found for parameter '{name}' of type {parameter.GetType().GetFriendlyName()}.");

            handler.Handle(args, parameter, name);
        }

        ValidateRequiredParameters(mode);
    }

    private static void ValidateRequiredParameters(LaunchModeBase mode)
    {
        var missingParameters = mode.Parameters
            .Where(p => p.Value.IsRequired && p.Value.Value == null)
            .ToList();

        if (missingParameters.Count != 0)
        {
            throw new MissingParametersException(mode.Name, missingParameters);
        }
    }
}
