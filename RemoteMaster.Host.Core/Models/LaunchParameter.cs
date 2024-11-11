// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Abstractions;

namespace RemoteMaster.Host.Core.Models;

public class LaunchParameter<T>(string name, string description, bool isRequired, params string[] aliases) : ILaunchParameter<T>
{
    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Parameter name cannot be null or empty.", nameof(name))
        : name;

    public string Description { get; } = string.IsNullOrWhiteSpace(description)
        ? throw new ArgumentException("Description cannot be null or empty.", nameof(description))
        : description;

    public bool IsRequired { get; } = isRequired;

    public T? Value { get; private set; }

    public IReadOnlyList<string> Aliases { get; } = aliases ?? throw new ArgumentNullException(nameof(aliases));

    /// <summary>
    /// Attempts to extract the value for this parameter from the provided arguments and converts it to the expected type.
    /// </summary>
    /// <param name="args">The list of arguments.</param>
    /// <returns>The extracted value or null if not found.</returns>
    public T? GetValue(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        foreach (var arg in args)
        {
            if (arg.Equals($"--{Name}", StringComparison.OrdinalIgnoreCase))
            {
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)true;
                }

                throw new ArgumentException($"Parameter '{Name}' expects a value.");
            }

            if (arg.StartsWith($"--{Name}=", StringComparison.OrdinalIgnoreCase))
            {
                var stringValue = arg[(arg.IndexOf('=') + 1)..].Trim();

                return ConvertValue(stringValue);
            }

            foreach (var alias in Aliases)
            {
                if (arg.Equals($"--{alias}", StringComparison.OrdinalIgnoreCase) || (alias.Length == 1 && arg.Equals($"-{alias}", StringComparison.OrdinalIgnoreCase)))
                {
                    if (typeof(T) == typeof(bool))
                    {
                        return (T)(object)true;
                    }

                    throw new ArgumentException($"Parameter '{Name}' expects a value.");
                }

                if (!arg.StartsWith($"--{alias}=", StringComparison.OrdinalIgnoreCase) && (alias.Length != 1 || !arg.StartsWith($"-{alias}=", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var stringValue = arg[(arg.IndexOf('=') + 1)..].Trim();

                return ConvertValue(stringValue);
            }
        }

        return default;
    }

    /// <summary>
    /// Sets the value of the parameter with type conversion.
    /// </summary>
    /// <param name="value">The value to set as a string.</param>
    public void SetValue(T value)
    {
        Value = value;
    }

    void ILaunchParameter.SetValue(object value)
    {
        switch (value)
        {
            case T typedValue:
                SetValue(typedValue);
                break;
            case string stringValue:
                Value = ConvertValue(stringValue);
                break;
            default:
                throw new ArgumentException($"Invalid value type. Expected {typeof(T)}, got {value.GetType()}");
        }
    }

    /// <summary>
    /// Converts the string value to the expected type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="stringValue">The value as a string.</param>
    /// <returns>The converted value.</returns>
    private static T ConvertValue(string stringValue)
    {
        try
        {
            if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(stringValue, out var boolResult))
                {
                    return (T)(object)boolResult;
                }

                throw new FormatException($"Invalid boolean value: '{stringValue}'");
            }

            if (typeof(T).IsEnum)
            {
                return (T)Enum.Parse(typeof(T), stringValue, true);
            }

            return (T)Convert.ChangeType(stringValue, typeof(T));
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to convert '{stringValue}' to type {typeof(T)}: {ex.Message}", ex);
        }
    }

    object? ILaunchParameter.Value => Value;

    object? ILaunchParameter.GetValue(string[] args) => GetValue(args);
}
