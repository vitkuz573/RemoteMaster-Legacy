// Copyright © 2023 Vitaly Kuzyaev. All rights reserved.
// This file is part of the RemoteMaster project.
// Licensed under the GNU Affero General Public License v3.0.

using System.Collections.Concurrent;

namespace RemoteMaster.Host.Core.Abstractions;

public abstract class LaunchModeBase
{
    public abstract string Name { get; }

    public abstract string Description { get; }


    private readonly ConcurrentDictionary<string, ILaunchParameter> _parameters = new();

    /// <summary>
    /// Retrieves all parameters as a read-only dictionary.
    /// </summary>
    public IReadOnlyDictionary<string, ILaunchParameter> Parameters => _parameters;

    protected LaunchModeBase()
    {
        InitializeParameters();
        MapAliases();
    }

    /// <summary>
    /// Initializes parameters for this specific launch mode.
    /// </summary>
    protected abstract void InitializeParameters();

    /// <summary>
    /// Adds a typed parameter to the collection.
    /// </summary>
    /// <typeparam name="T">The type of the parameter value.</typeparam>
    /// <param name="parameter">The parameter to add.</param>
    protected void AddParameter<T>(ILaunchParameter<T> parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (!_parameters.TryAdd(parameter.Name, parameter))
        {
            throw new InvalidOperationException($"Parameter with name '{parameter.Name}' already exists.");
        }
    }

    /// <summary>
    /// Maps parameter aliases to their primary names for simplified access.
    /// Ensures alias uniqueness.
    /// </summary>
    private void MapAliases()
    {
        foreach (var parameterEntry in _parameters.ToList())
        {
            if (parameterEntry.Value is not ILaunchParameter parameter)
            {
                continue;
            }

            foreach (var alias in parameter.Aliases)
            {
                if (!_parameters.TryAdd(alias, parameterEntry.Value))
                {
                    throw new InvalidOperationException($"Alias conflict: The alias '{alias}' is already associated with another parameter.");
                }
            }
        }
    }

    /// <summary>
    /// Retrieves a parameter by name or alias.
    /// </summary>
    /// <typeparam name="T">The expected type of the parameter.</typeparam>
    /// <param name="name">The name or alias of the parameter.</param>
    /// <returns>The parameter as <see cref="ILaunchParameter{T}"/>.</returns>
    public ILaunchParameter<T>? GetParameter<T>(string name)
    {
        if (_parameters.TryGetValue(name, out var parameter) && parameter is ILaunchParameter<T> typedParameter)
        {
            return typedParameter;
        }

        return null;
    }

    /// <summary>
    /// Executes the main logic for this launch mode.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous execution.</param>
    public abstract Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
