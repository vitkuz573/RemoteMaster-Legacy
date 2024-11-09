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
        var parametersSnapshot = Parameters.Values.ToList();

        foreach (var parameter in parametersSnapshot)
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
    /// Asynchronously executes the main action for this launch mode.
    /// </summary>
    /// <param name="serviceProvider">Service provider for dependencies.</param>
    public abstract Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
