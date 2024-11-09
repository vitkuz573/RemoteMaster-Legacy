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
