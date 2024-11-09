// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class LaunchParameter(string description, bool isRequired, params string[] aliases) : ILaunchParameter
{
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
        var paramArg = args.FirstOrDefault(arg =>
            arg.StartsWith($"--{Description}=", StringComparison.OrdinalIgnoreCase) ||
            Aliases.Any(alias => arg.StartsWith($"--{alias}=", StringComparison.OrdinalIgnoreCase)));

        return paramArg?[(paramArg.IndexOf('=') + 1)..].Trim();
    }

    /// <summary>
    /// Sets the value of the parameter, optionally validating or transforming it.
    /// </summary>
    /// <param name="value">The value to set.</param>
    /// <exception cref="ArgumentException">Thrown if the value is invalid.</exception>
    public void SetValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Value for parameter '{Description}' cannot be null or empty.", nameof(value));
        }

        Value = value;
    }
}
