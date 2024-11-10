// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class LaunchParameter(string name, string description, bool isRequired, params string[] aliases) : ILaunchParameter
{
    public string Name { get; } = name;

    public string Description { get; } = description;

    public bool IsRequired { get; } = isRequired;

    public string? Value { get; private set; }

    public IReadOnlyList<string> Aliases { get; } = aliases;

    /// <summary>
    /// Attempts to extract the value for this parameter from the provided arguments.
    /// </summary>
    /// <param name="args">The list of arguments.</param>
    /// <returns>The extracted value or null if not found.</returns>
    public string? GetValue(string[] args)
    {
        var paramArg = args.FirstOrDefault(arg => arg.StartsWith($"--{Name}=", StringComparison.OrdinalIgnoreCase));

        if (paramArg is null)
        {
            foreach (var alias in Aliases)
            {
                paramArg = args.FirstOrDefault(arg => arg.StartsWith($"--{alias}=", StringComparison.OrdinalIgnoreCase));
                
                if (paramArg != null)
                {
                    break;
                }

                if (alias.Length == 1)
                {
                    paramArg = args.FirstOrDefault(arg => arg.StartsWith($"-{alias}=", StringComparison.OrdinalIgnoreCase));
                    
                    if (paramArg != null)
                    {
                        break;
                    }
                }
            }
        }

        return paramArg?[(paramArg.IndexOf('=') + 1)..].Trim();
    }

    /// <summary>
    /// Sets the value of the parameter.
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <exception cref="ArgumentException">Thrown if the value is invalid.</exception>
    public void SetValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Value for parameter '{Name}' cannot be null or empty.", nameof(value));
        }

        Value = value;
    }
}
