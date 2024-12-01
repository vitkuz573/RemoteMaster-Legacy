// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;
using RemoteMaster.Host.Core.Exceptions;
using RemoteMaster.Host.Core.Helpers;

namespace RemoteMaster.Host.Core.Services;

public class ArgumentSerializer(ILaunchModeProvider modeProvider, IEnumerable<IParameterHandler> handlers) : IArgumentSerializer
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
            if (processedKeys.Contains(name))
            {
                continue;
            }

            processedKeys.Add(name);

            if (parameter.Aliases != null)
            {
                foreach (var alias in parameter.Aliases)
                {
                    processedKeys.Add(alias);
                }
            }

            if (parameter.Value is bool boolValue)
            {
                if (boolValue)
                {
                    arguments.Add($"--{name}");
                }
            }
            else if (parameter.Value is string stringValue)
            {
                if (!string.IsNullOrWhiteSpace(stringValue))
                {
                    arguments.Add($"--{name}={stringValue}");
                }
                else if (parameter.IsRequired)
                {
                    arguments.Add($"--{name}=");
                }
            }
            else if (parameter.Value != null)
            {
                arguments.Add($"--{name}={parameter.Value}");
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
            var handler = handlers.FirstOrDefault(h => h.CanHandle(parameter)) ?? throw new NotSupportedException($"No handler found for parameter '{name}'.");
            handler.Handle(args, parameter, name);
        }

        ValidateRequiredParameters(mode);

        return mode;
    }

    private static void ValidateRequiredParameters(LaunchModeBase mode)
    {
        var missingParameters = mode.Parameters
            .Where(p => p.Value.IsRequired && (p.Value.Value == null || (p.Value.Value is string str && string.IsNullOrWhiteSpace(str))))
            .ToList();

        if (missingParameters.Count != 0)
        {
            throw new MissingParametersException(mode.Name, missingParameters);
        }
    }
}
