// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Exceptions;
using RemoteMaster.Host.Core.Helpers;

namespace RemoteMaster.Host.Core.Services;

public class ArgumentSerializer(ILaunchModeProvider modeProvider, IEnumerable<IParameterSerializer> serializers) : IArgumentSerializer
{
    public string[] Serialize(LaunchModeBase mode)
    {
        ArgumentNullException.ThrowIfNull(mode);

        var arguments = new List<string>
        {
            $"--launch-mode={mode.Name.ToLower()}"
        };

        var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, parameter) in mode.Parameters)
        {
            if (!processedKeys.Add(name))
            {
                continue;
            }

            foreach (var alias in parameter.Aliases)
            {
                processedKeys.Add(alias);
            }

            var serializer = serializers.FirstOrDefault(s => s.CanHandle(parameter)) ?? throw new NotSupportedException($"No serializer found for parameter '{name}'.");

            var serializedValue = serializer.Serialize(parameter, name);

            if (!string.IsNullOrEmpty(serializedValue))
            {
                arguments.Add(serializedValue);
            }
        }

        return [.. arguments];
    }

    public LaunchModeBase Deserialize(string[] args)
    {
        var modeName = ArgumentUtils.ExtractLaunchModeName(args);

        if (string.IsNullOrEmpty(modeName) || !modeProvider.GetAvailableModes().TryGetValue(modeName, out var mode))
        {
            throw new ArgumentException("Invalid or missing launch mode.");
        }

        foreach (var (name, parameter) in mode.Parameters)
        {
            var serializer = serializers.FirstOrDefault(s => s.CanHandle(parameter)) ?? throw new NotSupportedException($"No serializer found for parameter '{name}'.");

            var aliases = new[] { name }.Concat(parameter.Aliases).ToList();

            var matchedArg = args.FirstOrDefault(arg => aliases.Any(alias =>
                arg.Equals($"--{alias}", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals($"-{alias}", StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith($"--{alias}=", StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith($"-{alias}=", StringComparison.OrdinalIgnoreCase)));

            if (matchedArg == null)
            {
                continue;
            }

            string? argumentValue = null;

            if (matchedArg.Contains('='))
            {
                argumentValue = matchedArg[(matchedArg.IndexOf('=') + 1)..];
            }

            serializer.Deserialize(argumentValue, parameter);
        }

        ValidateRequiredParameters(mode);

        return mode;
    }

    private static void ValidateRequiredParameters(LaunchModeBase mode)
    {
        var missingParameters = mode.Parameters
            .Where(p => p.Value.IsRequired && (p.Value.Value == null || (p.Value.Value is string str && string.IsNullOrWhiteSpace(str))))
            .ToList();

        if (missingParameters.Count > 0)
        {
            throw new MissingParametersException(mode.Name, missingParameters);
        }
    }
}
