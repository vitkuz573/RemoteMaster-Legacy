// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class LaunchParameter(string name, string description, bool isRequired, params string[] aliases) : ILaunchParameter
{
    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Parameter name cannot be null or empty.", nameof(name))
        : name;

    public string Description { get; } = string.IsNullOrWhiteSpace(description)
        ? throw new ArgumentException("Description cannot be null or empty.", nameof(description))
        : description;

    public bool IsRequired { get; } = isRequired;

    public string? Value { get; private set; }

    public IReadOnlyList<string> Aliases { get; } = aliases ?? throw new ArgumentNullException(nameof(aliases));

    /// <summary>
    /// Attempts to extract the value for this parameter from the provided arguments.
    /// </summary>
    /// <param name="args">The list of arguments.</param>
    /// <returns>The extracted value or null if not found.</returns>
    public string? GetValue(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        foreach (var arg in args)
        {
            if (arg.StartsWith($"--{Name}=", StringComparison.OrdinalIgnoreCase))
            {
                return arg[(arg.IndexOf('=') + 1)..].Trim();
            }

            foreach (var alias in Aliases)
            {
                if (arg.StartsWith($"--{alias}=", StringComparison.OrdinalIgnoreCase) || (alias.Length == 1 && arg.StartsWith($"-{alias}=", StringComparison.OrdinalIgnoreCase)))
                {
                    return arg[(arg.IndexOf('=') + 1)..].Trim();
                }
            }
        }

        return null;
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
