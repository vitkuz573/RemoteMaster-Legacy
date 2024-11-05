// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public abstract class LaunchModeBase
{
    public abstract string Name { get; }
    
    public abstract string Description { get; }

    public readonly Dictionary<string, ILaunchParameter> Parameters = [];

    protected LaunchModeBase()
    {
        InitializeParameters();
        MapAliases();
    }

    /// <summary>
    /// Method to initialize parameters for each specific launch mode.
    /// </summary>
    protected abstract void InitializeParameters();

    /// <summary>
    /// Maps parameter aliases to their primary names for simplified access.
    /// Checks for alias conflicts to ensure uniqueness.
    /// </summary>
    private void MapAliases()
    {
        foreach (var parameter in Parameters.Values)
        {
            if (parameter is not LaunchParameter launchParameter)
            {
                continue;
            }

            foreach (var alias in launchParameter.Aliases)
            {
                if (!Parameters.TryAdd(alias, parameter))
                {
                    throw new InvalidOperationException($"Alias conflict: The alias '{alias}' is already associated with another parameter.");
                }
            }
        }
    }

    /// <summary>
    /// Sets the value of a parameter by its name or alias.
    /// </summary>
    /// <param name="nameOrAlias">The name or alias of the parameter.</param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="ArgumentException">Thrown if the parameter is not found.</exception>
    public void SetParameterValue(string nameOrAlias, string value)
    {
        if (Parameters.TryGetValue(nameOrAlias, out var parameter))
        {
            parameter.Value = value;
        }
        else
        {
            throw new ArgumentException($"Parameter '{nameOrAlias}' not found.");
        }
    }

    /// <summary>
    /// Retrieves the value of a parameter by its name, if it exists.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <returns>The parameter value, or null if not found.</returns>
    public string? GetParameterValue(string name)
    {
        return Parameters.TryGetValue(name, out var parameter) ? parameter.Value : null;
    }

    /// <summary>
    /// Asynchronously executes the main action for this launch mode.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependencies.</param>
    public abstract Task ExecuteAsync(IServiceProvider serviceProvider);
}
