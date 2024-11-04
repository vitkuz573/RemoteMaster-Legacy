// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using RemoteMaster.Host.Core.Models;

namespace RemoteMaster.Host.Core.Abstractions;

public abstract class LaunchModeBase
{
    public abstract string Name { get; }
    
    public abstract string Description { get; }

    public Dictionary<string, ILaunchParameter> Parameters { get; } = [];

    protected abstract void InitializeParameters();

    protected LaunchModeBase()
    {
        InitializeParameters();
        MapAliases();
    }

    private void MapAliases()
    {
        var aliasMappings = new List<KeyValuePair<string, ILaunchParameter>>();

        foreach (var parameter in Parameters.Values)
        {
            if (parameter is LaunchParameter launchParameter)
            {
                foreach (var alias in launchParameter.Aliases)
                {
                    aliasMappings.Add(new KeyValuePair<string, ILaunchParameter>(alias, parameter));
                }
            }
        }

        foreach (var mapping in aliasMappings)
        {
            Parameters[mapping.Key] = mapping.Value;
        }
    }

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

    public string? GetParameterValue(string name)
    {
        return Parameters.TryGetValue(name, out var parameter) ? parameter.Value : null;
    }

    public abstract Task ExecuteAsync(IServiceProvider serviceProvider);
}
