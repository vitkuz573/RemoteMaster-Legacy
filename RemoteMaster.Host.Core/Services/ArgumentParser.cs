// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Exceptions;
using RemoteMaster.Host.Core.ParameterHandlers;

namespace RemoteMaster.Host.Core.Services;

public class ArgumentParser(ILaunchModeProvider modeProvider, IHelpService helpService) : IArgumentParser
{
    private readonly List<IParameterHandler> _handlers =
    [
        new BooleanParameterHandler(),
        new StringParameterHandler()
    ];

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

            var handler = _handlers.FirstOrDefault(h => h.CanHandle(parameter)) ?? throw new NotSupportedException($"No handler found for parameter '{name}' of type {GetFriendlyTypeName(parameter.GetType())}.");

            handler.Handle(args, parameter, name);
        }

        ValidateRequiredParameters(mode);
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericTypeName = type.Name[..type.Name.IndexOf('`')];
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));

        return $"{genericTypeName}<{genericArgs}>";
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
